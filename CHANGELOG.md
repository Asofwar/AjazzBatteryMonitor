# Changelog — AJAZZ AJ179 APEX Battery Monitor

## [1.1.3] - 2026-07-23

### Fixed & Enhanced
- **Centralized Time Service (`IClock`)**: Implemented `IClock`, `SystemClock`, and `FakeClock`. Timestamps stored in UTC and converted to `TimeZoneInfo.Local` for display. Fixed 3-hour UTC offset display bug across UI, cards, tray, and diagnostics.
- **Fixed `ModernButton.OnPaint` GDI Crash**: Removed `using var fontBtn = Font;` that accidentally disposed the control's font. Migrated text rendering to safe GDI `TextRenderer` with `try...catch` fallback.
- **Application Lifecycle Exception Handling**: Introduced `AppLifecyclePhase` (`Starting`, `Running`, `ShuttingDown`) and error fingerprint deduplication to prevent UI paint exceptions from popping up "Startup Error" modal dialogs.
- **Privacy Log Redaction**: Automatically redact Bluetooth MAC addresses and full BLE DeviceInformation IDs in logs (`[id redacted]`).
- **Real Screen Capture Evidence Pipeline**: Created `scripts/capture-real-ui.ps1` for capturing actual Win32 window bounds, PID, HWND, SHA-256 hashes, UI Automation tree dump, and capture manifest. Invalidated all previous synthetic AI mockups into `artifacts/mockups-invalid/`.

### Fixed & Refactored
- **Strict 4-Row Grid Layout**: Replaced single-row header with dedicated `HeaderLayout` (Row 0, 68px) and `NavigationLayout` (Row 1, 48px).
- **Navigation Invariants**: Restored active "Обзор" tab button alongside "История" and "Настройки". Navigation text is never cut off or pushed off-screen.
- **Removed Text Separators**: Removed raw vertical `|` bar characters and bracket strings. Badges use clean rounded pill containers.
- **StatusCard Dynamic Height**: Rewrote `StatusCard` using nested 3-row `TableLayoutPanel`. Height fits text content dynamically without tall empty space.
- **Autonomy Text Truncation**: Display text `"≈ 3 дня"` is completely unclipped without `AutoEllipsis`.
- **CLI Diagnostic Flag**: Added `--dump-ui-tree` flag for dumping control hierarchy tree.
- **Устранение противоречивых статусов**: Согласована логика между `status.ConnectionMode`, `status.IsPresent` и `status.ActiveTransport` — устранено одновременное отображение «Подключена» и «Отключено».
- **Адаптация DPI (100%–300%)**: Заголовок и карточки пропорционально масштабируются без наложения элементов друг на друга.
- **Балансировка пространства**: Устранены пустые зоны в нижней части окна и перегрузка верхней шапки.

---

## [1.1.1] — 2026-07-23

### Исправлено
- **Устранение жестких координат (`Location = Point(...)`)**: Весь интерфейс `MainForm` переведен на динамические сетки `TableLayoutPanel` и `FlowLayoutPanel`.
- **Исправление обрезания вкладок навигации**: Кнопки вкладок («Обзор», «История», «Настройки») используют `AutoSize = true` с подгонкой под шрифт и системный масштаб текста.
- **Устранение противоречивых статусов**: Согласована логика между `status.ConnectionMode`, `status.IsPresent` и `status.ActiveTransport` — устранено одновременное отображение «Подключена» и «Отключено».
- **Адаптация DPI (100%–300%)**: Заголовок и карточки пропорционально масштабируются без наложения элементов друг на друга.
- **Балансировка пространства**: Устранены пустые зоны в нижней части окна и перегрузка верхней шапки.

---

## [1.1.0] — 2026-07-23
- Реализована централизованная дизайн-система с тёмной и светлой темами (`ThemePalette`, `ThemeManager`).
- Добавлено главное окно `MainForm` с круговым индикатором `BatteryGaugeControl` и карточками `StatusCard`.
- Внедрен движок уведомлений `BatteryNotificationService` (Toast & Balloon fallback).
- Обновлены динамические High-DPI иконки системного трея.

---

## [1.0.2] — 2026-07-23
- Реализован 2-step HID протокол `SET_FEATURE 0xF7` -> 30ms -> `GET_FEATURE 0x05`.
- Добавлен приоритет активного соединения Bluetooth LE GATT (`ConnectionStatus == Connected`).
