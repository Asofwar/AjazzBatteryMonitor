using AjazzBattery.Core;

namespace AjazzBattery.Hid;

public sealed class MockHidTransport : IHidTransport
{
    public List<DeviceDescriptor> ConfiguredDevices { get; set; } = new();
    public Func<DeviceDescriptor, byte[], byte[]>? TransferHandler { get; set; }
    public Exception? SimulateException { get; set; }
    public int DelayMs { get; set; } = 0;

    public MockHidTransport()
    {
        ConfiguredDevices.Add(new DeviceDescriptor(
            DevicePath: "\\\\?\\HID#VID_3151&PID_402D#MockDevice",
            ModelName: "AJAZZ AJ179 APEX",
            VendorId: 0x3151,
            ProductId: 0x402D,
            UsagePage: 0xFF00,
            Usage: 0x0001,
            InterfaceNumber: 1,
            ConnectionMode: ConnectionMode.Wireless24G,
            IsExperimental: false
        ));
    }

    public async Task<IReadOnlyList<DeviceDescriptor>> EnumerateDevicesAsync(CancellationToken cancellationToken)
    {
        if (DelayMs > 0) await Task.Delay(DelayMs, cancellationToken);
        if (SimulateException != null) throw SimulateException;
        return ConfiguredDevices;
    }

    public async Task<byte[]> TransferFeatureReportAsync(
        DeviceDescriptor device,
        byte reportId,
        byte[] requestBuffer,
        int expectedResponseLength,
        CancellationToken cancellationToken)
    {
        if (DelayMs > 0) await Task.Delay(DelayMs, cancellationToken);
        if (SimulateException != null) throw SimulateException;

        if (TransferHandler != null)
        {
            return TransferHandler(device, requestBuffer);
        }

        // Default mock response: 74% battery
        var response = new byte[expectedResponseLength];
        response[0] = reportId;
        response[1] = 0x20;
        response[2] = 0x01;
        response[3] = 0x01; // Not charging
        response[4] = 74;   // 74%
        return response;
    }

    public void Dispose() { }
}
