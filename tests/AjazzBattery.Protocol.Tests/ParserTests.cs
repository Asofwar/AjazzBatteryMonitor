using Xunit;
using AjazzBattery.Core;
using AjazzBattery.Devices;

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
    public void ParseResponse_HardwareConfirmed74Percent_ReturnsCorrectValues()
    {
        // Hardware frame: 05 00 00 4A 00 01 01 02 (74% discharging)
        byte[] response = { 0x05, 0x00, 0x00, 0x4A, 0x00, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Equal(74, status.Percent);
        Assert.False(status.IsCharging);
        Assert.Equal(StatusConfidence.High, status.Confidence);
        Assert.Equal(ProviderState.Connected, status.State);
    }

    [Fact]
    public void ParseResponse_HardwareConfirmed100Percent_Returns100AndCharging()
    {
        // Hardware frame: 05 00 00 64 01 01 01 02
        byte[] response = { 0x05, 0x00, 0x00, 0x64, 0x01, 0x01, 0x01, 0x02 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Equal(100, status.Percent);
        Assert.True(status.IsCharging);
        Assert.True(status.IsFullyCharged);
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
}
