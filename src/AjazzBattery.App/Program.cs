using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;
using AjazzBattery.Storage;

namespace AjazzBattery.App;

internal enum AppLifecyclePhase
{
    Starting,
    Running,
    ShuttingDown
}

internal static class Program
{
    private static Mutex? _singleInstanceMutex;
    private static NamedPipeServerStream? _pipeServer;

    private static AppLifecyclePhase _currentPhase = AppLifecyclePhase.Starting;
    private static readonly ConcurrentDictionary<string, bool> ShownErrorFingerprints = new();

    public static AppLifecyclePhase CurrentPhase => _currentPhase;

    // IPC command constants
    private const string PipeName     = "AjazzBatteryMonitor_IPC";
    private const string CmdOverview  = "ShowOverview";
    private const string CmdSettings  = "ShowSettings";
    private const string CmdShutdown  = "ShutdownForUpdate";

    [STAThread]
    private static void Main(string[] args)
    {
        _currentPhase = AppLifecyclePhase.Starting;

        // 1. Immediate Startup Logging BEFORE any UI, HID, or BLE initialization
        Logger.Log("STARTUP", "Process started");
        string exePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        Logger.Log("STARTUP", $"Application version: {typeof(Program).Assembly.GetName().Version}");
        Logger.Log("STARTUP", $"Executable path: {exePath}");
        Logger.Log("STARTUP", $"Runtime version: {Environment.Version}");
        Logger.Log("STARTUP", $"OS version: {Environment.OSVersion}");
        Logger.Log("STARTUP", $"Process architecture: {RuntimeInformation.ProcessArchitecture}");
        Logger.Log("STARTUP", $"Local timezone: {TimeZoneInfo.Local.Id} (UTC Offset: {TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow)})");
        Logger.Log("STARTUP", $"Args: [{string.Join(", ", args)}]");

        // 2. Global Unhandled Exception Handling
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) => HandleUnhandledException("Application.ThreadException", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleUnhandledException("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            HandleUnhandledException("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        // 3. Command Line Flag Parsing
        bool allowMultiple         = args.Contains("--allow-multiple-instances");
        bool isSmokeTest           = args.Contains("--smoke-test");
        bool isSmokeTestUi         = args.Contains("--smoke-test-ui");
        bool isSmokeTestNotif      = args.Contains("--smoke-test-notification");
        bool isDumpUiTree          = args.Contains("--dump-ui-tree");

        // Launch mode flags
        bool backgroundMode        = args.Contains("--background") || args.Contains("--autostart");
        bool showOverviewMode      = !backgroundMode && (
                                        args.Length == 0 ||
                                        args.Contains("--show") ||
                                        args.Contains("--overview") ||
                                        isSmokeTestUi);
        bool showSettingsMode      = !backgroundMode && args.Contains("--settings");

        int? mockBattery = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith("--mock-battery=", StringComparison.Ordinal))
            {
                if (int.TryParse(arg.Substring("--mock-battery=".Length), out int mb)) mockBattery = mb;
            }
        }

        if (isDumpUiTree)
        {
            ApplicationConfiguration.Initialize();
            var dummyEngine = new BatteryMonitorEngine(Array.Empty<IMouseBatteryProvider>(), null!, null!, st => { });
            var dummyNotif = new BatteryNotificationService(new FakeNotificationTransport(), new FakeNotificationTransport());
            using var form = new MainForm(dummyEngine, dummyNotif, new WindowsAutoStartManager(), new BatteryHistoryStorage());
            form.ValidateNavigationInvariants();
            string tree = form.DumpUiTree();
            Console.WriteLine(tree);
            Logger.Log("UI_TREE", tree);
            return;
        }

        // 4. Single-Instance Mutex & Named Pipe IPC
        if (!allowMultiple)
        {
            Logger.Log("MUTEX", "Single-instance check started");
            bool createdNew;
            try
            {
                _singleInstanceMutex = new Mutex(true, @"Local\AjazzBatteryMonitor", out createdNew);
            }
            catch (AbandonedMutexException)
            {
                Logger.Log("MUTEX", "AbandonedMutexException caught - acquiring mutex ownership.");
                createdNew = true;
            }

            if (!createdNew)
            {
                // Determine which IPC command to send based on args
                string ipcCommand = showSettingsMode ? CmdSettings : CmdOverview;
                Logger.Log("MUTEX", $"Second instance detected - sending IPC command '{ipcCommand}' and exiting.");
                SendIpcCommand(ipcCommand);
                return;
            }
            Logger.Log("MUTEX", "Single-instance check passed");
        }

        Logger.Log("STARTUP", $"Launch mode: background={backgroundMode}, showOverview={showOverviewMode}, showSettings={showSettingsMode}");

        // 5. Initialize WinForms UI Loop
        try
        {
            ApplicationConfiguration.Initialize();

            var launchMode = backgroundMode ? LaunchMode.Background :
                             showSettingsMode ? LaunchMode.Settings :
                             LaunchMode.Overview;

            var context = new TrayApplicationContext(launchMode, isSmokeTest, isSmokeTestUi, mockBattery);

            _currentPhase = AppLifecyclePhase.Running;
            Logger.Log("LIFECYCLE", "App phase transitioned to Running");

            // Start Named Pipe IPC server (background thread)
            _ = Task.Run(() => RunPipeServerAsync(context));

            Application.Run(context);
        }
        catch (Exception ex)
        {
            HandleUnhandledException("Application.Run", ex);
        }
        finally
        {
            _currentPhase = AppLifecyclePhase.ShuttingDown;
            _pipeServer?.Dispose();
            _singleInstanceMutex?.ReleaseMutex();
        }
    }

    private static void SendIpcCommand(string command)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1000); // 1s timeout
            var data = Encoding.UTF8.GetBytes(command);
            client.Write(data, 0, data.Length);
            client.Flush();
        }
        catch (Exception ex)
        {
            Logger.Log("IPC", $"Failed to send IPC command '{command}': {ex.Message}");
            // Legacy fallback: try old EventWaitHandle
            try
            {
                using var evt = EventWaitHandle.OpenExisting(@"Local\AjazzBatteryMonitor_Activate");
                evt.Set();
            }
            catch { }
        }
    }

    private static async Task RunPipeServerAsync(TrayApplicationContext context)
    {
        while (_currentPhase != AppLifecyclePhase.ShuttingDown)
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await _pipeServer.WaitForConnectionAsync();

                using var reader = new StreamReader(_pipeServer, Encoding.UTF8);
                string? command = await reader.ReadToEndAsync();
                command = command?.Trim();

                Logger.Log("IPC", $"Received IPC command: '{command}'");

                if (!string.IsNullOrEmpty(command))
                {
                    DispatchIpcCommand(context, command);
                }

                _pipeServer.Disconnect();
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                Logger.Log("IPC", $"Pipe server error: {ex.Message}");
                await Task.Delay(500);
            }
        }
    }

    private static void DispatchIpcCommand(TrayApplicationContext context, string command)
    {
        try
        {
            if (Application.OpenForms.Count > 0)
            {
                Application.OpenForms[0]?.BeginInvoke(() => ExecuteIpcCommand(context, command));
            }
            else
            {
                // Fall back to invoking from the context via the notification area handle
                context.BeginInvokeOnUiThread(() => ExecuteIpcCommand(context, command));
            }
        }
        catch (Exception ex)
        {
            Logger.Log("IPC", $"Dispatch error: {ex.Message}");
        }
    }

    private static void ExecuteIpcCommand(TrayApplicationContext context, string command)
    {
        switch (command)
        {
            case CmdOverview:
                Logger.Log("IPC", "Executing ShowOverview");
                context.ShowOverview();
                break;
            case CmdSettings:
                Logger.Log("IPC", "Executing ShowSettings");
                context.ShowSettings();
                break;
            case CmdShutdown:
                Logger.Log("IPC", "Executing ShutdownForUpdate");
                context.ShutdownForUpdate();
                break;
            default:
                Logger.Log("IPC", $"Unknown IPC command: '{command}' — defaulting to ShowOverview");
                context.ShowOverview();
                break;
        }
    }

    private static void HandleUnhandledException(string source, Exception? ex)
    {
        if (ex == null) return;

        Logger.LogException($"CRITICAL_{source}", ex);

        string fingerprint = $"{ex.GetType().FullName}:{ex.Message}:{ex.StackTrace?.Split('\n').FirstOrDefault() ?? ""}";

        if (_currentPhase == AppLifecyclePhase.Starting)
        {
            string message = $"Произошла ошибка запуска ({source}):\n\n{ex.Message}\n\nПодробная информация записана в лог-файл:\n{Logger.LogFilePath}";
            MessageBox.Show(message, "AJAZZ Battery Monitor — Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
        else
        {
            if (ShownErrorFingerprints.TryAdd(fingerprint, true))
            {
                string message = $"В компоненте интерфейса произошла ошибка ({source}):\n\n{ex.Message}\n\nПриложение продолжит работу. Лог записан в:\n{Logger.LogFilePath}";
                MessageBox.Show(message, "AJAZZ Battery Monitor — Ошибка интерфейса", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
