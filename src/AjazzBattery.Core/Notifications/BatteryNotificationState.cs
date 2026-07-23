namespace AjazzBattery.Core.Notifications;

public sealed record BatteryNotificationState
{
    public int? LastPercent { get; set; }
    public DateTimeOffset? LastNotificationTime { get; set; }
    public HashSet<int> TriggeredThresholds { get; set; } = new();
    public DateTimeOffset? LastCriticalReminderTime { get; set; }
    public bool? WasCharging { get; set; }
    public bool InitialReadingProcessed { get; set; } = false;
    public int? PendingAnomalyPercent { get; set; }
}
