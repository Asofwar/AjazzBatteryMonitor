# AJAZZ Battery Monitor

AJAZZ Battery Monitor is a Windows 10/11 x64 tray application for monitoring the battery of an AJAZZ AJ179 APEX mouse. It tries Bluetooth LE Battery Service (`0x180F` / `0x2A19`) first and only uses the HID Feature Report fallback for an explicitly approved control collection.

## Status

The target version is `1.2.1`. Version metadata is centralized in `Directory.Build.props`.

Hardware support is intentionally conservative: a successful real-device read is required before claiming that a transport is supported. CI runs non-hardware tests only; it cannot validate a physical mouse.

## Build and test

Requirements: Windows 10/11 and .NET SDK 8.

```powershell
dotnet restore AjazzBattery.sln --locked-mode
dotnet build AjazzBattery.sln --configuration Release --no-restore
dotnet test AjazzBattery.sln --configuration Release --no-build --filter "Category!=Hardware"
```

To create a self-contained portable executable:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

The optional per-user installer is built with Inno Setup 6:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
```

It installs to `%LocalAppData%\Programs\AJAZZ Battery Monitor` without UAC. User settings and history remain under `%LocalAppData%\AjazzBatteryMonitor` and are preserved by default on upgrade and uninstall.

## Privacy

Do not publish Bluetooth MAC addresses, full BLE identifiers, HID paths, serial numbers, local paths, logs, diagnostic archives, settings or battery history in issues, releases or pull requests. The project contains no screenshots, images or UI captures.

## Code signing

Release binaries are unsigned unless a release workflow receives the configured signing secrets. Unsigned Windows executables can show a SmartScreen warning.

## License

Licensed under [GPL-3.0-or-later](LICENSE), a dependency-compatible copyleft license. This is an independent implementation with documented protocol provenance; the audit is retained for traceability and does not remove the GPL obligations.
