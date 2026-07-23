using System.Drawing.Drawing2D;
using System.Windows.Forms;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Core;

namespace AjazzBattery.App.UI.Rendering;

public static class TrayIconRenderer
{
    public static Icon CreateTrayIcon(BatteryStatus status, int targetSize = 32)
    {
        using var bitmap = new Bitmap(targetSize, targetSize);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int percent = status.Percent ?? 0;
            bool isCharging = status.IsChargingConfirmed;
            bool isSleeping = status.IsSleeping;
            bool isPresent = status.IsPresent;

            Color bgColor = ThemeManager.GetBatteryLevelColor(percent, isCharging, isSleeping);
            if (!isPresent) bgColor = ColorTranslator.FromHtml("#596273");

            // Draw Rounded Pill Container
            var rect = new Rectangle(0, 0, targetSize - 1, targetSize - 1);
            using (var path = GetRoundedRectPath(rect, targetSize / 4))
            {
                using var fillBrush = new SolidBrush(bgColor);
                g.FillPath(fillBrush, path);
            }

            // Draw Text / Symbol
            string displayText;
            if (!isPresent)
            {
                displayText = "×";
            }
            else if (isSleeping)
            {
                displayText = "Z";
            }
            else if (isCharging)
            {
                displayText = "⚡";
            }
            else if (!status.Percent.HasValue)
            {
                displayText = "?";
            }
            else if (percent == 100)
            {
                displayText = "100";
            }
            else
            {
                displayText = percent.ToString();
            }

            float fontSize = targetSize switch
            {
                16 => 7.5f,
                20 => 9.5f,
                24 => 11f,
                _ => 13.5f
            };

            if (displayText == "100") fontSize *= 0.75f;

            using var font = new Font("Segoe UI Variable Text", fontSize, FontStyle.Bold, GraphicsUnit.Point);
            using var textBrush = new SolidBrush(Color.White);

            var sz = g.MeasureString(displayText, font);
            float x = (targetSize - sz.Width) / 2f;
            float y = (targetSize - sz.Height) / 2f;

            g.DrawString(displayText, font, textBrush, x, y);
        }

        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            Win32DestroyIcon(hIcon);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
    private static extern bool Win32DestroyIcon(IntPtr hIcon);

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
