# Independent charging-state review — v1.2.1

## Scope

- no charging bit is accepted without physical evidence
- BLE `0x2A19` supplies percentage only
- invalid frames and telemetry gaps clear charging and full-charge state
- UI, tray, history, and notifications distinguish unknown from false
- status probes use the AJAZZ HID allowlist before `SET_FEATURE`

## Evidence

- static code review: PASS after freshness/debounce corrections
- non-hardware build and tests: PASS, 47/47
- awake-off-dock hardware capture: NOT_EXECUTED
- sleeping-off-dock hardware capture: PASS, five HID 2.4G readings; charging unknown, sleeping true
- current fail-safe UI check: PASS; no lightning and no "Зарядка началась" notification while sleeping off dock
- on-dock-charging hardware capture: NOT_EXECUTED
- removed-from-dock hardware capture: NOT_EXECUTED
- USB-charging hardware capture: NOT_EXECUTED
- fully-charged hardware capture: NOT_EXECUTED
- BLE to HID transition: NOT_EXECUTED

## Verdict

PASS_FOR_FAIL_SAFE_RELEASE. 2.4 GHz HID charging detection is unsupported:
`IsCharging`, `IsFullyCharged`, and `ChargingConfidence` remain `null`,
`null`, and `Unknown` respectively. Additional power-state captures are
required before enabling HID charging detection, not before releasing this
fail-safe behavior. The release must not claim hardware-confirmed charging.
