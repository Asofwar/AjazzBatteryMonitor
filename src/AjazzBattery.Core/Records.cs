namespace AjazzBattery.Core;

public sealed record DeviceDescriptor(
    string DevicePath,
    string ModelName,
    ushort VendorId,
    ushort ProductId,
    ushort UsagePage,
    ushort Usage,
    int InterfaceNumber,
    ConnectionMode ConnectionMode,
    ushort FeatureReportByteLength = 65,
    ushort InputReportByteLength = 0,
    ushort OutputReportByteLength = 0,
    string Manufacturer = "",
    string Product = "",
    bool CanOpen = true,
    int LastWin32Error = 0,
    ConfirmationStatus ConfirmationStatus = ConfirmationStatus.Unverified
);

public sealed record BatteryStatus(
    bool IsPresent,
    int? Percent,
    bool? IsCharging,
    bool? IsFullyCharged,
    bool IsSleeping,
    ConnectionMode ConnectionMode,
    DateTimeOffset Timestamp,
    StatusConfidence Confidence,
    string? DiagnosticMessage,
    ProviderState State = ProviderState.DeviceNotFound,
    string ActiveTransport = "None",
    byte[]? RawFrameHex = null
)
{
    public static BatteryStatus CreateUnknown(string? diagnosticMessage = null, ProviderState state = ProviderState.DeviceNotFound) =>
        new(
            IsPresent: false,
            Percent: null,
            IsCharging: null,
            IsFullyCharged: null,
            IsSleeping: false,
            ConnectionMode: ConnectionMode.Unknown,
            Timestamp: DateTimeOffset.UtcNow,
            Confidence: StatusConfidence.Unknown,
            DiagnosticMessage: diagnosticMessage ?? "Заряд неизвестен",
            State: state,
            ActiveTransport: "None"
        );
}

public delegate BatteryStatus BatteryResponseParser(
    DeviceDescriptor device,
    byte[] rawResponse,
    DateTimeOffset timestamp
);

public sealed record AjazzDeviceProfile(
    string Model,
    ushort VendorId,
    ushort ProductId,
    ushort UsagePage,
    ushort? Usage,
    byte QueryReportId,
    byte QueryOpcode,
    byte ResponseReportId,
    BatteryResponseParser Parser,
    ConnectionMode ConnectionMode,
    ConfirmationStatus ConfirmationStatus
);
