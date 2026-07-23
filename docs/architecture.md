# System Architecture: AJAZZ Battery Monitor

```
+-------------------------------------------------------------------------+
|                        AjazzBattery.App (WPF/Tray)                      |
| System Tray Icon | Notifications | Settings Window | HKCU AutoStart     |
+-------------------------------------------------------------------------+
       |                                              |
       v                                              v
+-----------------------------+               +---------------------------+
|    AjazzBattery.Core        |               |   AjazzBattery.Storage    |
| BatteryMonitorEngine        |<------------->| AppSettings               |
| IMouseBatteryProvider       |               | BatteryHistoryStorage     |
+-----------------------------+               +---------------------------+
       |
       v
+-----------------------------+
|   AjazzBattery.Devices      |
| DeviceProfileRegistry       |
| YichipBatteryParser         |
+-----------------------------+
       |
       v
+-----------------------------+               +---------------------------+
|     AjazzBattery.Hid        |               |  AjazzBattery.DeviceProbe |
| Win32HidTransport (Win32)   |               | CLI Diagnostic Utility    |
| MockHidTransport (Tests)    |               +---------------------------+
+-----------------------------+
```

## Layer Descriptions
1. **`AjazzBattery.Core`**: Domain models (`BatteryStatus`, `DeviceDescriptor`, `AjazzDeviceProfile`), provider interfaces, notification interface, auto-start interface, and the adaptive polling `BatteryMonitorEngine`.
2. **`AjazzBattery.Hid`**: Windows PnP and HID API integration (`CreateFile`, `HidD_GetFeature`, `HidP_GetCaps`). Includes `MockHidTransport` for robust offline unit testing.
3. **`AjazzBattery.Devices`**: Hardware profile registry for VID `0x3151` across PIDs `0x402D`, `0x5007`, `0x502D`, `0x5008`, and BLE GATT. Features strict range verification in `YichipBatteryParser`.
4. **`AjazzBattery.Storage`**: Persistence layer managing `%AppData%\AjazzBatteryMonitor\settings.json` and `history.json`.
5. **`AjazzBattery.App`**: Main WPF and Windows Tray application hosting GDI+ icon rendering, system notifications, auto-start management, and single-instance mutex control.
6. **`AjazzBattery.DeviceProbe`**: Standalone CLI diagnostic tool.
