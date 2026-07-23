# Changelog

## [1.0.0] - 2026-07-23

### Added
- Complete C# .NET 8 solution for AJAZZ AJ179 APEX battery tracking.
- `AjazzBattery.Core`: Domain model, interfaces, adaptive polling engine (`BatteryMonitorEngine`).
- `AjazzBattery.Hid`: Win32 HID API wrapper (`Win32HidTransport`) and mock test transport (`MockHidTransport`).
- `AjazzBattery.Devices`: Device profile registry for VID `0x3151` and PIDs `0x402D`, `0x5007`, `0x502D`, `0x5008`, with Yichip battery response parser.
- `AjazzBattery.Storage`: Persistence for settings and local history log.
- `AjazzBattery.App`: WPF/Windows System Tray application with dynamic GDI+ icon rendering, context menu, toast notifications, and HKCU auto-start.
- `AjazzBattery.DeviceProbe`: Standalone CLI diagnostic utility.
- Full xUnit unit and integration test suite (`tests/`).
- Portable publish scripts generating `artifacts/AjazzBatteryMonitor-win-x64.exe` and `artifacts/AjazzBatteryMonitor-win-x64-portable.zip`.
- Comprehensive documentation suite under `docs/`.
