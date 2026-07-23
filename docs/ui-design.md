# Руководство по дизайну интерфейса (UI/UX Design Spec v1.1.0)

В данном документе описана дизайн-система и принципы визуального оформления приложения **AJAZZ AJ179 APEX Battery Monitor** v1.1.0.

---

## 1. Архитектурные принципы UI

- **Чистый WinForms ApplicationContext**: Отсутствие тяжелых фреймворков (WPF/WinUI/Electron), предотвращающее runtime-дефекты сборки single-file EXE.
- **Поддержка High-DPI**: `AutoScaleMode = AutoScaleMode.Dpi`, динамическое сглаживание элементов (GDI+ `SmoothingMode.AntiAlias` и `TextRenderingHint.ClearTypeGridFit`).
- **Светлая и Тёмная темы**: Поддержка `System`, `Light`, `Dark` режимов с автоопределением системного стиля Windows `AppsUseLightTheme`.
- **Иммерсивный заголовки DWM**: Включение тёмной рамки окна Windows 10/11 через `DWMWA_USE_IMMERSIVE_DARK_MODE`.

---

## 2. Цветовая дизайн-система (`ThemePalette.cs`)

### Тёмная тема
- Background: `#0F1117`
- Surface: `#171A23`
- SurfaceElevated: `#1D212C`
- Border: `#2A303C`
- Text (Primary / Secondary / Muted): `#F4F6FA` / `#AAB2C0` / `#788293`
- Accent: `#7C8CFF`
- Success / Warning / Danger / Critical: `#48D17A` / `#FFB84D` / `#FF5D6C` / `#E5394F`

### Светлая тема
- Background: `#F4F6FA`
- Surface: `#FFFFFF`
- SurfaceElevated: `#F8F9FC`
- Border: `#DCE1E8`
- Text (Primary / Secondary / Muted): `#171A23` / `#596273` / `#808A9A`
- Accent: `#5869E8`
- Success / Warning / Danger / Critical: `#209B55` / `#D98600` / `#D93D50` / `#BE1E35`

---

## 3. Компоненты UI

1. **`BatteryGaugeControl`**:
   - Дуговой/круговой GDI+ индикатор заряда с динамическим цветом уровня.
   - Крупное цифровое значение процента и подпись состояния (`Отличный заряд`, `Заряжается ⚡`, `Мышь спит`).
2. **`StatusCard`**:
   - Карточки статуса с тонкой границей и скруглением 10px для отображения типа подключения, состояния, времени последнего обновления и автономности.
3. **`BatteryHistoryChart`**:
   - График истории с выбором временного диапазона (`24 часа`, `7 дней`, `30 дней`).
   - Разрывы линий при длительном отсутствии данных (отсутствие связи / выключение ПК).
4. **`TrayIconRenderer`**:
   - Генерация векторных значков трея высоких разрешений (16x16, 20x20, 24x24, 32x32) с белыми цифрами, иконками молнии (`⚡`), сна (`Z`), и отсоединения (`×`).
