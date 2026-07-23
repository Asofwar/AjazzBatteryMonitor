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
        ProductId: 0x402D,
        UsagePage: 0xFF00,
        Usage: 0x0001,
        InterfaceNumber: 1,
        ConnectionMode: ConnectionMode.Wireless24G,
        IsExperimental: false
    );

    [Fact]
    public void ParseResponse_Valid74Percent_ReturnsCorrectValues()
    {
        byte[] response = { 0x00, 0x20, 0x01, 0x00, 74, 0x00 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Equal(74, status.Percent);
        Assert.False(status.IsCharging);
        Assert.False(status.IsSleeping);
        Assert.Equal(StatusConfidence.High, status.Confidence);
    }

    [Fact]
    public void ParseResponse_FullyCharged0xFF_Returns100PercentAndCharging()
    {
        byte[] response = { 0x00, 0x20, 0x01, 0x01, 0xFF, 0x00 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Equal(100, status.Percent);
        Assert.True(status.IsCharging);
        Assert.True(status.IsFullyCharged);
    }

    [Fact]
    public void ParseResponse_SleepingMouseBitSet_ReturnsIsSleepingTrue()
    {
        byte[] response = { 0x00, 0x20, 0x01, 0x02, 50, 0x00 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.True(status.IsPresent);
        Assert.Equal(50, status.Percent);
        Assert.True(status.IsSleeping);
    }

    [Fact]
    public void ParseResponse_ShortBuffer_ReturnsUnknownStatus()
    {
        byte[] response = { 0x00, 0x20 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.False(status.IsPresent);
        Assert.Null(status.Percent);
        Assert.Contains("Некорректный размер", status.DiagnosticMessage);
    }

    [Fact]
    public void ParseResponse_InvalidOpcode_ReturnsUnknownStatus()
    {
        byte[] response = { 0x00, 0x99, 0x01, 0x00, 50, 0x00 };
        var status = YichipBatteryParser.ParseResponse(_dummyDevice, response, DateTimeOffset.UtcNow);

        Assert.False(status.IsPresent);
        Assert.Null(status.Percent);
        Assert.Contains("Неизвестный report opcode", status.DiagnosticMessage);
    }
}
