namespace AjazzBattery.App;

/// <summary>
/// Defines how the application was launched, controlling initial window visibility.
/// </summary>
public enum LaunchMode
{
    /// <summary>Normal launch: open MainForm immediately on the Overview tab.</summary>
    Overview,

    /// <summary>Settings launch: open MainForm immediately on the Settings tab.</summary>
    Settings,

    /// <summary>Background launch (--background): run silently in the tray. No window shown.</summary>
    Background
}
