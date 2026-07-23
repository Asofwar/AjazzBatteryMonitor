using AjazzBattery.Core;

namespace AjazzBattery.Devices;

public sealed class DeviceProfileRegistry
{
    public IReadOnlyList<AjazzDeviceProfile> Profiles { get; }

    public DeviceProfileRegistry()
    {
        Profiles = new List<AjazzDeviceProfile>
        {
            // Hardware confirmed AJAZZ AJ179 APEX Dock Station & Receiver
            new(
                Model: "AJAZZ AJ179 APEX (Dock Station / Receiver)",
                VendorId: 0x3151,
                ProductId: 0x5007,
                UsagePage: 0xFFFF,
                Usage: 0x0002,
                QueryReportId: 0x00,
                QueryOpcode: 0xF7,
                ResponseReportId: 0x05,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.DockStation,
                ConfirmationStatus: ConfirmationStatus.ConfirmedOnDevice
            ),
            new(
                Model: "AJAZZ AJ179 APEX (2.4G Receiver)",
                VendorId: 0x3151,
                ProductId: 0x402D,
                UsagePage: 0xFFFF,
                Usage: 0x0002,
                QueryReportId: 0x00,
                QueryOpcode: 0xF7,
                ResponseReportId: 0x05,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.Wireless24G,
                ConfirmationStatus: ConfirmationStatus.HardwareConfirmedUpstream
            ),
            new(
                Model: "AJAZZ AJ179 APEX (USB Cable)",
                VendorId: 0x3151,
                ProductId: 0x502D,
                UsagePage: 0xFFFF,
                Usage: 0x0002,
                QueryReportId: 0x00,
                QueryOpcode: 0xF7,
                ResponseReportId: 0x05,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.UsbCable,
                ConfirmationStatus: ConfirmationStatus.HardwareConfirmedUpstream
            ),
            new(
                Model: "AJAZZ AJ179 APEX (2.4G Alt)",
                VendorId: 0x3151,
                ProductId: 0x5008,
                UsagePage: 0xFFFF,
                Usage: 0x0002,
                QueryReportId: 0x00,
                QueryOpcode: 0xF7,
                ResponseReportId: 0x05,
                Parser: YichipBatteryParser.ParseResponse,
                ConnectionMode: ConnectionMode.Wireless24G,
                ConfirmationStatus: ConfirmationStatus.SourceCodeOnly
            )
        };
    }

    public AjazzDeviceProfile? FindMatchingProfile(DeviceDescriptor descriptor)
    {
        return Profiles.FirstOrDefault(p =>
            p.VendorId == descriptor.VendorId &&
            p.ProductId == descriptor.ProductId &&
            p.UsagePage == descriptor.UsagePage &&
            p.Usage == descriptor.Usage &&
            p.ConnectionMode == descriptor.ConnectionMode
        );
    }
}
