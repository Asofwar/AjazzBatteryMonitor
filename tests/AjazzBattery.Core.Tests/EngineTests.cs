using Xunit;
using AjazzBattery.Core;
using AjazzBattery.Devices;
using AjazzBattery.Hid;

namespace AjazzBattery.Core.Tests;

public class EngineTests
{
    private sealed class SequenceProvider : IMouseBatteryProvider
    {
        private readonly Queue<BatteryStatus> _statuses;
        public string ProviderId => "SequenceProvider";

        public SequenceProvider(params BatteryStatus[] statuses) => _statuses = new(statuses);
        public bool CanHandle(DeviceDescriptor device) => true;
        public Task<BatteryStatus> ReadStatusAsync(DeviceDescriptor device, CancellationToken cancellationToken) =>
            Task.FromResult(_statuses.Dequeue());
    }

    private class TestNotificationService : INotificationService
    {
        public List<int> LowBatteryNotified { get; } = new();
        public bool ChargingStartedNotified { get; private set; }
        public bool ChargingCompletedNotified { get; private set; }
        public bool LongDisconnectionNotified { get; private set; }
        public bool ConflictNotified { get; private set; }

        public void NotifyLowBattery(int percentage, string model) => LowBatteryNotified.Add(percentage);
        public void NotifyChargingStarted(string model) => ChargingStartedNotified = true;
        public void NotifyChargingCompleted(string model) => ChargingCompletedNotified = true;
        public void NotifyLongDisconnection(string model) => LongDisconnectionNotified = true;
        public void NotifyDeviceConflict(string message) => ConflictNotified = true;
    }

    [Fact]
    public async Task PollOnceAsync_ValidDeviceResponse_ReturnsCorrectStatus()
    {
        var mockTransport = new MockHidTransport();
        var registry = new DeviceProfileRegistry();
        var provider = new AjazzMouseBatteryProvider(mockTransport, registry);
        var notify = new TestNotificationService();
        BatteryStatus? updatedStatus = null;

        var engine = new BatteryMonitorEngine(
            new[] { provider },
            mockTransport,
            notify,
            st => updatedStatus = st
        );

        var result = await engine.PollOnceAsync(CancellationToken.None);

        Assert.True(result.IsPresent);
        Assert.Equal(74, result.Percent);
        Assert.NotNull(updatedStatus);
    }

    [Fact]
    public async Task PollOnceAsync_NoDevicesFound_ReturnsUnknownWithoutFake100()
    {
        var mockTransport = new MockHidTransport { ConfiguredDevices = new List<DeviceDescriptor>() };
        var registry = new DeviceProfileRegistry();
        var provider = new AjazzMouseBatteryProvider(mockTransport, registry);
        var notify = new TestNotificationService();

        var engine = new BatteryMonitorEngine(
            new[] { provider },
            mockTransport,
            notify,
            _ => { }
        );

        var result = await engine.PollOnceAsync(CancellationToken.None);

        Assert.False(result.IsPresent);
        Assert.Null(result.Percent);
        Assert.Equal(ProviderState.DeviceNotFound, result.State);
    }

    [Fact]
    public async Task PollOnceAsync_LowBatteryNotification_DebouncedCorrectly()
    {
        var mockTransport = new MockHidTransport
        {
            TransferHandler = (dev, req) => new byte[] { 0x05, 0x00, 0x00, 15, 0x00, 0x01, 0x01, 0x02 } // 15% not charging
        };
        var registry = new DeviceProfileRegistry();
        var provider = new AjazzMouseBatteryProvider(mockTransport, registry);
        var notify = new TestNotificationService();

        var engine = new BatteryMonitorEngine(
            new[] { provider },
            mockTransport,
            notify,
            _ => { }
        );

        await engine.PollOnceAsync(CancellationToken.None);
        await engine.PollOnceAsync(CancellationToken.None); // second poll

        Assert.Single(notify.LowBatteryNotified);
        Assert.Equal(20, notify.LowBatteryNotified[0]);
    }

    [Fact]
    public async Task PollOnceAsync_InvalidFrameResetsChargingDebounce()
    {
        BatteryStatus confirmed = new(true, 50, true, null, false, ConnectionMode.Wireless24G,
            DateTimeOffset.UtcNow, StatusConfidence.High, null, ProviderState.Connected, "HID",
            ChargingConfidence: ChargingConfidence.ProtocolConfirmed);
        BatteryStatus invalid = new(true, null, null, null, false, ConnectionMode.Wireless24G,
            DateTimeOffset.UtcNow, StatusConfidence.Low, "invalid", ProviderState.InvalidFrame, "HID");
        var transport = new MockHidTransport();
        var provider = new SequenceProvider(confirmed, confirmed, invalid, confirmed, confirmed);
        var engine = new BatteryMonitorEngine(new[] { provider }, transport, new TestNotificationService(), _ => { });

        Assert.Null((await engine.PollOnceAsync(CancellationToken.None)).IsCharging);
        Assert.True((await engine.PollOnceAsync(CancellationToken.None)).IsChargingConfirmed);
        Assert.Null((await engine.PollOnceAsync(CancellationToken.None)).IsCharging);
        Assert.Null((await engine.PollOnceAsync(CancellationToken.None)).IsCharging);
        Assert.True((await engine.PollOnceAsync(CancellationToken.None)).IsChargingConfirmed);
    }
}
