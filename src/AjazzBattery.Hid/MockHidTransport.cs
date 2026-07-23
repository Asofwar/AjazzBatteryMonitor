using AjazzBattery.Core;

namespace AjazzBattery.Hid;

public sealed class MockHidTransport : IHidTransport
{
    public List<DeviceDescriptor> ConfiguredDevices { get; set; } = new();
    public Func<DeviceDescriptor, byte[], byte[]>? TransferHandler { get; set; }
    public Exception? SimulateException { get; set; }
    public int DelayMs { get; set; } = 0;
    public bool SetFeatureCalled { get; private set; }
    public byte LastSetFeatureOpcode { get; private set; }

    public MockHidTransport()
    {
        ConfiguredDevices.Add(new DeviceDescriptor(
            DevicePath: "\\\\?\\HID#VID_3151&PID_5007#MockControlCollection",
            ModelName: "AJAZZ AJ179 APEX",
            VendorId: 0x3151,
            ProductId: 0x5007,
            UsagePage: 0xFFFF,
            Usage: 0x0002,
            InterfaceNumber: 1,
            ConnectionMode: ConnectionMode.DockStation,
            FeatureReportByteLength: 65,
            ConfirmationStatus: ConfirmationStatus.ConfirmedOnDevice
        ));
    }

    public async Task<IReadOnlyList<DeviceDescriptor>> EnumerateDevicesAsync(CancellationToken cancellationToken)
    {
        return await EnumerateAllHidCollectionsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceDescriptor>> EnumerateAllHidCollectionsAsync(CancellationToken cancellationToken)
    {
        if (DelayMs > 0) await Task.Delay(DelayMs, cancellationToken);
        if (SimulateException != null) throw SimulateException;
        return ConfiguredDevices;
    }

    public async Task<bool> SetFeatureReportAsync(
        DeviceDescriptor device,
        byte reportId,
        byte[] data,
        CancellationToken cancellationToken)
    {
        if (DelayMs > 0) await Task.Delay(DelayMs, cancellationToken);
        if (SimulateException != null) throw SimulateException;

        SetFeatureCalled = true;
        if (data.Length > 0) LastSetFeatureOpcode = data[0];
        return true;
    }

    public async Task<byte[]> GetFeatureReportAsync(
        DeviceDescriptor device,
        byte reportId,
        int expectedLength,
        CancellationToken cancellationToken)
    {
        if (DelayMs > 0) await Task.Delay(DelayMs, cancellationToken);
        if (SimulateException != null) throw SimulateException;

        if (TransferHandler != null)
        {
            return TransferHandler(device, new byte[] { reportId });
        }

        // Must enforce that SET_FEATURE 0xF7 was called prior to GET_FEATURE 0x05
        if (!SetFeatureCalled || LastSetFeatureOpcode != 0xF7)
        {
            throw new InvalidOperationException("GET_FEATURE 0x05 called without prior SET_FEATURE 0xF7 status poll");
        }

        // Standard hardware frame: 05 00 00 4A 01 01 01 02 (74%)
        var response = new byte[Math.Max(expectedLength, 65)];
        response[0] = reportId; // 0x05
        response[1] = 0x00;
        response[2] = 0x00;
        response[3] = 74;   // 74% battery
        response[4] = 0x01; // Online
        response[5] = 0x01;
        response[6] = 0x01;
        response[7] = 0x02;
        return response;
    }

    public Task<byte[]> TransferFeatureReportAsync(
        DeviceDescriptor device,
        byte reportId,
        byte[] requestBuffer,
        int expectedResponseLength,
        CancellationToken cancellationToken)
    {
        return GetFeatureReportAsync(device, reportId, expectedResponseLength, cancellationToken);
    }

    public void Dispose() { }
}
