namespace AjazzBattery.Core;

public interface IMouseBatteryProvider
{
    string ProviderId { get; }

    bool CanHandle(DeviceDescriptor device);

    Task<BatteryStatus> ReadStatusAsync(
        DeviceDescriptor device,
        CancellationToken cancellationToken);
}

public interface IHidTransport : IDisposable
{
    Task<IReadOnlyList<DeviceDescriptor>> EnumerateDevicesAsync(CancellationToken cancellationToken);

    Task<byte[]> TransferFeatureReportAsync(
        DeviceDescriptor device,
        byte reportId,
        byte[] requestBuffer,
        int expectedResponseLength,
        CancellationToken cancellationToken);
}

public interface INotificationService
{
    void NotifyLowBattery(int percentage, string model);
    void NotifyChargingStarted(string model);
    void NotifyChargingCompleted(string model);
    void NotifyLongDisconnection(string model);
    void NotifyDeviceConflict(string message);
}

public interface IAutoStartManager
{
    bool IsAutoStartEnabled();
    void SetAutoStart(bool enable);
}
