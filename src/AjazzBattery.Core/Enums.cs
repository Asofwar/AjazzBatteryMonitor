namespace AjazzBattery.Core;

public enum ConnectionMode
{
    Unknown = 0,
    Wireless24G = 1,
    DockStation = 2,
    UsbCable = 3,
    BluetoothLe = 4
}

public enum StatusConfidence
{
    High = 0,
    Medium = 1,
    Low = 2,
    Stale = 3,
    Unknown = 4
}

public enum ChargingConfidence
{
    Unknown = 0,
    Estimated = 1,
    ProtocolConfirmed = 2,
    HardwareValidated = 3
}

public enum ConfirmationStatus
{
    ConfirmedOnDevice = 0,
    HardwareConfirmedUpstream = 1,
    SourceCodeOnly = 2,
    Experimental = 3,
    Unverified = 4
}

public enum ProviderState
{
    DeviceNotFound = 0,
    ReceiverFound = 1,
    ControlCollectionFound = 2,
    OpeningDevice = 3,
    TelemetryInitializing = 4,
    ReadingBattery = 5,
    Connected = 6,
    MouseSleeping = 7,
    TelemetryNotReady = 8,
    BluetoothPairedButDisconnected = 9,
    AccessDenied = 10,
    InterfaceBusy = 11,
    UnsupportedProtocol = 12,
    InvalidFrame = 13,
    Timeout = 14,
    Error = 15
}
