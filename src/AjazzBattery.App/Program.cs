using System.Runtime.InteropServices;
using System.Windows.Forms;
using AjazzBattery.Core;

namespace AjazzBattery.App;

internal static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    private static void Main(string[] args)
    {
        // 1. Immediate Startup Logging BEFORE any UI, HID, or BLE initialization
        Logger.Log("STARTUP", "Process started");
        string exePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        Logger.Log("STARTUP", "Application version: 1.0.2");
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

        // 3. Single-Instance Mutex Check
        bool allowMultiple = args.Contains("--allow-multiple-instances");
        bool isSmokeTest = args.Contains("--smoke-test");

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
                Logger.Log("MUTEX", "Second instance detected - alerting and exiting.");
                MessageBox.Show(
                    "Приложение AJAZZ Battery Monitor уже запущено в системном трее.",
                    "AJAZZ Battery Monitor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }
            Logger.Log("MUTEX", "Single-instance check passed");
        }

        // 4. Initialize WinForms UI Loop
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplicationContext(isSmokeTest));
        }
        catch (Exception ex)
        {
            HandleUnhandledException("Application.Run", ex);
        }
        finally
        {
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
