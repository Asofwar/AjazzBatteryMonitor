namespace AjazzBattery.Core.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset LocalNow { get; }
    DateTimeOffset ToLocal(DateTimeOffset value);
    string FormatRelativeTime(DateTimeOffset timestamp);
}
