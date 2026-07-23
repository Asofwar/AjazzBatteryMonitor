# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses semantic versioning.

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
