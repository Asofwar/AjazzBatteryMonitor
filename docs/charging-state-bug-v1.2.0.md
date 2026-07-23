# Charging-state bug in v1.2.0

## Root cause

`src/AjazzBattery.Devices/YichipBatteryParser.cs`, `ParseResponse`, treated
`frame[4] & 0x01` as charging and treated `frame[3] == 100` as fully charged.
Neither interpretation was validated on a real AJ179 APEX across dock, sleep,
USB and off-dock states. The subsequent `BatteryMonitorEngine` fallback copied
the previous charging state when new HID telemetry was unavailable.

## Consequence

A sleeping 2.4 GHz mouse could retain or receive a charging presentation even
though no current, valid, hardware-confirmed charging telemetry existed. The
sleep bit at `frame[7] & 0x02` is independent from charging and must not imply
external power.

## v1.2.1 safety rule

Until repeated hardware capture validates a charging flag, HID and BLE battery
level reads set `IsCharging` and `IsFullyCharged` to `null`. Invalid, short or
missing frames also clear these values; a last known battery percentage may be
shown as stale without carrying a charging claim.
