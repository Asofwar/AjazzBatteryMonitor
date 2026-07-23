# Pre-publication review

## Scope

Static independent review of the hydrated working tree for the planned public release `1.2.0`. Runtime, hardware, installer, CI and Git-history checks that require unavailable tooling or external state are explicitly marked `NOT_EXECUTED` or `NOT_PROVEN`.

## Reviewed commit

`e346c089679a255b8bb52a1946a551c33dffa504` (`release: v1.1.3 centralized IClock local time fix, ModernButton GDI paint repair, and Win32 capture pipeline`).

## Runtime findings

| ID | Severity | Finding |
|---|---|---|
| R-01 | High | `TrayApplicationContext.OnStatusUpdated` is invoked from asynchronous polling and directly updates `NotifyIcon`, its menu and icon outside the UI thread. |
| R-02 | Medium | Manual refresh, power/session refresh and polling can overlap; notification state is mutated across awaited calls without synchronization. |
| R-03 | Medium | Toast and balloon click activation is not proven: `action=open_main_window` is not parsed by `Program` and the required desktop registration is not evidenced. |
| R-04 | Low | Startup logging hard-codes `1.1.3`; the release version is not a single source of truth. |

## BLE/HID findings

| ID | Severity | Finding |
|---|---|---|
| P-01 | High | The engine attempts the HID provider for all enumerated collections. The provider defaults to `SET_FEATURE 0xF7` when no profile matches, so an unapproved collection can receive a write command. |
| P-02 | Medium | Device profile matching ignores usage page, usage and interface number. |
| P-03 | Medium | HID cancellation is caught as a generic error; parser validation accepts too-short frames. |
| P-04 | Medium | BLE discovery uses weak name/identifier matching and does not classify an empty GATT value as an invalid frame. |

Hardware support remains `NOT_PROVEN`: mock tests do not prove a real AJ179 APEX HID reading.

## UI findings

`ModernButton.OnPaint` no longer calls `Graphics.MeasureString`; it uses `TextRenderer` with fallback handling. Static inspection is encouraging, but release-EXE `DrawToBitmap` verification is `NOT_EXECUTED`.

## Notification findings

| ID | Severity | Finding |
|---|---|---|
| N-01 | High | Notification settings controls have no change handlers and do not persist their values. |
| N-02 | High | Threshold/repeat state is constructed in memory and is lost on restart, allowing duplicate notifications. |

## Security findings

| ID | Severity | Finding |
|---|---|---|
| S-01 | High | `scripts/package.ps1` packages the local startup log, diagnostics documents and screenshots. These are forbidden public assets. |
| S-02 | High | The working tree includes screenshot files and a screen-capture script, contrary to the publication constraints. |
| S-03 | Medium | Diagnostic and probe code can render or print full HID paths; production logging redaction does not redact all local paths or HID paths. |

## Privacy findings

No secrets are reproduced in this report. Repository-wide Git-history secret scan is `NOT_EXECUTED`; public release is blocked until it is completed and image/log/history cleanup is proven.

## Dependency findings

NuGet package references have no lock files and no locked-restore configuration. Dependency vulnerability analysis is `NOT_EXECUTED`.

## License findings

No project license has been selected. Third-party provenance and license compatibility are `NOT_PROVEN`; do not declare MIT or publish until audit completion.

## Build findings

`Directory.Build.props` is absent and project/script versions disagree. `.github/workflows` is absent. There is no verified installer, SBOM, checksums or release workflow.

## Test findings

`dotnet test` is `NOT_EXECUTED`: the host has only an x86 .NET runtime and no .NET SDK. Hardware tests were not run.

## Release risks

Public publication would expose prohibited assets and cannot currently prove reproducible builds, package integrity, license status, CI security checks, installer behaviour or actual device telemetry.

## Blocking issues

All High findings (`R-01`, `N-01`, `N-02`, `P-01`, `S-01`, `S-02`) and the missing versioning, CI/CD, installer, security/history and license gates block publication.

## Recommendations

1. Remove prohibited assets and unsafe packaging paths before any public remote is created.
2. Make HID polling fail closed; persist notification settings/state; marshal all WinForms work to the UI thread.
3. Centralize `1.2.0`, add locked restore, test coverage, CI/CD, installer and release verification.
4. Complete dependency, license and Git-history security audits before publication.
