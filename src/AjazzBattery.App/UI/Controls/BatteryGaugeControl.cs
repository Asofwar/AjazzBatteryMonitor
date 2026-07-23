using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Core;

namespace AjazzBattery.App.UI.Controls;

public sealed class BatteryGaugeControl : UserControl
{
    private BatteryStatus _status = BatteryStatus.CreateUnknown();

    public BatteryStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            Invalidate();
        }
    }

    public BatteryGaugeControl()
    {
        DoubleBuffered = true;
        Size = new Size(200, 200);
        Font = new Font("Segoe UI Variable Display", 9f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var pal = ThemeManager.Palette;
        g.Clear(Parent?.BackColor ?? pal.Surface);

        int size = Math.Min(Width, Height) - 20;
        if (size <= 0) return;

        int strokeWidth = 14;
        var rect = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);

        // 1. Draw Outer Track Background
        using (var trackPen = new Pen(pal.Border, strokeWidth))
        {
            trackPen.StartCap = LineCap.Round;
            trackPen.EndCap = LineCap.Round;
            g.DrawArc(trackPen, rect, 135, 270);
        }

        // 2. Calculate Fill Angle & Color
        int percent = _status.Percent ?? 0;
        bool isCharging = _status.IsCharging == true;
        bool isSleeping = _status.IsSleeping;
        bool isPresent = _status.IsPresent;

        Color gaugeColor = ThemeManager.GetBatteryLevelColor(percent, isCharging, isSleeping);

        if (isPresent && _status.Percent.HasValue && !isSleeping)
        {
            float sweepAngle = (percent / 100f) * 270f;
            if (sweepAngle > 0)
            {
                using var fillPen = new Pen(gaugeColor, strokeWidth);
                fillPen.StartCap = LineCap.Round;
                fillPen.EndCap = LineCap.Round;
                g.DrawArc(fillPen, rect, 135, sweepAngle);
            }
        }

        // 3. Draw Centered Content
        string mainText = !isPresent ? "—" : (isSleeping ? "Сон" : (_status.Percent.HasValue ? $"{percent}%" : "?"));
        using var fontLarge = new Font("Segoe UI Variable Display", 32f, FontStyle.Bold);
        using var brushMain = new SolidBrush(pal.PrimaryText);
        var sizeMain = g.MeasureString(mainText, fontLarge);

        float textX = (Width - sizeMain.Width) / 2;
        float textY = (Height - sizeMain.Height) / 2 - 10;
        g.DrawString(mainText, fontLarge, brushMain, textX, textY);

        // 4. Draw Status Subtitle
        string subText;
        if (!isPresent)
        {
            subText = "Отключена";
        }
        else if (isSleeping)
        {
            subText = "Мышь спит";
        }
        else if (isCharging)
        {
            subText = "Заряжается ⚡";
        }
        else if (!_status.Percent.HasValue)
        {
            subText = "Заряд неизвестен";
        }
        else if (percent > 50)
        {
            subText = "Отличный заряд";
        }
        else if (percent > 20)
        {
            subText = "Нормальный заряд";
        }
        else if (percent > 10)
        {
            subText = "Низкий заряд";
        }
        else
        {
            subText = "Критический заряд!";
        }

        using var fontSub = new Font("Segoe UI Variable Text", 10.5f, FontStyle.Bold);
        using var brushSub = new SolidBrush(gaugeColor);
        var sizeSub = g.MeasureString(subText, fontSub);
        g.DrawString(subText, fontSub, brushSub, (Width - sizeSub.Width) / 2, textY + sizeMain.Height - 4);
    }
}
