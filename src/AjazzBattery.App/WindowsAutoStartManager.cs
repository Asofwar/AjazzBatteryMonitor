using Microsoft.Win32;
using AjazzBattery.Core;

namespace AjazzBattery.App;

/// <summary>
/// Manages the Windows autostart registry entry for the current user (HKCU Run).
/// Registry value: HKCU\Software\Microsoft\Windows\CurrentVersion\Run
/// Value name: AjazzBatteryMonitor
/// Value data: "&lt;path-to-exe&gt;" --background
///
/// The --background flag ensures Windows autostart runs silently in the tray
/// without opening the main window. A manual launch (no args) always opens the window.
/// </summary>
public sealed class WindowsAutoStartManager : IAutoStartManager
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string AppName = "AjazzBatteryMonitor";
    private const string BackgroundArg = "--background";

    /// <summary>
    /// Returns true if a valid autostart entry exists with the --background argument.
    /// An entry without --background is considered outdated and NOT counted as enabled.
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            var val = key?.GetValue(AppName) as string;
            return val != null && val.Contains(BackgroundArg, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current raw registry value for the autostart entry, or null if absent.
    /// </summary>
    public string? GetAutoStartValue()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            return key?.GetValue(AppName) as string;
        }
        catch { return null; }
    }

    /// <summary>
    /// Enables or disables the autostart registry entry.
    /// When enabling, always writes: "&lt;exe-path&gt;" --background
    /// </summary>
    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            if (key == null)
            {
                Logger.Log("AUTOSTART", "Failed to open HKCU Run key for writing");
                return;
            }

            if (enable)
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? Environment.ProcessPath
                    ?? "";

                if (!string.IsNullOrEmpty(exePath))
                {
                    // Format: "<path with spaces>" --background
                    // The closing quote is BEFORE --background so Windows correctly handles paths with spaces
                    string value = $"\"{exePath}\" {BackgroundArg}";
                    key.SetValue(AppName, value);
                    Logger.Log("AUTOSTART", $"Autostart enabled: {value}");
                }
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                Logger.Log("AUTOSTART", "Autostart disabled (registry entry removed)");
            }
        }
        catch (Exception ex)
        {
            Logger.Log("AUTOSTART", $"SetAutoStart({enable}) failed: {ex.Message}");
        }
    }
}
