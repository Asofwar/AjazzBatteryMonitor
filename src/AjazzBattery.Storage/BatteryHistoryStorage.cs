using System.Text.Json;

namespace AjazzBattery.Storage;

public sealed record AppSettings(
    bool AutoStart = true,
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

    public BatteryHistoryStorage()
    {
        _storageDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AjazzBatteryMonitor"
        );
        Directory.CreateDirectory(_storageDir);

        _settingsPath = Path.Combine(_storageDir, "settings.json");
        _historyPath = Path.Combine(_storageDir, "history.json");
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
}
