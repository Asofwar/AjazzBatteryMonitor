# Upstream Repository Comparison & Architecture Decision

## Evaluated Upstream Projects

### 1. Aj179PStat
- **Repository**: `https://github.com/GetTheNya/Aj179PStat`
- **Tech Stack**: C# / Windows Desktop (.NET)
- **License**: no root license file was present in the audited upstream commit; reuse permission is `NOT_PROVEN`
- **Key Features**: Windows Tray icon, PID `0x402D` HID polling, simple percentage display.
- **Strengths**: Lightweight Windows tray foundation.
- **Weaknesses**: Single hardcoded PID (`0x402D`), lacks multi-PID support, missing robust error/sleep fallback, no tests, unmaintained.

### 2. Nibble
- **Repository**: `https://github.com/mahammadismayilov/nibble`
- **Tech Stack**: JavaScript / WebHID API
- **License**: GPL-3.0-or-later
- **Key Features**: Browser-based configuration for AJAZZ mice.
- **Strengths**: Reverse-engineered protocol definitions for various AJAZZ models.
- **Weaknesses**: Requires browser tab to be open; cannot run natively in Windows system tray or handle background sleep recovery seamlessly.

### 3. AJAZZ Control Center
- **Repository**: `https://github.com/Aiacos/ajazz-control-center`
- **Tech Stack**: C++20 / Qt6 / Python out-of-process plugins
- **License**: GPL-3.0
- **Key Features**: Full-fledged RGB, macro, and battery control suite.
- **Strengths**: Multi-device architecture and backend abstraction.
- **Weaknesses**: Heavy footprint (Qt6 + Python), complex UI suite overkill for a tray battery monitor.

### 4. AJ179 Linux Battery
- **Repository**: `https://github.com/Rockeyxx/AJ179-linux-battery`
- **Tech Stack**: C / libusb / Linux daemon
- **License**: MIT
- **Key Features**: Writes percentage to `/tmp` for Waybar / Polybar status bars.
- **Strengths**: Pure protocol querying logic (`0xF7` opcode).
- **Weaknesses**: Linux-only (`/dev/hidraw`), no Windows tray integration, blocking I/O crashes on mouse sleep.

---

## Architectural Decision & Base Choice

### Selected Approach: Custom C# .NET 8 Modular Architecture (`AjazzBattery.*`)
The project evaluates tray and protocol concepts from **Aj179PStat** and protocol facts described by **Nibble** / **AJAZZ Control Center**. This document is an architectural comparison, not a claim of permission to copy source code. Any public distribution requires the provenance and license gate in `docs/reviews/license-audit.md` to be resolved.

1. **`AjazzBattery.Core`**: Domain models (`BatteryStatus`, `DeviceDescriptor`, `AjazzDeviceProfile`), interfaces (`IMouseBatteryProvider`).
2. **`AjazzBattery.Hid`**: Windows Win32 HID API wrapper + Bluetooth GATT provider with safe timeout-bounded async I/O.
3. **`AjazzBattery.Devices`**: Configurable profile manager mapping VIDs (`0x3151`), PIDs (`0x402D`, `0x5007`, `0x502D`, `0x5008`), Report IDs, and parsers.
4. **`AjazzBattery.App`**: WPF/WinForms Windows System Tray app with dynamic GDI tray icons, low-battery notifications, HKCU registry auto-start, and conflict handling.
5. **`AjazzBattery.DeviceProbe`**: Standalone CLI diagnostic tool.
6. **`tests/`**: Unit & Integration tests using `MockHidTransport`.
