# Changelog — AJAZZ AJ179 APEX Battery Monitor

## [1.1.0] — 2026-07-23

### Добавлено
- **Современная дизайн-система**: Реализованы `AppTheme`, `ThemePalette`, `ThemeManager` и `ThemeAwareForm` со светлой и тёмной темами.
- **Главное окно (`MainForm`)**: Внедрен круговой GDI+ индикатор `BatteryGaugeControl`, карточки статуса `StatusCard`, и навигация по разделам «Обзор», «История» и «Настройки».
- **График истории (`BatteryHistoryChart`)**: Отрисовка графиков разряда за 24 часа, 7 дней и 30 дней с корректной обработкой разрывов при отсутствии данных.
- **Движок уведомлений (`BatteryNotificationService`)**: Windows Toast Notifications для порогов 20%, 10%, 5%, гистерезис 5%, защита от одноопросных аномалий и критические напоминания.
- **Обновленный динамический Tray Icon**: Векторная отрисовка иконок в разрешениях 16x16, 20x20, 24x24, 32x32 с поддержкой индикаторов молнии `⚡`, сна `Z` и отсоединения `×`.
- **IPC активация первого экземпляра**: При попытке запуска второго экземпляра активируется уже запущенное окно первого экземпляра без создания дубликатов иконок в трее.

---

## [1.0.2] — 2026-07-23
- Реализован 2-step HID протокол `SET_FEATURE 0xF7` -> 30ms -> `GET_FEATURE 0x05`.
- Добавлен приоритет активного соединения Bluetooth LE GATT (`ConnectionStatus == Connected`).
- Создано CLI средство отладки `AjazzBattery.DeviceProbe`.

---

## [1.0.1] — 2026-07-23
- Миграция на чистое WinForms `ApplicationContext` для устранения runtime-вылета `System.DllNotFoundException` в single-file EXE.
- Добавлено раннее логирование в `%LocalAppData%\AjazzBatteryMonitor\logs\startup.log`.
