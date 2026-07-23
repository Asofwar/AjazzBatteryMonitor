namespace AjazzBattery.Core.Notifications;

public sealed record BatteryNotificationSettings
{
    public bool NotificationsEnabled { get; set; } = true;
    public List<int> Thresholds { get; set; } = new() { 20, 10, 5 };
    public bool CriticalReminderEnabled { get; set; } = true;
    public int CriticalReminderIntervalMinutes { get; set; } = 30;
    public bool NotifyChargingStarted { get; set; } = true;
    public bool NotifyFullyCharged { get; set; } = true;
    public bool PlaySound { get; set; } = true;
    public int HysteresisMargin { get; set; } = 5;

    public void ValidateAndSanitize()
    {
        Thresholds = Thresholds
            .Where(t => t >= 1 && t <= 99)
            .Distinct()
            .OrderByDescending(t => t)
            .ToList();

        if (Thresholds.Count == 0)
        {
            Thresholds = new List<int> { 20, 10, 5 };
        }

        if (CriticalReminderIntervalMinutes < 15) CriticalReminderIntervalMinutes = 15;
        if (HysteresisMargin < 1) HysteresisMargin = 5;
    }
}
