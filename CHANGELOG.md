# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses semantic versioning.

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

### Known limitations

- real-device HID/BLE telemetry remains unverified by automated tests
- public licensing remains blocked pending a provenance decision for the upstream protocol reference
