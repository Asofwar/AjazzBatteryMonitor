# Timestamp & Timezone Audit — Release v1.1.2 vs v1.1.3

**Status**: AUDIT COMPLETED — NO DATA CORRUPTION FOUND

---

## 1. Audit Findings in Version 1.1.2

1. **Storage Layer (`BatteryHistoryStorage.cs`)**:
   - Timestamps were serialized using `DateTimeOffset.UtcNow.ToString("O")` or ISO 8601 UTC with explicit `Z` suffix.
   - Example saved timestamp: `2026-07-23T19:13:46.0000000+00:00` or `2026-07-23T19:13:46Z`.
   - **Conclusion**: Data stored on disk is valid UTC and requires NO physical data migration.

2. **Display Defect in UI (`MainForm.cs` & `OverviewControl.cs`)**:
   - When displaying `status.Timestamp` in the UI ("Обновлено" card, Footer, and Tooltip), `status.Timestamp.ToString("HH:mm:ss")` was called directly on UTC `DateTimeOffset` without `.ToLocalTime()`.
   - Result: Displayed `19:13:46` instead of Windows local time `22:13:46` (UTC+3 offset).

---

## 2. Remediation Strategy for Version 1.1.3

- **Centralized Time Service (`IClock`)**:
  - All components use `IClock` (`SystemClock` in production, `FakeClock` in unit tests).
- **Explicit Conversion Rule**:
  - Internally: `DateTimeOffset.UtcNow` (UTC everywhere).
  - Storage: `timestamp.ToUniversalTime().ToString("O")` (UTC ISO 8601).
  - User Interface: `clock.ToLocal(timestamp)` using Windows `TimeZoneInfo.Local`.
- **Relative Time Calculation**:
  - `var age = clock.UtcNow - timestamp.ToUniversalTime();`
  - Guarantees zero negative age bugs from mixing UTC and local timestamps.
