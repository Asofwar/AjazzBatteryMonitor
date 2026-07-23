using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AjazzBattery.App.UI.Theme;

namespace AjazzBattery.App.UI.Controls;

public sealed class StatusCard : UserControl
{
    private readonly TableLayoutPanel _layout;
    private readonly Label _lblTitle;
    private readonly Label _lblValue;
    private readonly Label _lblSubText;

    private string _cardTitle = "ЗАГОЛОВОК";
    private string _cardValue = "Значение";
    private string _cardSubText = "Дополнительно";
    private Color? _valueColor;

    public string CardTitle
    {
        get => _cardTitle;
        set
        {
            _cardTitle = value;
            _lblTitle.Text = _cardTitle.ToUpperInvariant();
        }
    }

    public string CardValue
    {
        get => _cardValue;
        set
        {
            _cardValue = value;
            _lblValue.Text = _cardValue;
        }
    }

    public string CardSubText
    {
        get => _cardSubText;
        set
        {
            _cardSubText = value;
            _lblSubText.Text = _cardSubText;
        }
    }

    public Color? ValueColor
    {
        get => _valueColor;
        set
        {
            _valueColor = value;
            _lblValue.ForeColor = _valueColor ?? ThemeManager.Palette.PrimaryText;
        }
    }

    public StatusCard()
    {
        DoubleBuffered = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(0);

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14),
            Margin = new Padding(0)
        };

        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _lblTitle = new Label
        {
            Text = _cardTitle.ToUpperInvariant(),
            Font = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Bold),
            ForeColor = ThemeManager.Palette.MutedText,
            AutoSize = true,
            AutoEllipsis = false,
            Margin = new Padding(0, 0, 0, 4)
        };

        _lblValue = new Label
        {
            Text = _cardValue,
            Font = new Font("Segoe UI Variable Display", 11.5f, FontStyle.Bold),
            ForeColor = ThemeManager.Palette.PrimaryText,
            AutoSize = true,
            AutoEllipsis = false,
            Margin = new Padding(0, 0, 0, 4)
        };

        _lblSubText = new Label
        {
            Text = _cardSubText,
            Font = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Regular),
            ForeColor = ThemeManager.Palette.SecondaryText,
            AutoSize = true,
            AutoEllipsis = false,
            Margin = new Padding(0)
        };

        _layout.Controls.Add(_lblTitle, 0, 0);
        _layout.Controls.Add(_lblValue, 0, 1);
        _layout.Controls.Add(_lblSubText, 0, 2);

        Controls.Add(_layout);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var pal = ThemeManager.Palette;
        g.Clear(Parent?.BackColor ?? pal.Background);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        int radius = 10;

        using var path = GetRoundedRectPath(rect, radius);
        using (var fillBrush = new SolidBrush(pal.Surface))
        {
            g.FillPath(fillBrush, path);
        }
        using (var borderPen = new Pen(pal.Border, 1.2f))
        {
            g.DrawPath(borderPen, path);
        }
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
