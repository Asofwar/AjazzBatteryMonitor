# AJ179 APEX power-state matrix

This matrix contains only anonymized, repeated hardware observations. Capture
files contain no MAC address, serial number, HID path, or device identifier.

| Physical state | Captures | Charging confirmed | Sleeping confirmed | Verdict |
|---|---:|---|---|---|
| Awake, off dock | 0 | unknown | unknown | NOT_EXECUTED |
| Sleeping, off dock | 5 | no | yes | PASS: no false charging claim |
| On dock, charging | 0 | unknown | unknown | NOT_EXECUTED |
| Removed from dock | 0 | unknown | unknown | NOT_EXECUTED |
| USB charging | 0 | unknown | unknown | NOT_EXECUTED |
| Fully charged | 0 | unknown | unknown | NOT_EXECUTED |

Until each state has at least five sequential captures, 2.4 GHz charging is
not protocol-confirmed. Production code therefore keeps `IsCharging` and
`IsFullyCharged` null rather than inferring either from status bits or percent.

## Recorded observation

`power-state-state-sleeping-off-dock.json` contains five sequential HID 2.4G
responses recorded on 2026-07-23 21:38 UTC. All were 65-byte report `0x05`,
began `05-00-00-51-01-01-01-02`, parsed to 81%, and had
`IsSleeping=true`, `IsCharging=null`. This verifies that the observed sleep
bit must not be used as a charging bit. It does not verify a charging bit.
