# AJ179 APEX power-state matrix

This matrix must contain only anonymized, repeated hardware observations. No
real capture has been recorded for v1.2.1 yet; all rows below are intentionally
`NOT_EXECUTED` and cannot validate a charging bit.

| Physical state | Captures | Charging confirmed | Sleeping confirmed | Verdict |
|---|---:|---|---|---|
| Awake, off dock | 0 | unknown | unknown | NOT_EXECUTED |
| Sleeping, off dock | 0 | unknown | unknown | NOT_EXECUTED |
| On dock, charging | 0 | unknown | unknown | NOT_EXECUTED |
| Removed from dock | 0 | unknown | unknown | NOT_EXECUTED |
| USB charging | 0 | unknown | unknown | NOT_EXECUTED |
| Fully charged | 0 | unknown | unknown | NOT_EXECUTED |

Until each state has at least five sequential captures, 2.4 GHz charging is
not protocol-confirmed. Production code therefore keeps `IsCharging` and
`IsFullyCharged` null rather than inferring either from status bits or percent.
