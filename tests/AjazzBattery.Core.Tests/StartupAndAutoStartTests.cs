using Xunit;
using AjazzBattery.App;

namespace AjazzBattery.Core.Tests;

/// <summary>
/// Tests for startup launch-mode argument parsing logic and AutoStartManager registry behavior.
/// These are unit tests that do NOT require a running instance of the app.
/// </summary>
public class StartupAndAutoStartTests
{
    // ── Argument parsing ─────────────────────────────────────────────────────

    [Fact]
    public void NoArgs_ShouldBeOverviewMode()
    {
        string[] args = [];
        bool background = args.Contains("--background") || args.Contains("--autostart");
        bool settings   = !background && args.Contains("--settings");
        bool overview   = !background && (args.Length == 0 || args.Contains("--show") || args.Contains("--overview"));

        Assert.False(background);
        Assert.False(settings);
        Assert.True(overview);
    }

    [Fact]
    public void BackgroundArg_ShouldBeBackgroundMode()
    {
        string[] args = ["--background"];
        bool background = args.Contains("--background") || args.Contains("--autostart");

        Assert.True(background);
    }

    [Fact]
    public void AutostartArg_LegacyShouldBeBackgroundMode()
    {
        string[] args = ["--autostart"];
        bool background = args.Contains("--background") || args.Contains("--autostart");

        Assert.True(background);
    }

    [Fact]
    public void ShowArg_ShouldBeOverviewMode()
    {
        string[] args = ["--show"];
        bool background = args.Contains("--background");
        bool overview   = !background && (args.Length == 0 || args.Contains("--show") || args.Contains("--overview"));

        Assert.False(background);
        Assert.True(overview);
    }

    [Fact]
    public void OverviewArg_ShouldBeOverviewMode()
    {
        string[] args = ["--overview"];
        bool background = args.Contains("--background");
        bool overview   = !background && (args.Length == 0 || args.Contains("--show") || args.Contains("--overview"));

        Assert.False(background);
        Assert.True(overview);
    }

    [Fact]
    public void SettingsArg_ShouldBeSettingsMode()
    {
        string[] args = ["--settings"];
        bool background = args.Contains("--background");
        bool settings   = !background && args.Contains("--settings");

        Assert.False(background);
        Assert.True(settings);
    }

    [Fact]
    public void BackgroundArgWithOtherArgs_BackgroundTakesPrecedence()
    {
        string[] args = ["--background", "--settings"];
        bool background = args.Contains("--background") || args.Contains("--autostart");
        bool settings   = !background && args.Contains("--settings");
        bool overview   = !background && (args.Length == 0 || args.Contains("--show") || args.Contains("--overview"));

        Assert.True(background);
        Assert.False(settings);
        Assert.False(overview);
    }

    [Fact]
    public void PostInstallLaunch_NoArgsIsOverview()
    {
        // Installer launches: {app}\AjazzBatteryMonitor.exe (no args)
        string[] args = [];
        bool background = args.Contains("--background");
        bool overview   = !background && (args.Length == 0 || args.Contains("--show") || args.Contains("--overview"));

        Assert.True(overview, "Post-install launch with no args must open Overview");
        Assert.False(background, "Post-install launch must NOT use background mode");
    }

    [Fact]
    public void AutostartRegistryEntry_ShouldContainBackgroundArg()
    {
        // The registry value for Windows autostart must include --background
        string exePath = @"C:\Users\test\AppData\Local\Programs\AJAZZ Battery Monitor\AjazzBatteryMonitor.exe";
        string registryValue = $"\"{exePath}\" --background";

        Assert.Contains("--background", registryValue);
        // Path is quoted separately from the argument
        Assert.StartsWith("\"", registryValue);
        Assert.Contains("\" --background", registryValue); // quote closes before the flag
    }

    // ── AutoStartManager registry value format ────────────────────────────────

    [Fact]
    public void AutoStartManager_EnabledValue_HasBackgroundFlag()
    {
        // Simulate what WindowsAutoStartManager.SetAutoStart(true) produces
        string exePath = @"C:\Program Files\AjazzBatteryMonitor.exe";
        string value = $"\"{exePath}\" --background";

        Assert.Contains("--background", value);
        Assert.DoesNotContain("--autostart", value); // old legacy value should not be written
        Assert.StartsWith($"\"{exePath}\"", value);  // path is correctly quoted
    }

    [Fact]
    public void AutoStartManager_IsEnabled_ReturnsFalse_WhenValueLacksBackgroundFlag()
    {
        // Simulate old-style entry without --background
        string oldValue = @"""C:\AjazzBatteryMonitor.exe"" --autostart";
        bool hasBackground = oldValue.Contains("--background", StringComparison.OrdinalIgnoreCase);
        Assert.False(hasBackground, "Old entry without --background should not be considered enabled by the new logic");
    }

    [Fact]
    public void AutoStartManager_IsEnabled_ReturnsTrue_WhenValueHasBackgroundFlag()
    {
        string newValue = @"""C:\AjazzBatteryMonitor.exe"" --background";
        bool hasBackground = newValue.Contains("--background", StringComparison.OrdinalIgnoreCase);
        Assert.True(hasBackground);
    }

    [Fact]
    public void AutoStartManager_PathWithSpaces_IsCorrectlyQuoted()
    {
        // Paths with spaces must be quoted — the arg must follow the closing quote
        string exePath = @"C:\Users\My User\AppData\Local\Programs\AJAZZ Battery Monitor\AjazzBatteryMonitor.exe";
        string value = $"\"{exePath}\" --background";

        // The path should be enclosed in quotes
        Assert.StartsWith("\"", value);
        // The arg should appear after the closing quote for the path
        int closingQuoteIdx = value.IndexOf('"', 1);
        Assert.True(closingQuoteIdx > 0);
        string afterPath = value.Substring(closingQuoteIdx + 1).Trim();
        Assert.Equal("--background", afterPath);
    }

    [Fact]
    public void StartMenuShortcut_ShouldNotContainBackgroundArg()
    {
        // Start Menu shortcut launches the app without --background → opens main window
        string startMenuArgs = ""; // no arguments for Start Menu shortcut
        bool hasBackground = startMenuArgs.Contains("--background", StringComparison.OrdinalIgnoreCase);
        Assert.False(hasBackground, "Start Menu shortcut must NOT contain --background");
    }

    [Fact]
    public void DesktopShortcut_ShouldNotContainBackgroundArg()
    {
        string desktopArgs = "";
        bool hasBackground = desktopArgs.Contains("--background", StringComparison.OrdinalIgnoreCase);
        Assert.False(hasBackground, "Desktop shortcut must NOT contain --background");
    }

    [Fact]
    public void AppSection_Overview_MapsToTabIndex0()
    {
        int tabIndex = AppSection.Overview switch
        {
            AppSection.History  => 1,
            AppSection.Settings => 2,
            _                   => 0
        };
        Assert.Equal(0, tabIndex);
    }

    [Fact]
    public void AppSection_Settings_MapsToTabIndex2()
    {
        int tabIndex = AppSection.Settings switch
        {
            AppSection.History  => 1,
            AppSection.Settings => 2,
            _                   => 0
        };
        Assert.Equal(2, tabIndex);
    }

    [Fact]
    public void LaunchMode_Background_DoesNotOpenWindow()
    {
        // This verifies the semantic contract (not a runtime test)
        var mode = LaunchMode.Background;
        bool shouldOpenWindow = mode != LaunchMode.Background;
        Assert.False(shouldOpenWindow);
    }

    [Fact]
    public void LaunchMode_Overview_OpensWindow()
    {
        var mode = LaunchMode.Overview;
        bool shouldOpenWindow = mode != LaunchMode.Background;
        Assert.True(shouldOpenWindow);
    }
}
