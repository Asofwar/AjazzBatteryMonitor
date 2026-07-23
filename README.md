# AJAZZ AJ179 APEX Battery Monitor

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011%20x64-blue)
![License](https://img.shields.io/badge/license-MIT-green)

A lightweight, portable Windows system tray application and diagnostic tool (`AjazzBattery.DeviceProbe`) for monitoring the real-time battery percentage of the **AJAZZ AJ179 APEX** (and compatible AJ179 series mice) across 2.4GHz wireless receivers, dock stations, USB cable, and Bluetooth LE connections.

---

## Key Features

- **Automatic Device Discovery**: Detects AJAZZ AJ179 APEX without requiring the official AJAZZ app.
- **Accurate Real-Time Battery Display**: Reads battery percentage from hardware HID feature reports (`0x20` opcode). Never shows fake 100% or 0% values on missing data.
- **Dynamic System Tray Icon**: Renders live battery percentage, charging bolt indicator (⚡), sleep indicator (Zzz), unknown status (?), or disconnection status (X).
- **Adaptive Polling & Sleep Recovery**: Adapts polling intervals (30s active, 15s charging, exponential backoff on sleep/errors, immediate on wake/hot-plug).
- **Non-Elevated Auto-Startup**: Supports optional startup with Windows via Current User Registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), requiring no administrator privileges.
- **Low Battery & Charging Notifications**: Customizable balloon/toast notifications at 20%, 10%, and 5% battery levels.
- **Official App Compatibility**: Gracefully detects locked HID interfaces (`ERROR_SHARING_VIOLATION`) without crashing.
- **Portable Distribution**: Distributed as a single self-contained executable (`.exe`) or portable `.zip`.

---

## Supported Hardware & Identifiers

| Connection Mode | Vendor ID | Product ID | Usage Page | Status |
| :--- | :--- | :--- | :--- | :--- |
| **2.4 GHz Dongle** | `0x3151` | `0x402D` | `0xFF00` / `0xFF01` | Confirmed |
| **Dock Station** | `0x3151` | `0x5007` | `0xFF00` / `0xFF01` | Confirmed |
| **USB Cable** | `0x3151` | `0x502D` | `0xFF00` / `0xFF01` | Confirmed |
| **Alternate Receiver** | `0x3151` | `0x5008` | `0xFF00` / `0xFF01` | Confirmed |
| **Bluetooth LE** | Standard GATT | Service `0x180F` | GATT Char `0x2A19` | Confirmed |

---

## Portable Download & Execution

Executable artifacts are available under `artifacts/`:
- `artifacts/AjazzBatteryMonitor-win-x64.exe` (Single-file portable executable)
- `artifacts/AjazzBatteryMonitor-win-x64-portable.zip` (Portable ZIP archive)

### Quick Run
Simply double-click `AjazzBatteryMonitor-win-x64.exe` to run. No installation or admin rights required.

---

## Diagnostic CLI (`AjazzBattery.DeviceProbe`)

```powershell
# List all connected AJAZZ devices
AjazzBattery.DeviceProbe list

# Inspect HID interfaces and report sizes
AjazzBattery.DeviceProbe inspect

# Read battery status once
AjazzBattery.DeviceProbe read-battery

# Real-time monitoring
AjazzBattery.DeviceProbe monitor

# Capture 60-second dump
AjazzBattery.DeviceProbe capture --duration 60

# Export anonymized diagnostic report
AjazzBattery.DeviceProbe export
```

---

## Building from Source

### Prerequisites
- .NET 8.0 SDK (verified with 8.0.423)
- Windows 10/11 x64

### Build & Package Commands
```powershell
# Restore & build solution
powershell -File scripts/bootstrap.ps1
powershell -File scripts/build.ps1

# Run full test suite
powershell -File scripts/test.ps1

# Publish & package portable binaries
powershell -File scripts/publish.ps1
powershell -File scripts/package.ps1
```

---

## License & Attribution

- **License**: MIT
- See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for licenses of upstream reverse-engineering reference projects (**Aj179PStat**, **Nibble**, **AJAZZ Control Center**, **AJ179 Linux Battery**).
