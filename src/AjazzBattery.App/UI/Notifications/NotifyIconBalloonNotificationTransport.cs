using System.Windows.Forms;
using AjazzBattery.Core.Notifications;

namespace AjazzBattery.App.UI.Notifications;

public sealed class NotifyIconBalloonNotificationTransport : INotificationTransport
{
    private readonly NotifyIcon _notifyIcon;

    public string TransportName => "Windows Balloon Tip (Tray)";

    public NotifyIconBalloonNotificationTransport(NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));
    }

    public Task<bool> ShowNotificationAsync(
        string title,
        string message,
        string category,
        CancellationToken cancellationToken = default)
    {
        ToolTipIcon icon = category.Contains("critical") || category.Contains("5")
            ? ToolTipIcon.Error
            : (category.Contains("10") || category.Contains("20") ? ToolTipIcon.Warning : ToolTipIcon.Info);

        _notifyIcon.ShowBalloonTip(4000, title, message, icon);
        return Task.FromResult(true);
    }
}
