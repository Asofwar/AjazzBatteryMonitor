using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Storage;

namespace AjazzBattery.App.UI.Controls;

public sealed class BatteryHistoryChart : UserControl
{
    private List<BatteryHistoryEntry> _allHistory = new();
    private TimeSpan _timeRange = TimeSpan.FromHours(24);

    public TimeSpan TimeRange
    {
        get => _timeRange;
        set
        {
            _timeRange = value;
            Invalidate();
        }
    }

    public BatteryHistoryChart()
    {
        DoubleBuffered = true;
        Size = new Size(500, 220);
        Font = new Font("Segoe UI Variable Display", 9f);
    }

    public void SetHistoryData(IEnumerable<BatteryHistoryEntry> history)
    {
        _allHistory = history.OrderBy(h => h.Timestamp).ToList();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var pal = ThemeManager.Palette;
        g.Clear(Parent?.BackColor ?? pal.Surface);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        int radius = 10;

        using (var path = GetRoundedRectPath(rect, radius))
        {
            using var fillBrush = new SolidBrush(pal.Surface);
            g.FillPath(fillBrush, path);
            using var borderPen = new Pen(pal.Border, 1.2f);
            g.DrawPath(borderPen, path);
        }

        int paddingLeft = 45;
        int paddingRight = 20;
        int paddingTop = 35;
        int paddingBottom = 30;

        int chartWidth = Width - paddingLeft - paddingRight;
        int chartHeight = Height - paddingTop - paddingBottom;

        if (chartWidth <= 0 || chartHeight <= 0) return;

        // Draw Y Axis Gridlines (0%, 25%, 50%, 75%, 100%)
        using (var gridPen = new Pen(pal.Border, 1f) { DashStyle = DashStyle.Dash })
        using (var labelFont = new Font("Segoe UI Variable Text", 8f))
        using (var labelBrush = new SolidBrush(pal.MutedText))
        {
            for (int yVal = 0; yVal <= 100; yVal += 25)
            {
                float y = paddingTop + chartHeight - (yVal / 100f) * chartHeight;
                g.DrawLine(gridPen, paddingLeft, y, paddingLeft + chartWidth, y);
                g.DrawString($"{yVal}%", labelFont, labelBrush, 8, y - 6);
            }
        }

        // Filter history by selected time range
        var cutoff = DateTimeOffset.UtcNow.Subtract(_timeRange);
        var filteredData = _allHistory.Where(h => h.Timestamp >= cutoff).OrderBy(h => h.Timestamp).ToList();

        if (filteredData.Count < 2)
        {
            using var fontEmpty = new Font("Segoe UI Variable Text", 10f);
            using var brushEmpty = new SolidBrush(pal.MutedText);
            string msg = "Недостаточно данных истории за выбранный период";
            var sz = g.MeasureString(msg, fontEmpty);
            g.DrawString(msg, fontEmpty, brushEmpty, (Width - sz.Width) / 2, (Height - sz.Height) / 2);
            return;
        }

        // Compute points and handle gaps (> 15 minutes between samples = gap)
        DateTimeOffset minTime = cutoff;
        DateTimeOffset maxTime = DateTimeOffset.UtcNow;
        double totalSeconds = (maxTime - minTime).TotalSeconds;
        if (totalSeconds <= 0) totalSeconds = 1;

        var currentSegment = new List<PointF>();

        void RenderSegment(List<PointF> pts)
        {
            if (pts.Count == 0) return;

            if (pts.Count == 1)
            {
                using var pBrush = new SolidBrush(pal.Accent);
                g.FillEllipse(pBrush, pts[0].X - 3, pts[0].Y - 3, 6, 6);
                return;
            }

            // Fill gradient area below line
            using (var areaPath = new GraphicsPath())
            {
                areaPath.AddLines(pts.ToArray());
                areaPath.AddLine(pts.Last().X, paddingTop + chartHeight, pts.First().X, paddingTop + chartHeight);
                areaPath.CloseFigure();

                using var gradBrush = new LinearGradientBrush(
                    new PointF(0, paddingTop),
                    new PointF(0, paddingTop + chartHeight),
                    Color.FromArgb(60, pal.Accent),
                    Color.FromArgb(5, pal.Accent)
                );
                g.FillPath(gradBrush, areaPath);
            }

            // Draw line
            using (var linePen = new Pen(pal.Accent, 2.2f))
            {
                g.DrawLines(linePen, pts.ToArray());
            }
        }

        for (int i = 0; i < filteredData.Count; i++)
        {
            var p = filteredData[i];
            if (!p.Percent.HasValue) continue;

            float x = paddingLeft + (float)((p.Timestamp - minTime).TotalSeconds / totalSeconds) * chartWidth;
            float y = paddingTop + chartHeight - (p.Percent.Value / 100f) * chartHeight;
            x = Math.Clamp(x, paddingLeft, paddingLeft + chartWidth);
            y = Math.Clamp(y, paddingTop, paddingTop + chartHeight);

            if (currentSegment.Count > 0)
            {
                var prevPoint = filteredData[i - 1];
                if ((p.Timestamp - prevPoint.Timestamp).TotalMinutes > 30)
                {
                    RenderSegment(currentSegment);
                    currentSegment.Clear();
                }
            }

            currentSegment.Add(new PointF(x, y));
        }

        RenderSegment(currentSegment);

        // Draw Summary (Min, Max, Avg)
        var validPercents = filteredData.Where(h => h.Percent.HasValue).Select(h => h.Percent!.Value).ToList();
        if (validPercents.Count > 0)
        {
            int minP = validPercents.Min();
            int maxP = validPercents.Max();
            int avgP = (int)validPercents.Average();

            string statsStr = $"Мин: {minP}% | Макс: {maxP}% | Средн: {avgP}%";
            using var fontStats = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Bold);
            using var brushStats = new SolidBrush(pal.SecondaryText);
            g.DrawString(statsStr, fontStats, brushStats, paddingLeft, 10);
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
