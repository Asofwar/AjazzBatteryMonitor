using AjazzBattery.Core;

namespace AjazzBattery.Devices;

public sealed class DeviceProfileRegistry
{
    private static readonly byte[] StandardBatteryRequest = CreateBatteryRequest();

    public IReadOnlyList<AjazzDeviceProfile> Profiles { get; }

    public DeviceProfileRegistry()
    {
        Profiles = new List<AjazzDeviceProfile>
        {
            // Confirmed AJAZZ AJ179 APEX Profiles
            new(
                Model: "AJAZZ AJ179 APEX (2.4G Receiver)",
                VendorId: 0x3151,
                ProductId: 0x402D,
                UsagePage: 0xFF00,
                Usage: 0x0001,
                ReportId: 0x00,
                BatteryRequest: StandardBatteryRequest,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.Wireless24G,
                IsConfirmed: true
            ),
            new(
                Model: "AJAZZ AJ179 APEX (Dock Station)",
                VendorId: 0x3151,
                ProductId: 0x5007,
                UsagePage: 0xFF00,
                Usage: 0x0001,
                ReportId: 0x00,
                BatteryRequest: StandardBatteryRequest,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.DockStation,
                IsConfirmed: true
            ),
            new(
                Model: "AJAZZ AJ179 APEX (USB Cable)",
                VendorId: 0x3151,
                ProductId: 0x502D,
                UsagePage: 0xFF00,
                Usage: 0x0001,
                ReportId: 0x00,
                BatteryRequest: StandardBatteryRequest,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.UsbCable,
                IsConfirmed: true
            ),
            new(
                Model: "AJAZZ AJ179 APEX (2.4G Alt)",
                VendorId: 0x3151,
                ProductId: 0x5008,
                UsagePage: 0xFF00,
                Usage: 0x0001,
                ReportId: 0x00,
                BatteryRequest: StandardBatteryRequest,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.Wireless24G,
                IsConfirmed: true
            ),
            // Experimental Profiles
            new(
                Model: "AJAZZ AJ159 Pro (Experimental)",
                VendorId: 0x3151,
                ProductId: 0x4020,
                UsagePage: 0xFF00,
                Usage: 0x0001,
                ReportId: 0x00,
                BatteryRequest: StandardBatteryRequest,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.Wireless24G,
                IsConfirmed: false
            )
        };
    }

    public AjazzDeviceProfile? FindMatchingProfile(DeviceDescriptor descriptor)
    {
        return Profiles.FirstOrDefault(p =>
            p.VendorId == descriptor.VendorId &&
            p.ProductId == descriptor.ProductId
        );
    }

    private static byte[] CreateBatteryRequest()
    {
        var req = new byte[64];
        req[0] = 0x20; // Battery Query Opcode
        req[1] = 0x01; // Sub-command
        return req;
    }
}
