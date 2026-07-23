using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AjazzBattery.App.UI.Theme;

public class ThemeAwareForm : Form
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public ThemeAwareForm()
    {
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI Variable Display", 9.5f, FontStyle.Regular, GraphicsUnit.Point, 0, false);

        ThemeManager.ThemeChanged += OnThemeChangedInternal;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTheme();
    }

    private void OnThemeChangedInternal(object? sender, EventArgs e)
    {
        if (IsHandleCreated && !IsDisposed)
        {
            if (InvokeRequired)
            {
                BeginInvoke(ApplyTheme);
            }
            else
            {
                ApplyTheme();
            }
        }
    }

    public virtual void ApplyTheme()
    {
        var pal = ThemeManager.Palette;
        BackColor = pal.Background;
        ForeColor = pal.PrimaryText;

        ApplyTitleBarTheme();
        Invalidate(true);
    }

    private void ApplyTitleBarTheme()
    {
        if (IsHandleCreated)
        {
            int useDarkMode = ThemeManager.IsDark ? 1 : 0;
            int res = DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
            if (res != 0)
            {
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ThemeManager.ThemeChanged -= OnThemeChangedInternal;
        }
        base.Dispose(disposing);
    }
}
