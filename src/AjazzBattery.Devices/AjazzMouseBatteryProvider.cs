using AjazzBattery.Core;

namespace AjazzBattery.Devices;

public sealed class AjazzMouseBatteryProvider : IMouseBatteryProvider
{
    private readonly IHidTransport _transport;
    private readonly DeviceProfileRegistry _registry;

    public string ProviderId => "AjazzYichipHardwareProvider";

    public AjazzMouseBatteryProvider(IHidTransport transport, DeviceProfileRegistry registry)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public bool CanHandle(DeviceDescriptor device)
    {
        return device.VendorId == 0x3151 || device.VendorId == 0x248A || device.VendorId == 0x249A;
    }

    public async Task<BatteryStatus> ReadStatusAsync(
        DeviceDescriptor device,
        CancellationToken cancellationToken)
    {
        // Skip standard mouse input collection (UsagePage 0x0001 / Usage 0x0002)
        if (device.UsagePage == 0x0001 && device.Usage == 0x0002)
        {
            return BatteryStatus.CreateUnknown("Пропущена стандартная коллекция мыши 0x0001/0x0002", ProviderState.UnsupportedProtocol);
        }

        var profile = _registry.FindMatchingProfile(device);
        byte queryReportId = profile?.QueryReportId ?? 0x00;
        byte queryOpcode = profile?.QueryOpcode ?? 0xF7;
        byte responseReportId = profile?.ResponseReportId ?? 0x05;

        int[] retryDelaysMs = { 0, 100, 300 };

        for (int attempt = 0; attempt < retryDelaysMs.Length; attempt++)
        {
            if (retryDelaysMs[attempt] > 0)
            {
                await Task.Delay(retryDelaysMs[attempt], cancellationToken);
            }

            try
            {
                // 1. Send status poll command via SET_FEATURE (Report ID 0x00, Opcode 0xF7)
                bool setSuccess = await _transport.SetFeatureReportAsync(
                    device,
                    queryReportId,
                    new byte[] { queryOpcode },
                    cancellationToken
                );

                if (!setSuccess)
                {
                    Logger.Log("HID_POLL", $"Attempt {attempt + 1}: SET_FEATURE 0x{queryOpcode:X2} failed for {device.DevicePath}");
                    continue;
                }

                // 2. Hardware delay ~30ms as required by CompX/Yichip controller spec
                await Task.Delay(30, cancellationToken);

                // 3. Query response frame via GET_FEATURE (Report ID 0x05)
                byte[] rawFrame = await _transport.GetFeatureReportAsync(
                    device,
                    responseReportId,
                    65,
                    cancellationToken
                );

                var status = YichipBatteryParser.ParseResponse(device, rawFrame, DateTimeOffset.UtcNow);
                if (status.Percent.HasValue)
                {
                    Logger.Log("HID_POLL_OK", $"Successful hardware battery read: {status.Percent}% on attempt {attempt + 1}");
                    return status;
                }
                else if (status.State == ProviderState.TelemetryNotReady)
                {
                    Logger.Log("HID_POLL_RETRY", $"Attempt {attempt + 1}: Telemetry not ready yet (zero frame). Retrying...");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("HID_POLL_EX", $"Attempt {attempt + 1} exception: {ex.Message}");
            }
        }

        return BatteryStatus.CreateUnknown("Не удалось получить валидный кадр 0x05 от устройства после 3 попыток", ProviderState.TelemetryNotReady);
    }
}
