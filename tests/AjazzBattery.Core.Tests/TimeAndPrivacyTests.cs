using Xunit;
using AjazzBattery.Core;
using AjazzBattery.Core.Time;
using AjazzBattery.App.UI.Controls;
using System.Drawing;

namespace AjazzBattery.Core.Tests;

public class TimeAndPrivacyTests
{
    [Fact]
    public void TestClock_Utc3Offset_ConvertsCorrectly()
    {
        var utcTimestamp = DateTimeOffset.Parse("2026-07-23T19:13:46Z");
        var clock = new FakeClock(utcTimestamp, TimeSpan.FromHours(3));

        var localTime = clock.ToLocal(utcTimestamp);

        Assert.Equal(22, localTime.Hour);
        Assert.Equal(13, localTime.Minute);
        Assert.Equal(46, localTime.Second);
    }

    [Fact]
    public void TestClock_UtcMinus5Offset_ConvertsCorrectly()
    {
        var utcTimestamp = DateTimeOffset.Parse("2026-07-23T19:13:46Z");
        var clock = new FakeClock(utcTimestamp, TimeSpan.FromHours(-5));

        var localTime = clock.ToLocal(utcTimestamp);

        Assert.Equal(14, localTime.Hour);
        Assert.Equal(13, localTime.Minute);
        Assert.Equal(46, localTime.Second);
    }

    [Fact]
    public void TestClock_MidnightDateCrossover_ConvertsCorrectly()
    {
        var utcTimestamp = DateTimeOffset.Parse("2026-07-23T22:30:00Z");
        var clock = new FakeClock(utcTimestamp, TimeSpan.FromHours(3));

        var localTime = clock.ToLocal(utcTimestamp);

        Assert.Equal(24, localTime.Day);
        Assert.Equal(1, localTime.Hour);
        Assert.Equal(30, localTime.Minute);
    }

    [Fact]
    public void TestClock_FormatRelativeTime_JustNowAndSecondsAgo()
    {
        var now = DateTimeOffset.Parse("2026-07-23T19:14:00Z");
        var clock = new FakeClock(now, TimeSpan.FromHours(3));

        var read1 = DateTimeOffset.Parse("2026-07-23T19:13:55Z"); // 5s ago
        var read2 = DateTimeOffset.Parse("2026-07-23T19:13:40Z"); // 20s ago

        Assert.Equal("Только что", clock.FormatRelativeTime(read1));
        Assert.Equal("20 сек назад", clock.FormatRelativeTime(read2));
    }

    [Fact]
    public void TestLogger_RedactsBluetoothMacAndDeviceIds()
    {
        string raw = "Found matching BLE Device: AJ179 APEX (BluetoothLE#[redacted])";
        string redacted = Logger.RedactSensitiveData(raw);

        Assert.DoesNotContain("38:d5:7a:eb:8f:ee", redacted);
        Assert.Contains("[id redacted]", redacted);
    }

    [Fact]
    public void TestLogger_RedactsHidAndLocalPaths()
    {
        const string raw = @"Path [redacted HID path] [redacted local path]";
        string redacted = Logger.RedactSensitiveData(raw);

        Assert.DoesNotContain("HID#VID", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"[redacted local path]", redacted, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestModernButton_GetPreferredSize_AndRepeatedPaintsDoNotDisposeFont()
    {
        using var btn = new ModernButton { Text = "Проверить" };
        var prefSize = btn.GetPreferredSize(new Size(200, 100));

        Assert.True(prefSize.Width > 0);
        Assert.True(prefSize.Height >= 36);

        using var bmp = new Bitmap(200, 50);
        using var g = Graphics.FromImage(bmp);

        // Perform 100 consecutive paints via DrawToBitmap
        for (int i = 0; i < 100; i++)
        {
            btn.DrawToBitmap(bmp, new Rectangle(0, 0, 200, 50));
        }

        // Font should still be valid and un-disposed
        Assert.NotNull(btn.Font);
        Assert.True(btn.Font.Height > 0);
    }
}
