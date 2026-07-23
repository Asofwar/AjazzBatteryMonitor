namespace AjazzBattery.Core.Notifications;

public sealed class FakeNotificationTransport : INotificationTransport
{
    public string TransportName => "Fake Test Transport";

    public List<(string Title, string Message, string Category)> SentNotifications { get; } = new();

    public bool ShouldFail { get; set; } = false;

    public Task<bool> ShowNotificationAsync(
        string title,
        string message,
        string category,
        CancellationToken cancellationToken = default)
    {
        if (ShouldFail) return Task.FromResult(false);

        SentNotifications.Add((title, message, category));
        return Task.FromResult(true);
    }
}
