using Xunit;
using AjazzBattery.Core;
using AjazzBattery.Devices;
using AjazzBattery.Hid;

namespace AjazzBattery.Protocol.Tests;

public class ParserTests
{
    private readonly DeviceDescriptor _dummyDevice = new(
        DevicePath: "DUMMY_PATH",
        ModelName: "AJAZZ AJ179 APEX",
        VendorId: 0x3151,
        ProductId: 0x5007,
        UsagePage: 0xFFFF,
        Usage: 0x0002,
        InterfaceNumber: 1,
        ConnectionMode: ConnectionMode.DockStation,
        ConfirmationStatus: ConfirmationStatus.ConfirmedOnDevice
    );

    [Fact]
    public void ParseResponse_Valid74Percent_DoesNotInferCharging()
    {
        // Valid battery frame; byte 4 is not an established charging flag.
        byte[] response = { 0x05, 0x00, 0x00, 0x4A, 0x00, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Equal(74, status.Percent);
        Assert.Null(status.IsCharging);
        Assert.Null(status.IsFullyCharged);
        Assert.Equal(StatusConfidence.High, status.Confidence);
        Assert.Equal(ProviderState.Connected, status.State);
    }

    [Fact]
    public void ParseResponse_100Percent_DoesNotInferChargingOrFull()
    {
        // A full percentage alone is not evidence of charging or external power.
        byte[] response = { 0x05, 0x00, 0x00, 0x64, 0x01, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Equal(100, status.Percent);
        Assert.Null(status.IsCharging);
        Assert.Null(status.IsFullyCharged);
    }

    [Fact]
    public void ParseResponse_SleepingOffDock_DoesNotShowCharging()
    {
        // Regression: 2.4 GHz mouse asleep off dock, 88% and no fresh
        // hardware-confirmed charging telemetry. The sleep flag is independent
        // from charging and must not cause a lightning/charging UI state.
        byte[] response = { 0x05, 0x00, 0x00, 0x58, 0x01, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.Equal(88, status.Percent);
        Assert.True(status.IsSleeping);
        Assert.Null(status.IsCharging);
        Assert.Null(status.IsFullyCharged);
    }

    [Fact]
    public void ParseResponse_HardwareConfirmed1Percent_Returns1Percent()
    {
        byte[] response = { 0x05, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Equal(1, status.Percent);
    }

    [Fact]
    public void ParseResponse_AllZeroFrame_ReturnsTelemetryNotReady()
    {
        byte[] response = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Null(status.Percent);
        Assert.Equal(ProviderState.TelemetryNotReady, status.State);
    }

    [Fact]
    public void ParseResponse_InvalidHeader1_RejectsFrame()
    {
        byte[] response = { 0x05, 0x01, 0x00, 0x4A, 0x01, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.Null(status.Percent);
        Assert.Equal(ProviderState.InvalidFrame, status.State);
    }

    [Fact]
    public void ParseResponse_InvalidHeader2_RejectsFrame()
    {
        byte[] response = { 0x05, 0x00, 0x01, 0x4A, 0x01, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.Null(status.Percent);
        Assert.Equal(ProviderState.InvalidFrame, status.State);
    }

    [Fact]
    public void ParseResponse_PercentGreaterThan100_RejectsFrame()
    {
        byte[] response = { 0x05, 0x00, 0x00, 0xFF, 0x01, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.Null(status.Percent);
        Assert.Equal(ProviderState.InvalidFrame, status.State);
    }

    [Fact]
    public void ParseResponse_ShortBuffer_RejectsFrame()
    {
        byte[] response = { 0x05, 0x00 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.Null(status.Percent);
        Assert.Equal(ProviderState.InvalidFrame, status.State);
    }

    [Fact]
    public async Task ReadStatus_UnknownCollection_DoesNotSendFeatureReport()
    {
        using var transport = new MockHidTransport();
        var provider = new AjazzMouseBatteryProvider(transport, new DeviceProfileRegistry());
        var unknown = _dummyDevice with { VendorId = 0xFFFF, ProductId = 0x0001 };

        var status = await provider.ReadStatusAsync(unknown, CancellationToken.None);

        Assert.Equal(ProviderState.UnsupportedProtocol, status.State);
        Assert.False(transport.SetFeatureCalled);
    }

    [Fact]
    public void FindMatchingProfile_RequiresControlCollectionIdentity()
    {
        var registry = new DeviceProfileRegistry();
        var wrongUsage = _dummyDevice with { UsagePage = 0x0001, Usage = 0x0002 };

        Assert.Null(registry.FindMatchingProfile(wrongUsage));
    }
}
