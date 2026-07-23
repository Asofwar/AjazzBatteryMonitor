namespace AjazzBattery.Core.Time;

public sealed class FakeClock : IClock
{
    private DateTimeOffset _utcNow;
    private TimeSpan _localOffset;

    public DateTimeOffset UtcNow => _utcNow;
    public DateTimeOffset LocalNow => _utcNow.ToOffset(_localOffset);

    public FakeClock(DateTimeOffset utcNow, TimeSpan? localOffset = null)
    {
        _utcNow = utcNow;
        _localOffset = localOffset ?? TimeSpan.FromHours(3);
    }

    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
    public void SetLocalOffset(TimeSpan offset) => _localOffset = offset;

    public DateTimeOffset ToLocal(DateTimeOffset value)
        => value.ToUniversalTime().ToOffset(_localOffset);

    public string FormatRelativeTime(DateTimeOffset timestamp)
    {
        var age = _utcNow - timestamp.ToUniversalTime();
        if (age.TotalSeconds < 0) age = TimeSpan.Zero;

        if (age.TotalSeconds < 10) return "Только что";
        if (age.TotalSeconds < 60) return $"{(int)age.TotalSeconds} сек назад";
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes} мин назад";

        var localTimestamp = ToLocal(timestamp);
        if (localTimestamp.Date == LocalNow.Date) return localTimestamp.ToString("HH:mm:ss");

        return localTimestamp.ToString("dd.MM.yyyy HH:mm");
    }
}
