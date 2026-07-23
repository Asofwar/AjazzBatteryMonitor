using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Core;

namespace AjazzBattery.App.UI.Controls;

public sealed class ModernButton : Button
{
    private bool _isHovered;
    private bool _isPressed;

    public bool IsPrimary { get; set; } = true;

    public ModernButton()
    {
        DoubleBuffered = true;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Size = new Size(130, 36);
        Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var font = Font ?? SystemFonts.MessageBoxFont;
        var textSize = TextRenderer.MeasureText(
            Text ?? string.Empty,
            font,
            proposedSize,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

        return new Size(
            textSize.Width + 24,
            Math.Max(textSize.Height + 12, 36));
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        try
        {
            PaintModernButton(pevent);
        }
        catch (Exception ex)
        {
            Logger.LogException($"ModernButton_PaintFailed_{Name}", ex);
            PaintFallbackButton(pevent);
        }
    }

    private void PaintModernButton(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var pal = ThemeManager.Palette;
        g.Clear(Parent?.BackColor ?? pal.Surface);

        Color btnBg = IsPrimary
            ? (_isPressed ? pal.Accent : (_isHovered ? ControlPaint.Light(pal.Accent, 0.1f) : pal.Accent))
            : (_isPressed ? pal.SurfaceElevated : (_isHovered ? pal.SurfaceElevated : pal.Surface));

        Color btnText = IsPrimary ? Color.White : pal.PrimaryText;
        Color borderCol = IsPrimary ? pal.Accent : pal.Border;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = GetRoundedRectPath(rect, 6))
        {
            using (var fillBrush = new SolidBrush(btnBg))
            {
                g.FillPath(fillBrush, path);
            }
            using (var borderPen = new Pen(borderCol, 1.2f))
            {
                g.DrawPath(borderPen, path);
            }
        }

        var font = Font ?? SystemFonts.MessageBoxFont;
        var textBounds = new Rectangle(4, 2, Width - 8, Height - 4);

        if (!string.IsNullOrEmpty(Text))
        {
            TextRenderer.DrawText(
                g,
                Text,
                font,
                textBounds,
                btnText,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix);
        }
    }

    private void PaintFallbackButton(PaintEventArgs e)
    {
        e.Graphics.Clear(SystemColors.Control);
        ControlPaint.DrawButton(e.Graphics, ClientRectangle, _isPressed ? ButtonState.Pushed : ButtonState.Normal);

        var font = SystemFonts.MessageBoxFont;
        TextRenderer.DrawText(
            e.Graphics,
            Text ?? string.Empty,
            font,
            ClientRectangle,
            SystemColors.ControlText,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix);
    }

    private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
