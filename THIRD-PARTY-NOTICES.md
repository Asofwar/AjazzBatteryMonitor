# Third-Party Notices & Licenses

This project is built using native .NET 8.0 SDK, WinForms, and WinRT APIs without external third-party GUI framework dependencies.

---

## 1. .NET 8.0 Runtime & Libraries
- **License**: MIT License
- **Copyright**: (c) .NET Foundation and Contributors.

---

## 2. Upstream Open Source References
- **ajazz-control-center**:
  - **URL**: https://github.com/Aiacos/ajazz-control-center
  - **License**: GPL-3.0
  - **Usage**: Used as protocol reference for AJAZZ AJ series mouse hardware packet structure (`0xF7` SET_FEATURE status poll and `0x05` GET_FEATURE frame).
- **Nibble**:
  - **URL**: https://github.com/mahammadismayilov/nibble
  - **License**: GPL-3.0-or-later
  - **Usage**: Evaluated as a protocol reference only; no source-code reuse is asserted by this notice.
- **Aj179PStat**:
  - **URL**: https://github.com/GetTheNya/Aj179PStat
  - **License**: no root license file found in the audited upstream commit `6c3be85598cd5f728530d729f7e4918a0b7b53f0`.
  - **Usage**: Evaluated as a tray-application reference only; reuse permission is not assumed.
