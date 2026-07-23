using System.Runtime.InteropServices;
using System.Windows.Forms;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;
using AjazzBattery.Storage;

namespace AjazzBattery.App;

internal static class Program
{
    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _activateEventHandle;

    [STAThread]
    private static void Main(string[] args)
    {
        // 1. Immediate Startup Logging BEFORE any UI, HID, or BLE initialization
        Logger.Log("STARTUP", "Process started");
        string exePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        Logger.Log("STARTUP", "Application version: 1.1.2");
        Logger.Log("STARTUP", $"Executable path: {exePath}");
        Logger.Log("STARTUP", $"Runtime version: {Environment.Version}");
        Logger.Log("STARTUP", $"OS version: {Environment.OSVersion}");
        Logger.Log("STARTUP", $"Process architecture: {RuntimeInformation.ProcessArchitecture}");

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
        bool allowMultiple = args.Contains("--allow-multiple-instances");
        bool isSmokeTest = args.Contains("--smoke-test");
        bool isSmokeTestUi = args.Contains("--smoke-test-ui");
        bool isSmokeTestNotification = args.Contains("--smoke-test-notification");
        bool isDumpUiTree = args.Contains("--dump-ui-tree");

        int? mockBattery = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith("--mock-battery="))
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

        // 4. Single-Instance Mutex & IPC Event Check
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
                Logger.Log("MUTEX", "Second instance detected - signaling active instance via IPC and exiting.");
                try
                {
                    using var evt = EventWaitHandle.OpenExisting(@"Local\AjazzBatteryMonitor_Activate");
                    evt.Set();
                }
                catch { }

                return;
            }
            Logger.Log("MUTEX", "Single-instance check passed");
        }

        // Create IPC Event Handle for activating window when 2nd instance is launched
        _activateEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\AjazzBatteryMonitor_Activate");

        // 5. Initialize WinForms UI Loop
        try
        {
            ApplicationConfiguration.Initialize();
            var context = new TrayApplicationContext(isSmokeTest, isSmokeTestUi, mockBattery);

            // Background IPC listener thread
            _ = Task.Run(() =>
            {
                while (_activateEventHandle.WaitOne())
                {
                    Logger.Log("IPC", "Activation signal received from second instance - showing MainForm.");
                    try
                    {
                        if (Application.OpenForms.Count > 0)
                        {
                            Application.OpenForms[0]?.BeginInvoke(() => context.ShowMainForm());
                        }
                        else
                        {
                            context.ShowMainForm();
                        }
                    }
                    catch { }
                }
            });

            Application.Run(context);
        }
        catch (Exception ex)
        {
            HandleUnhandledException("Application.Run", ex);
        }
        finally
        {
            _activateEventHandle?.Dispose();
            _singleInstanceMutex?.ReleaseMutex();
        }
    }

    private static void HandleUnhandledException(string source, Exception? ex)
    {
        if (ex == null) return;

        Logger.LogException($"CRITICAL_{source}", ex);

        string message = $"Произошла критическая ошибка запуска ({source}):\n\n{ex.Message}\n\nПодробная информация записана в лог-файл:\n{Logger.LogFilePath}";
        MessageBox.Show(message, "AJAZZ Battery Monitor — Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);

        Environment.Exit(1);
    }
}
