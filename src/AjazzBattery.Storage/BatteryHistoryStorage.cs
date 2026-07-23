using System.Text.Json;
using AjazzBattery.Core.Notifications;

namespace AjazzBattery.Storage;

public sealed record AppSettings(
    bool AutoStart = false,
    int LowBatteryThreshold = 20,
    int RefreshIntervalSeconds = 30,
    bool EnableNotifications = true,
    bool EnableExperimentalProfiles = false
);

public sealed record BatteryHistoryEntry(
    DateTimeOffset Timestamp,
    int? Percent,
    bool? IsCharging,
    string ConnectionMode
);

public sealed class BatteryHistoryStorage
{
    private readonly string _storageDir;
    private readonly string _settingsPath;
    private readonly string _historyPath;
    private readonly string _notificationSettingsPath;
    private readonly string _notificationStatePath;

    public BatteryHistoryStorage()
    {
        _storageDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AjazzBatteryMonitor"
        );
        Directory.CreateDirectory(_storageDir);

        _settingsPath = Path.Combine(_storageDir, "settings.json");
        _historyPath = Path.Combine(_storageDir, "history.json");
        _notificationSettingsPath = Path.Combine(_storageDir, "notification-settings.json");
        _notificationStatePath = Path.Combine(_storageDir, "notification-state.json");
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }

    public BatteryNotificationSettings LoadNotificationSettings()
        => LoadJson<BatteryNotificationSettings>(_notificationSettingsPath) ?? new BatteryNotificationSettings();

    public BatteryNotificationState LoadNotificationState()
        => LoadJson<BatteryNotificationState>(_notificationStatePath) ?? new BatteryNotificationState();

    public void SaveNotificationState(BatteryNotificationSettings settings, BatteryNotificationState state)
    {
        SaveJson(_notificationSettingsPath, settings);
        SaveJson(_notificationStatePath, state);
    }

    public void AppendHistory(int? percent, bool? isCharging, string connectionMode)
    {
        try
        {
            var history = LoadHistory();
            history.Add(new BatteryHistoryEntry(DateTimeOffset.UtcNow, percent, isCharging, connectionMode));

            // Keep max 500 entries
            if (history.Count > 500)
            {
                history = history.Skip(history.Count - 500).ToList();
            }

            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
        }
        catch { }
    }

    public List<BatteryHistoryEntry> LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                var json = File.ReadAllText(_historyPath);
                return JsonSerializer.Deserialize<List<BatteryHistoryEntry>>(json) ?? new List<BatteryHistoryEntry>();
            }
        }
        catch { }
        return new List<BatteryHistoryEntry>();
    }

    private static T? LoadJson<T>(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) : default;
        }
        catch
        {
            return default;
        }
    }

    private static void SaveJson<T>(string path, T value)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
        }
    }
}
