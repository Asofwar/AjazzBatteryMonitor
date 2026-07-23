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
    bool IsExperimental = false
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
    string? DiagnosticMessage
)
{
    public static BatteryStatus CreateUnknown(string? diagnosticMessage = null) =>
        new(
            IsPresent: false,
            Percent: null,
            IsCharging: null,
            IsFullyCharged: null,
            IsSleeping: false,
            ConnectionMode: ConnectionMode.Unknown,
            Timestamp: DateTimeOffset.UtcNow,
            Confidence: StatusConfidence.Unknown,
            DiagnosticMessage: diagnosticMessage ?? "Заряд неизвестен"
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
    byte ReportId,
    byte[] BatteryRequest,
    BatteryResponseParser Parser,
    ConnectionMode ConnectionMode,
    bool IsConfirmed = true
);
