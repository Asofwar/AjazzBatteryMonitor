# Pre-publication remediation record

## Resolved findings

| Finding | Resolution | Evidence |
|---|---|---|
| R-01 | Tray/menu mutations are marshalled to the captured WinForms UI thread. | Release build and installer smoke test passed locally. |
| R-02, N-01, N-02 | Notification processing is serialized; settings and state persist in local application data. | 43 non-hardware tests passed locally. |
| P-01, P-02 | HID access fails closed unless the full approved collection identity matches. | Protocol tests cover unknown and mismatched collections. |
| P-03, P-04 | Parser and empty GATT frame validation were strengthened. | Non-hardware tests passed locally. |
| S-01, S-02, S-03 | Diagnostic/image publication paths were removed; identifiers are redacted. | Publication history scan and gitleaks completed locally. |
| Build/release | Central props, locks, CI, CodeQL, Dependabot, installer, SBOM and checksums were added. | Local release asset build and verification passed. |

## Remaining gates

| Gate | Status |
|---|---|
| Physical AJ179 APEX HID/BLE reading | `NOT_PROVEN` — no real-device evidence in this audit. |
| GitHub workflow execution | `NOT_EXECUTED` — no remote repository/release has been created. |
| Windows code signing | `NOT_CONFIGURED` — the workflow supports conditional signing but no certificate was supplied. |
| License/provenance | `PASS WITH GPL OBLIGATIONS` — GPL-3.0-or-later is declared; corresponding source and notices must accompany public binaries. |
