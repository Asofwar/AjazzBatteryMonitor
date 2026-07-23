using Xunit;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;

namespace AjazzBattery.Core.Tests;

public class NotificationEngineTests
{
    private static BatteryStatus CreateStatus(int? percent, bool isCharging = false, bool isSleeping = false, StatusConfidence conf = StatusConfidence.High)
    {
        return new BatteryStatus(
            IsPresent: true,
            Percent: percent,
            IsCharging: isCharging,
            IsFullyCharged: percent == 100,
            IsSleeping: isSleeping,
            ConnectionMode: ConnectionMode.BluetoothLe,
            Timestamp: DateTimeOffset.UtcNow,
            Confidence: conf,
            DiagnosticMessage: "Test Status",
            State: percent.HasValue ? ProviderState.Connected : ProviderState.TelemetryNotReady,
            ActiveTransport: "BLE"
        );
    }

    [Fact]
    public async Task Rule1_DownwardCrossing20Percent_SendsOneNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(21));
        await service.ProcessBatteryUpdateAsync(CreateStatus(20));

        Assert.Single(primary.SentNotifications);
        Assert.Equal("threshold_20", primary.SentNotifications[0].Category);
    }

    [Fact]
    public async Task Rule2_StayAt19Percent_NoRepeatNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(21));
        await service.ProcessBatteryUpdateAsync(CreateStatus(20));
        await service.ProcessBatteryUpdateAsync(CreateStatus(19));

        Assert.Single(primary.SentNotifications);
    }

    [Fact]
    public void Rule3_DownwardCrossing10Percent_Sends10PercentNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var state = new BatteryNotificationState { LastPercent = 11, InitialReadingProcessed = true, TriggeredThresholds = new() { 20 } };
        var service = new BatteryNotificationService(primary, fallback, state: state);

        service.ProcessBatteryUpdateAsync(CreateStatus(10)).Wait();

        Assert.Single(primary.SentNotifications);
        Assert.Equal("threshold_10", primary.SentNotifications[0].Category);
    }

    [Fact]
    public void Rule4_DownwardCrossing5Percent_SendsCriticalNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var state = new BatteryNotificationState { LastPercent = 6, InitialReadingProcessed = true, TriggeredThresholds = new() { 20, 10 } };
        var service = new BatteryNotificationService(primary, fallback, state: state);

        service.ProcessBatteryUpdateAsync(CreateStatus(5)).Wait();

        Assert.Single(primary.SentNotifications);
        Assert.Equal("threshold_5", primary.SentNotifications[0].Category);
    }

    [Fact]
    public async Task Rule5_FirstLaunchAt4Percent_SendsOnlyCritical5PercentNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(4));

        Assert.Single(primary.SentNotifications);
        Assert.Equal("threshold_5", primary.SentNotifications[0].Category);
    }

    [Fact]
    public async Task Rule6_FirstLaunchAt9Percent_SendsOnly10PercentNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(9));

        Assert.Single(primary.SentNotifications);
        Assert.Equal("threshold_10", primary.SentNotifications[0].Category);
    }

    [Fact]
    public async Task Rule7_RestartAt9PercentWithPersistedState_NoRepeatNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var state = new BatteryNotificationState { LastPercent = 9, InitialReadingProcessed = true, TriggeredThresholds = new() { 10, 20 } };
        var service = new BatteryNotificationService(primary, fallback, state: state);

        await service.ProcessBatteryUpdateAsync(CreateStatus(9));

        Assert.Empty(primary.SentNotifications);
    }

    [Fact]
    public async Task Rule8_Hysteresis_RearmsThresholdWhenBatteryRises()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var state = new BatteryNotificationState { LastPercent = 11, InitialReadingProcessed = true, TriggeredThresholds = new() { 20 } };
        var service = new BatteryNotificationService(primary, fallback, state: state);

        await service.ProcessBatteryUpdateAsync(CreateStatus(10)); // Triggered 10%
        Assert.Single(primary.SentNotifications);

        // Rise to 16% (10 + 5 hysteresis)
        await service.ProcessBatteryUpdateAsync(CreateStatus(16));

        // Drop again to 10%
        await service.ProcessBatteryUpdateAsync(CreateStatus(10));
        Assert.Equal(2, primary.SentNotifications.Count);
    }

    [Fact]
    public async Task Rule9_ValueUnknown_NoNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(null));

        Assert.Empty(primary.SentNotifications);
    }

    [Fact]
    public async Task Rule10_ValueStale_NoNotification()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(5, conf: StatusConfidence.Stale));

        Assert.Empty(primary.SentNotifications);
    }

    [Fact]
    public async Task Rule11_MouseCharging_LowBatteryNotificationSuppressed()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(5, isCharging: true));

        Assert.Single(primary.SentNotifications);
        Assert.Equal("charging_started", primary.SentNotifications[0].Category);
    }

    [Fact]
    public async Task Rule12_SinglePollAnomaly_IgnoredWithoutSecondRead()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(88));
        await service.ProcessBatteryUpdateAsync(CreateStatus(3)); // Anomaly drop > 20%
        await service.ProcessBatteryUpdateAsync(CreateStatus(88)); // Restored

        Assert.Empty(primary.SentNotifications);
    }

    [Fact]
    public async Task Rule13_ConfirmedAnomaly_NotificationTriggeredOnSecondRead()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(88));
        await service.ProcessBatteryUpdateAsync(CreateStatus(3)); // 1st read (ignored)
        await service.ProcessBatteryUpdateAsync(CreateStatus(3)); // 2nd read (confirmed)

        Assert.Single(primary.SentNotifications);
    }

    [Fact]
    public async Task Rule16_ChargingStarted_SendsNotificationOnce()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(15, isCharging: true));
        await service.ProcessBatteryUpdateAsync(CreateStatus(16, isCharging: true));

        Assert.Single(primary.SentNotifications);
        Assert.Equal("charging_started", primary.SentNotifications[0].Category);
    }

    [Fact]
    public async Task Rule18_FullyCharged_SendsNotificationOnce()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.ProcessBatteryUpdateAsync(CreateStatus(99, isCharging: true));
        await service.ProcessBatteryUpdateAsync(CreateStatus(100, isCharging: true));
        await service.ProcessBatteryUpdateAsync(CreateStatus(100, isCharging: true));

        Assert.Equal(2, primary.SentNotifications.Count);
        Assert.Equal("charging_started", primary.SentNotifications[0].Category);
        Assert.Equal("fully_charged", primary.SentNotifications[1].Category);
    }

    [Fact]
    public async Task Rule20_TestNotification_DoesNotAlterPersistedState()
    {
        var primary = new FakeNotificationTransport();
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.SendTestNotificationAsync();

        Assert.Single(primary.SentNotifications);
        Assert.Equal("test_notification", primary.SentNotifications[0].Category);
        Assert.Empty(service.State.TriggeredThresholds);
    }

    [Fact]
    public async Task Rule21_PrimaryTransportError_UsesFallbackTransport()
    {
        var primary = new FakeNotificationTransport { ShouldFail = true };
        var fallback = new FakeNotificationTransport();
        var service = new BatteryNotificationService(primary, fallback);

        await service.SendTestNotificationAsync();

        Assert.Empty(primary.SentNotifications);
        Assert.Single(fallback.SentNotifications);
    }
}
