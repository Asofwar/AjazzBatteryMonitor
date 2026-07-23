using AjazzBattery.Core;

namespace AjazzBattery.Devices;

public sealed class AjazzMouseBatteryProvider : IMouseBatteryProvider
{
    private readonly IHidTransport _transport;
    private readonly DeviceProfileRegistry _registry;

    public string ProviderId => "AjazzYichipProvider";

    public AjazzMouseBatteryProvider(IHidTransport transport, DeviceProfileRegistry registry)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public bool CanHandle(DeviceDescriptor device)
    {
        return _registry.FindMatchingProfile(device) != null || device.VendorId == 0x3151;
    }

    public async Task<BatteryStatus> ReadStatusAsync(
        DeviceDescriptor device,
        CancellationToken cancellationToken)
    {
        var profile = _registry.FindMatchingProfile(device);
        byte reportId = profile?.ReportId ?? 0x00;
        byte[] request = profile?.BatteryRequest ?? new byte[] { 0x20, 0x01 };

        try
        {
            byte[] response = await _transport.TransferFeatureReportAsync(
                device,
                reportId,
                request,
                65,
                cancellationToken
            );

            return profile?.Parser(device, response, DateTimeOffset.UtcNow)
                ?? YichipBatteryParser.ParseResponse(device, response, DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("заняло"))
        {
            return BatteryStatus.CreateUnknown(ex.Message);
        }
        catch (Exception ex)
        {
            return BatteryStatus.CreateUnknown($"Ошибка чтения батареи: {ex.Message}");
        }
    }
}
