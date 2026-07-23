namespace AjazzBattery.Core;

public static class Logger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AjazzBatteryMonitor",
        "logs"
    );

    private static readonly string StartupLogPath = Path.Combine(LogDir, "startup.log");
    private static readonly object LogLock = new();

    public static string LogFilePath => StartupLogPath;
    public static string LogDirectoryPath => LogDir;

    static Logger()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
        }
        catch { }
    }

    public static void Log(string stage, string message)
    {
        lock (LogLock)
        {
            try
            {
                string line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] [{stage}] {message}{Environment.NewLine}";
                File.AppendAllText(StartupLogPath, line);
            }
            catch { }
        }
    }

    public static void LogException(string stage, Exception ex)
    {
        lock (LogLock)
        {
            try
            {
                string details = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] [ERROR] [{stage}] Exception: {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}StackTrace:{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                if (ex.InnerException != null)
                {
                    details += $"InnerException: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}{Environment.NewLine}{ex.InnerException.StackTrace}{Environment.NewLine}";
                }
                File.AppendAllText(StartupLogPath, details);
            }
            catch { }
        }
    }
}
