namespace AjazzBattery.Core.Notifications;

public interface INotificationTransport
{
    string TransportName { get; }

    Task<bool> ShowNotificationAsync(
        string title,
        string message,
        string category,
        CancellationToken cancellationToken = default);
}
