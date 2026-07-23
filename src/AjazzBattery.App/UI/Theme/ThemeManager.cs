using Microsoft.Win32;

namespace AjazzBattery.App.UI.Theme;

public static class ThemeManager
{
    private static AppThemeMode _currentMode = AppThemeMode.System;

    public static event EventHandler? ThemeChanged;

    public static AppThemeMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode != value)
            {
                _currentMode = value;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public static ThemePalette Palette => GetEffectivePalette();

    public static bool IsDark => GetEffectiveMode() == AppThemeMode.Dark;

    public static ThemePalette GetEffectivePalette()
    {
        return GetEffectiveMode() == AppThemeMode.Dark ? ThemePalette.Dark : ThemePalette.Light;
    }

    public static AppThemeMode GetEffectiveMode()
    {
        if (_currentMode == AppThemeMode.System)
        {
            return IsWindowsInDarkMode() ? AppThemeMode.Dark : AppThemeMode.Light;
        }
        return _currentMode;
    }

    public static bool IsWindowsInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                object? val = key.GetValue("AppsUseLightTheme");
                if (val is int intVal)
                {
                    return intVal == 0; // 0 = Dark Mode, 1 = Light Mode
                }
            }
        }
        catch { }

        return true; // Default to Dark mode if registry query fails
    }

    public static Color GetBatteryLevelColor(int? percent, bool isCharging, bool isSleeping)
    {
        var pal = Palette;

        if (isSleeping) return pal.MutedText;
        if (!percent.HasValue) return pal.MutedText;
        if (isCharging) return pal.Success;

        int p = percent.Value;
        if (p > 50) return pal.Success;
        if (p > 20) return pal.Accent;
        if (p > 10) return pal.Warning;
        if (p > 5) return pal.Danger;
        return pal.Critical;
    }
}
