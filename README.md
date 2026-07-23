# AJAZZ AJ179 APEX Battery Monitor (v1.1.2)

Легковесное Windows-приложение для отслеживания уровня заряда мыши **AJAZZ AJ179 APEX** через **Bluetooth LE GATT Battery Service** и резервный **2.4 GHz HID Feature Report**.

---

## Особенности версии 1.1.2

- **Новая 4-строчная компоновка (`TableLayoutPanel`)**:
  - `HeaderLayout` (68px): Название `AJAZZ AJ179 APEX` и бейджи состояния (`Подключена`, `Bluetooth LE`).
  - `NavigationLayout` (48px): Выделенная строка с навигационными вкладками («Обзор», «История», «Настройки»).
  - `ContentHost` (100%): Двухколоночный интерфейс с дугой `BatteryGaugeControl` и компактными карточками `StatusCard`.
  - `FooterLayout` (44px): Индикатор связи и кнопка быстрого обновления.
- **Инварианты навигации и отказ от `AutoEllipsis`**: Исключено обрезание названий вкладок и значений карточек («≈ 3 дня»).
- **Полная поддержка High DPI (100%–200%)**: Динамический расчёт высоты карточек без пустого пространства.
- **Согласованный статус состояний**: Исключение противоречивых состояний (`[Подключена]` и `[Отключено]`). Единый точный маппинг для Bluetooth LE, 2.4 GHz HID и USB соединения.
- **Двойной транспорт телеметрии**: Автоматический выбор между нативным **Bluetooth LE GATT** (Service `0x180F` / Characteristic `0x2A19`) и аппаратно подтвержденным 2-step **Win32 HID Feature Report** протоколом (`SET_FEATURE 0xF7` -> 30ms -> `GET_FEATURE 0x05`).
- **Современный UI**: Дизайн-система с поддержкой светлой и тёмной тем, круговым индикатором `BatteryGaugeControl`, карточками статуса и графиками истории разряда.
- **Интеллектуальные уведомления**: Windows 10/11 Toast Notifications с защитой от спама, гистерезисом 5%, пороговыми уровнями 20%, 10%, 5%, критическими напоминаниями и balloon tip fallback.
- **Динамический системный трей**: Векторные иконки высокого разрешения (16x16–32x32) с белыми цифрами процента, индикаторами молнии `⚡`, сна `Z` и отсоединения `×`.
- **Чистый WinForms ApplicationContext**: Отсутствие WPF/Electron/WebView фреймворков для гарантии работы single-file portable EXE без вылетов.

---

## Сборка и запуск

### Системные требования
- Windows 10 (19041+) или Windows 11
- .NET 8.0 SDK (для сборки)

### Быстрый запуск
```powershell
# Запуск публикации portable EXE v1.1.1
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1

# Упаковка ZIP архивов
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1

# Запуск автоматического smoke-теста
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test-app.ps1
```
