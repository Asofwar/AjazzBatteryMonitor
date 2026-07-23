namespace AjazzBattery.Core.Time;

public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset LocalNow => DateTimeOffset.Now;

    public DateTimeOffset ToLocal(DateTimeOffset value)
        => value.ToLocalTime();

    public string FormatRelativeTime(DateTimeOffset timestamp)
    {
        var age = UtcNow - timestamp.ToUniversalTime();
        if (age.TotalSeconds < 0) age = TimeSpan.Zero;

        if (age.TotalSeconds < 10) return "Только что";
        if (age.TotalSeconds < 60) return $"{(int)age.TotalSeconds} сек назад";
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes} мин назад";

        var localTimestamp = ToLocal(timestamp);
        if (localTimestamp.Date == LocalNow.Date) return localTimestamp.ToString("HH:mm:ss");

        return localTimestamp.ToString("dd.MM.yyyy HH:mm");
    }
}
