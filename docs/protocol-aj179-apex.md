# AJAZZ AJ179 APEX HID Protocol Specification

## Device Identification Matrix

| Connection Mode | Vendor ID | Product ID | Usage Page | Usage | Interface |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **2.4 GHz Dongle** | `0x3151` | `0x402D` | `0xFF00` / `0xFF01` | `0x0001` | Vendor-defined HID interface |
| **Dock Station** | `0x3151` | `0x5007` | `0xFF00` / `0xFF01` | `0x0001` | Charging Dock Receiver HID |
| **USB Cable** | `0x3151` | `0x502D` | `0xFF00` / `0xFF01` | `0x0001` | Direct Wired USB Interface |
| **Alternate 2.4G** | `0x3151` | `0x5008` | `0xFF00` / `0xFF01` | `0x0001` | AJ179 Variant Receiver |
| **Bluetooth LE** | Standard GATT | `0x180F` | `0x2A19` (Char) | N/A | BLE Battery Service |

## Query Command Specification (Feature Report)

### 2.4GHz / USB Query Packet (65 Bytes Total: 1 Byte Report ID + 64 Bytes Data)
- **Report ID**: `0x00` (or `0x08` on specific firmware variants)
- **Feature Report Buffer**:
  - `[0]` = `0x00` (Report ID)
  - `[1]` = `0x20` (Query Opcode / Battery Request)
  - `[2]` = `0x01` (Sub-opcode)
  - `[3..64]` = `0x00` (Zero padding)

### Response Packet Layout (64 / 65 Bytes)
- **Byte 4 (`resp[4]`)**: Battery percentage integer (`0` to `100` decimal).
  - `0xFF` (255) indicates fully charged / connected to external power.
  - `0x00` (0) when invalid indicates read error or mouse entering sleep state.
- **Byte 3 / 5 (`resp[3]`, `resp[5]`)**:
  - Bit 0: Charging status (`1` = Charging, `0` = Discharging)
  - Bit 1: Sleep status (`1` = Sleep / Deep Sleep)

## Forbidden Commands
The following HID opcodes are strictly prohibited from being sent to avoid risking hardware bricking or EEPROM corruption:
- Firmware Flashing / Bootloader Opcodes (`0xA1`, `0xFF`)
- Memory Write Commands
- Polling Rate / DPI Modification Opcodes
- RGB Profile Writes
- Macro Writes
