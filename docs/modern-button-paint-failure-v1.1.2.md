# Root Cause Analysis: ModernButton OnPaint Failure (Parameter is not valid)

**Defect Ticket**: `MODERN_BUTTON_PAINT_FAILURE_V1.1.2`  
**Target Fix Release**: `v1.1.3`  

---

## 1. Root Cause Identification

- **Exact Location**: `src/AjazzBattery.App/UI/Controls/ModernButton.cs:81`
- **Flawed Code**:
  ```csharp
  using var fontBtn = Font;
  var sz = g.MeasureString(Text, fontBtn);
  ```
- **Mechanism of Failure**:
  1. `Font` is a property owned by the `Control` class (or inherited from its parent Form).
  2. Placing `using var fontBtn = Font;` caused the C# compiler to emit a `fontBtn.Dispose()` call at the end of the `OnPaint` method.
  3. Consequently, the button's `Font` instance was disposed during its very first paint pass.
  4. On subsequent paint passes (e.g. hovering over the button, scrolling the Settings panel, or switching tabs), `Font` was an already-disposed GDI object.
  5. `Graphics.MeasureString(Text, fontBtn)` threw `System.ArgumentException: Parameter is not valid.` when invoked with a disposed `Font`.
  6. WinForms caught the unhandled `OnPaint` exception and rendered the standard fallback: a white box with a red X.

---

## 2. Remediation Strategy in Version 1.1.3

1. **Remove Disposing of Control Font**:
   - `Font` MUST NOT be disposed inside `OnPaint`.
   - Access `Font ?? SystemFonts.MessageBoxFont` safely.

2. **Migrate to GDI TextRenderer**:
   - Replace `Graphics.MeasureString` and `Graphics.DrawString` with `TextRenderer.DrawText` and `TextRenderer.MeasureText`.
   - Implement `GetPreferredSize(Size proposedSize)` using `TextRenderer.MeasureText`.

3. **Paint Fallback Protection**:
   - Wrap `OnPaint` in a `try ... catch (Exception ex)` block.
   - On any paint failure, log detailed control properties (`Name`, `Text`, `Bounds`, `Font`, `DPI`) and render a safe fallback button using `ControlPaint.DrawButton` and `TextRenderer.DrawText`.
   - Never allow `OnPaint` exceptions to bubble up to the WinForms top-level message loop.
