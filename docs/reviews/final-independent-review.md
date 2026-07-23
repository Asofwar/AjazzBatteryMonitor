# Final independent review — v1.2.0 candidate

## Verdict

`READY FOR PUBLIC RELEASE WITH CONDITIONS`.

The implementation and local publication checks are acceptable for a public GPL release. GitHub workflow execution, signing and physical-device telemetry retain the conditions stated below.

## Evidence

| Area | Result | Evidence |
|---|---|---|
| Release build | `PASS` | `dotnet build AjazzBattery.sln -c Release --no-restore` completed with 0 warnings and 0 errors. |
| Non-hardware tests | `PASS` | `dotnet test AjazzBattery.sln -c Release --no-restore --filter "Category!=Hardware"`: 43 passed, 0 failed. |
| Package and installer | `PASS` | Portable executable/ZIP, per-user installer, SBOM and checksum assets were built and locally verified; install, launch smoke test, upgrade retention and uninstall completed. |
| Security scan | `PASS` | gitleaks scanned all reachable commits with no findings. |
| Publication-path scan | `PASS` | No prohibited media, logs, diagnostics or archive paths remain in reachable Git history. |
| Privacy scan | `PASS WITH REVIEWED EXCEPTIONS` | The only Bluetooth-identifier matches are the redaction regular expression and a non-identifying test prefix; no complete identifier was found. |
| CI/CD configuration | `READY, NOT_EXECUTED` | Locked restore, tests, CodeQL, dependency review, Dependabot, secret scan, SBOM, checksum and conditional signing are configured but have not run on GitHub. |
| Signing | `NOT_CONFIGURED` | The workflow signs only when certificate secrets are supplied; no certificate was provided. |
| Real mouse telemetry | `NOT_PROVEN` | No physical AJ179 APEX HID/BLE session was available. |
| License/provenance | `PASS WITH GPL OBLIGATIONS` | GPL-3.0-or-later is declared in the root `LICENSE`; see `license-audit.md`. |

## Required before publication

1. Publish the complete corresponding source together with binaries and retain GPL notices.
2. Run the GitHub release workflow from an annotated `v1.2.0` tag.
3. Do not represent real mouse telemetry or signing as verified until corresponding evidence exists.
