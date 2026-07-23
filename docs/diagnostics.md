# Diagnostics & DeviceProbe Guide

`AjazzBattery.DeviceProbe` is a standalone CLI tool for inspecting, testing, and debugging AJAZZ mouse HID communication without running the full tray UI.

## Usage Commands

### 1. List Connected AJAZZ HID Devices
```powershell
AjazzBattery.DeviceProbe list
```

### 2. Inspect VID/PID, Usage Pages, and Report Sizes
```powershell
AjazzBattery.DeviceProbe inspect
```

### 3. One-Shot Battery Read
```powershell
AjazzBattery.DeviceProbe read-battery
```

### 4. Real-Time Status Monitoring
```powershell
AjazzBattery.DeviceProbe monitor
```

### 5. Packet Capture
```powershell
AjazzBattery.DeviceProbe capture --duration 60
```

### 6. Anonymous Diagnostic Export
```powershell
AjazzBattery.DeviceProbe export
```
