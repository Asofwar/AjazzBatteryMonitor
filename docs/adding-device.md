# Adding a New Device Profile

To add support for a new AJAZZ mouse model or variant:

1. Open `src/AjazzBattery.Devices/DeviceProfileRegistry.cs`.
2. Add a new `AjazzDeviceProfile` entry:
```csharp
new AjazzDeviceProfile(
    Model: "AJAZZ AJ159 Apex",
    VendorId: 0x3151,
    ProductId: 0x4030,
    UsagePage: 0xFF00,
    Usage: 0x0001,
    ReportId: 0x00,
    BatteryRequest: StandardBatteryRequest,
    Parser: YichipBatteryParser.ParseResponse,
    ConnectionMode: ConnectionMode.Wireless24G,
    IsConfirmed: true
)
```
3. Rebuild and run tests:
```powershell
dotnet test --configuration Release
```
