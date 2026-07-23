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
- on-dock-charging hardware capture: NOT_EXECUTED
- removed-from-dock hardware capture: NOT_EXECUTED
- USB-charging hardware capture: NOT_EXECUTED
- fully-charged hardware capture: NOT_EXECUTED
- BLE to HID transition: NOT_EXECUTED

## Verdict

NOT_READY_FOR_RELEASE. PASS is prohibited until repeated captures verify both
off-dock and on-dock physical states. No `v1.2.1` tag, release, installer
claim, or hardware-support claim may be published from this review.
