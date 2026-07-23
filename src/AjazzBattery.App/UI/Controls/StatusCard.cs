using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AjazzBattery.App.UI.Theme;

namespace AjazzBattery.App.UI.Controls;

public sealed class StatusCard : UserControl
{
    private string _cardTitle = "ЗАГОЛОВОК";
    private string _cardValue = "Значение";
    private string _cardSubText = "Дополнительно";
    private Color? _valueColor;

    public string CardTitle
    {
        get => _cardTitle;
        set { _cardTitle = value; Invalidate(); }
    }

    public string CardValue
    {
        get => _cardValue;
        set { _cardValue = value; Invalidate(); }
    }

    public string CardSubText
    {
        get => _cardSubText;
        set { _cardSubText = value; Invalidate(); }
    }

    public Color? ValueColor
    {
        get => _valueColor;
        set { _valueColor = value; Invalidate(); }
    }

    public StatusCard()
    {
        DoubleBuffered = true;
        Size = new Size(160, 90);
        Padding = new Padding(12);
        Font = new Font("Segoe UI Variable Display", 9f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var pal = ThemeManager.Palette;
        g.Clear(Parent?.BackColor ?? pal.Background);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        int radius = 10;

        using (var path = GetRoundedRectPath(rect, radius))
        {
            using (var fillBrush = new SolidBrush(pal.Surface))
            {
                g.FillPath(fillBrush, path);
            }
            using (var borderPen = new Pen(pal.Border, 1.2f))
            {
                g.DrawPath(borderPen, path);
            }
        }

        // Draw Title
        using var fontTitle = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Bold);
        using var brushTitle = new SolidBrush(pal.MutedText);
        g.DrawString(_cardTitle.ToUpper(), fontTitle, brushTitle, 12, 10);

        // Draw Main Value
        using var fontVal = new Font("Segoe UI Variable Display", 11.5f, FontStyle.Bold);
        using var brushVal = new SolidBrush(_valueColor ?? pal.PrimaryText);
        g.DrawString(_cardValue, fontVal, brushVal, 12, 30);

        // Draw SubText
        using var fontSub = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Regular);
        using var brushSub = new SolidBrush(pal.SecondaryText);
        g.DrawString(_cardSubText, fontSub, brushSub, 12, 58);
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
