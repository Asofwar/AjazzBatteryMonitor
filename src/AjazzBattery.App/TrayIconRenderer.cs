using System.Drawing;
using System.Drawing.Drawing2D;
using AjazzBattery.Core;

namespace AjazzBattery.App;

public static class TrayIconRenderer
{
    public static Icon CreateTrayIcon(BatteryStatus status)
    {
        const int width = 32;
        const int height = 32;

        using var bitmap = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.Transparent);

            Color bgColor;
            if (!status.IsPresent || !status.Percent.HasValue)
            {
                bgColor = Color.FromArgb(120, 120, 120);
            }
            else if (status.IsChargingConfirmed)
            {
                bgColor = Color.FromArgb(0, 150, 255);
            }
            else if (status.Percent.Value <= 20)
            {
                bgColor = Color.FromArgb(230, 50, 50);
            }
            else if (status.Percent.Value <= 45)
            {
                bgColor = Color.FromArgb(240, 180, 0);
            }
            else
            {
                bgColor = Color.FromArgb(40, 180, 80);
            }

            // Draw rounded background pill
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRoundedRectangle(brush, 1, 1, 30, 30, 6);
            }

            string text;
            if (!status.IsPresent)
            {
                text = "X";
            }
            else if (status.IsSleeping)
            {
                text = "Zzz";
            }
            else if (!status.Percent.HasValue)
            {
                text = "?";
            }
            else
            {
                text = status.Percent.Value.ToString();
            }

            float fontSize = text.Length switch
            {
                1 => 18f,
                2 => 15f,
                3 => 11f,
                _ => 10f
            };

            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(Color.White);
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(text, font, textBrush, new RectangleF(0, 0, width, height), sf);

            // Draw subtle lightning bolt indicator if charging
            if (status.IsChargingConfirmed && status.Percent.HasValue)
            {
                using var boltBrush = new SolidBrush(Color.Yellow);
                var boltPoints = new PointF[]
                {
                    new PointF(24, 2),
                    new PointF(28, 2),
                    new PointF(25, 10),
                    new PointF(30, 10),
                    new PointF(22, 20),
                    new PointF(24, 12),
                    new PointF(21, 12)
                };
                g.FillPolygon(boltBrush, boltPoints);
            }
        }

        IntPtr hIcon = bitmap.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        return icon;
    }

    private static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    {
        using var path = new GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
