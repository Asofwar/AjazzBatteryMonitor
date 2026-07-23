# Security & Hardware Safety Assessment

## Hardware Safety Guidelines
1. **Strictly Read-Only Feature Reports**: Communication with AJAZZ mice is limited to querying device battery status via Feature Report opcode `0x20`.
2. **Prohibited Opcodes**:
   - Firmware flashing opcodes (`0xA1`, `0xFF`) are strictly forbidden.
   - EEPROM/Flash memory write commands are strictly forbidden.
   - DPI, Polling Rate, Debounce, RGB, and Macro write commands are strictly prohibited.
3. **Non-Elevated Privilege Model**: Runs entirely without administrator privileges (standard user mode).
4. **Data Privacy**: Diagnostic exports contain zero serial numbers, user names, or machine IDs.
