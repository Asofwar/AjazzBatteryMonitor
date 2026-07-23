using Microsoft.Win32;
using AjazzBattery.Core;

namespace AjazzBattery.App;

public sealed class WindowsAutoStartManager : IAutoStartManager
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "AjazzBatteryMonitor";

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            var val = key?.GetValue(AppName);
            return val != null;
        }
        catch
        {
            return false;
        }
    }

    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key == null) return;

            if (enable)
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? Environment.ProcessPath
                    ?? "";

                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\" --autostart");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch { }
    }
}
