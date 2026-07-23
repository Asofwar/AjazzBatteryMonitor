using System.Windows.Forms;
using AjazzBattery.Core;

namespace AjazzBattery.App;

public sealed class WindowsNotificationService : INotificationService
{
    private readonly NotifyIcon _notifyIcon;

    public WindowsNotificationService(NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));
    }

    public void NotifyLowBattery(int percentage, string model)
    {
        _notifyIcon.ShowBalloonTip(
            5000,
            "Низкий уровень заряда",
            $"{model}: осталось {percentage}% заряда батареи.",
            ToolTipIcon.Warning
        );
    }

    public void NotifyChargingStarted(string model)
    {
        _notifyIcon.ShowBalloonTip(
            4000,
            "Зарядка началась",
            $"{model} подключена к источнику питания.",
            ToolTipIcon.Info
        );
    }

    public void NotifyChargingCompleted(string model)
    {
        _notifyIcon.ShowBalloonTip(
            4000,
            "Зарядка завершена",
            $"{model} полностью заряжена (100%).",
            ToolTipIcon.Info
        );
    }

    public void NotifyLongDisconnection(string model)
    {
        _notifyIcon.ShowBalloonTip(
            5000,
            "Устройство отключено",
            $"{model} длительное время недоступна.",
            ToolTipIcon.Info
        );
    }

    public void NotifyDeviceConflict(string message)
    {
        _notifyIcon.ShowBalloonTip(
            5000,
            "Конфликт HID-интерфейса",
            message,
            ToolTipIcon.Warning
        );
    }
}
