using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;

namespace AjazzBattery.App.UI.Notifications;

public sealed class WindowsToastNotificationTransport : INotificationTransport
{
    private const string AppUserModelId = "Ajazz.BatteryMonitor";

    public string TransportName => "Windows 10/11 Toast Notifications";

    public Task<bool> ShowNotificationAsync(
        string title,
        string message,
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string xml = $@"
<toast launch=""action=open_main_window"">
    <visual>
        <binding template=""ToastGeneric"">
            <text>{System.Security.SecurityElement.Escape(title)}</text>
            <text>{System.Security.SecurityElement.Escape(message)}</text>
        </binding>
    </visual>
    <audio src=""ms-winsoundevent:Notification.Default"" />
</toast>";

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            var toast = new ToastNotification(xmlDoc);
            toast.ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(5);

            var notifier = ToastNotificationManager.CreateToastNotifier(AppUserModelId);
            notifier.Show(toast);

            Logger.Log("TOAST_OK", $"Toast notification shown: '{title}'");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogException("TOAST_ERR", ex);
            return Task.FromResult(false);
        }
    }
}
