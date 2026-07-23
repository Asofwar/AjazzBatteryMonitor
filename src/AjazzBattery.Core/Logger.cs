using System.Text.RegularExpressions;

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

    private static readonly Regex MacPattern = new(@"([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})", RegexOptions.Compiled);
    private static readonly Regex BleIdPattern = new(@"BluetoothLE#BluetoothLE[a-zA-Z0-9:\-\\\?\#]+", RegexOptions.Compiled);

    static Logger()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
        }
        catch { }
    }

    public static string RedactSensitiveData(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = BleIdPattern.Replace(text, "[id redacted]");
        text = MacPattern.Replace(text, "[mac redacted]");
        return text;
    }

    public static void Log(string stage, string message)
    {
        lock (LogLock)
        {
            try
            {
                string safeMsg = RedactSensitiveData(message);
                string line = $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] [{stage}] {safeMsg}{Environment.NewLine}";
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
                string safeMsg = RedactSensitiveData(ex.Message);
                string details = $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] [ERROR] [{stage}] Exception: {ex.GetType().FullName}: {safeMsg}{Environment.NewLine}StackTrace:{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
                if (ex.InnerException != null)
                {
                    string safeInner = RedactSensitiveData(ex.InnerException.Message);
                    details += $"InnerException: {ex.InnerException.GetType().FullName}: {safeInner}{Environment.NewLine}{ex.InnerException.StackTrace}{Environment.NewLine}";
                }
                File.AppendAllText(StartupLogPath, details);
            }
            catch { }
        }
    }
}
