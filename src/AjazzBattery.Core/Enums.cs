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
