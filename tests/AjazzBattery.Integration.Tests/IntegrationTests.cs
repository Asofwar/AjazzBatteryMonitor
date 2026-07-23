using Xunit;
using AjazzBattery.Core;
using AjazzBattery.Devices;
using AjazzBattery.Hid;

namespace AjazzBattery.Integration.Tests;

public class IntegrationTests
{
    private class DummyNotificationService : INotificationService
    {
        public void NotifyLowBattery(int percentage, string model) { }
        public void NotifyChargingStarted(string model) { }
        public void NotifyChargingCompleted(string model) { }
        public void NotifyLongDisconnection(string model) { }
        public void NotifyDeviceConflict(string message) { }
    }

    [Fact]
    public async Task FullSystemFlow_MockTransportToEngine_ExecutesSuccessfully()
    {
        var mockTransport = new MockHidTransport();
        var registry = new DeviceProfileRegistry();
        var provider = new AjazzMouseBatteryProvider(mockTransport, registry);
        var notify = new DummyNotificationService();

        BatteryStatus? lastStatus = null;
        var engine = new BatteryMonitorEngine(
            new[] { provider },
            mockTransport,
            notify,
            s => lastStatus = s
        );

        var status = await engine.PollOnceAsync(CancellationToken.None);

        Assert.NotNull(status);
        Assert.True(status.IsPresent);
        Assert.Equal(74, status.Percent);
        Assert.NotNull(lastStatus);
        Assert.Equal(74, lastStatus.Percent);
    }

    [Fact]
    public async Task FullSystemFlow_LockedInterface_HandlesConflictGracefully()
    {
        var mockTransport = new MockHidTransport
        {
            SimulateException = new InvalidOperationException("Официальное приложение AJAZZ заняло интерфейс устройства.")
        };
        var registry = new DeviceProfileRegistry();
        var provider = new AjazzMouseBatteryProvider(mockTransport, registry);
        var notify = new DummyNotificationService();

        var engine = new BatteryMonitorEngine(
            new[] { provider },
            mockTransport,
            notify,
            _ => { }
        );

        var status = await engine.PollOnceAsync(CancellationToken.None);

        Assert.False(status.IsPresent);
        Assert.Null(status.Percent);
        Assert.Contains("заняло интерфейс", status.DiagnosticMessage);
    }
}
