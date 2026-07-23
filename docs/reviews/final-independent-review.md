# Final independent review — v1.2.0 candidate

## Verdict

`BLOCKED FOR PUBLIC RELEASE`.

The implementation and local publication checks are acceptable for a release candidate, but a public GitHub repository, tag and release must not be created until the license/provenance gate is resolved. This is a release gate, not a code-quality failure.

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
| License/provenance | `BLOCKED` | See `license-audit.md`; no defensible public license selection is evidenced. |

## Required before publication

1. Resolve upstream code/protocol provenance and select a compatible root license.
2. Add the approved `LICENSE` and update notices.
3. Re-run this review and the GitHub release workflow from an annotated `v1.2.0` tag.
