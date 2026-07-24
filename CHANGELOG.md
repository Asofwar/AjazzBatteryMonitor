# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses semantic versioning.

## [1.3.0] - 2026-07-24

### Added

- **Custom application icon** — original GDI+-rendered mouse-with-battery design embedded as a multi-resolution ICO (16–256 px), no third-party assets
- **Startup UX**: normal launch (no args) and post-install launch now open the main window immediately on the **Overview** tab
- **`--background` flag**: Windows autostart always runs with `--background` — silently in the system tray, no window shown
- **Named pipe IPC** replaces `EventWaitHandle` for second-instance communication; sends `ShowOverview` or `ShowSettings` commands
- **`AppSection` enum** and `MainForm.SelectSection()` for programmatic tab navigation
- **`LaunchMode` enum** (`Overview`, `Settings`, `Background`) for type-safe launch mode handling
- **Close-to-tray hint**: first window close shows a one-time balloon: "application continues running in the system tray"
- **Installer autostart task** (`checkedonce`, checked by default): writes `"<exe>" --background` to HKCU Run
- **19 new unit tests** covering argument parsing, launch modes, autostart value format, section-to-tab mapping

### Changed

- `WindowsAutoStartManager.SetAutoStart(true)` now always writes `--background` as the launch argument
- `WindowsAutoStartManager.IsAutoStartEnabled()` now validates that the HKCU Run value contains `--background` (rejects old entries)
- `MainForm` close button hides to tray (handled in `TrayApplicationContext`); `ShowInTaskbar=true`
- `TrayApplicationContext` replaced generic `ShowMainForm()` with `ShowOverview()` and `ShowSettings()` 
- Win32 `SetForegroundWindow` called to reliably bring window to front on repeated launch
- Assembly output name changed to `AjazzBatteryMonitor` (EXE: `AjazzBatteryMonitor.exe`)
- Icon embedded as `EmbeddedResource` and applied to EXE via `<ApplicationIcon>`

## [1.2.1] - 2026-07-24

### Fixed

- stopped inferring charging or full-charge status from unvalidated HID flags and battery percentage
- cleared charging state on telemetry gaps and restricted status probes to the AJAZZ HID allowlist
- treated BLE Battery Level as percentage-only telemetry

### Changed

- added anonymized five-sample power-state captures and a documented hardware-validation matrix
- display unknown charging state explicitly in history, diagnostics and the DeviceProbe CLI

### Known limitations

- physical AJ179 APEX power-state validation is still required before publishing a v1.2.1 release

## [1.2.0] - 2026-07-24

### Added

- centralized version metadata and locked NuGet dependency restores
- GitHub CI, CodeQL, dependency review and Dependabot configuration
- per-user Inno Setup installer, portable package, CycloneDX SBOM and SHA-256 checksum tooling
- non-hardware protocol, privacy and integration test coverage

### Changed

- HID fallback only runs for an explicitly approved AJ179 APEX control collection
- notification settings and repeat state persist in local application data
- tray updates are marshalled to the WinForms UI thread
- runtime logging redacts Bluetooth identifiers, HID paths and local user paths

### Security

- removed diagnostic, screenshot and local-environment publication paths
- release workflow validates version, locked dependencies, tests, SBOM and checksums before upload
- selected GPL-3.0-or-later and documented upstream protocol provenance

### Known limitations

- real-device HID/BLE telemetry remains unverified by automated tests
