# AJ179 APEX protocol status

## Evidence levels

| Field | Source-code behavior | Repeated hardware observation | Current status |
|---|---|---|---|
| Battery percent | parsed from a valid HID status frame; BLE `0x2A19` is percentage-only | not yet captured for this release | source-code behavior only |
| Sleep flag | parser reads byte 7, mask `0x02`, after the minimum-frame validation | not yet captured for this release | hypothesis, not a charging signal |
| Charging flag | no HID byte or mask is used in production | none | unknown; `IsCharging = null` |
| Dock presence | no dock bit is used as charging evidence | none | unknown |
| Full-charge flag | neither 100% nor a HID bit is used | none | unknown; `IsFullyCharged = null` |
| Other bytes | retained only in anonymized diagnostics | none | unknown |

## Frame validation

The HID parser requires expected report/header values, a minimum eight-byte
response, and a percentage in range before it reads status bytes. A short or
invalid frame returns `ProviderState.InvalidFrame`; it does not infer charging
or read missing bytes.

## Charging rule

Charging over 2.4 GHz is not currently supported as a confirmed state. Only a
future repeated physical observation may promote a documented bit to
`ProtocolConfirmed` or `HardwareValidated`. Until then `IsCharging` and
`IsFullyCharged` remain `null`, and production UI, tray, and notifications do
not show charging.
