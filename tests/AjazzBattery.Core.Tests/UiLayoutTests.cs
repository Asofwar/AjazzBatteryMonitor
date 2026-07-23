using Xunit;
using AjazzBattery.App;
using AjazzBattery.App.UI.Controls;
using AjazzBattery.App.UI.Notifications;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;
using AjazzBattery.Hid;
using AjazzBattery.Storage;

namespace AjazzBattery.Core.Tests;

public class UiLayoutTests
{
    private (MainForm Form, BatteryMonitorEngine Engine) CreateForm()
    {
        var transport = new Win32HidTransport();
        var dummyNotif = new BatteryNotificationService(new FakeNotificationTransport(), new FakeNotificationTransport());
        var dummyEngine = new BatteryMonitorEngine(Array.Empty<IMouseBatteryProvider>(), transport, new DummyNotificationBridge(), st => { });
        var form = new MainForm(dummyEngine, dummyNotif, new WindowsAutoStartManager(), new BatteryHistoryStorage());
        _ = form.Handle; // Force HWND handle creation so control properties are active
        return (form, dummyEngine);
    }

    private class DummyNotificationBridge : INotificationService
    {
        public void NotifyLowBattery(int percentage, string model) { }
        public void NotifyChargingStarted(string model) { }
        public void NotifyChargingCompleted(string model) { }
        public void NotifyLongDisconnection(string model) { }
        public void NotifyDeviceConflict(string message) { }
    }

    [Fact]
    public void TestNavigationButtons_AreAllThreeVisibleAndInCorrectRow()
    {
        var (form, _) = CreateForm();

        Assert.NotNull(form.TabOverviewButton);
        Assert.NotNull(form.TabHistoryButton);
        Assert.NotNull(form.TabSettingsButton);

        Assert.NotNull(form.TabOverviewButton.Parent);
        Assert.NotNull(form.TabHistoryButton.Parent);
        Assert.NotNull(form.TabSettingsButton.Parent);

        Assert.False(form.TabOverviewButton.IsDisposed);
        Assert.False(form.TabHistoryButton.IsDisposed);
        Assert.False(form.TabSettingsButton.IsDisposed);

        Assert.Equal("Обзор", form.TabOverviewButton.Text);
        Assert.Equal("История", form.TabHistoryButton.Text);
        Assert.Equal("Настройки", form.TabSettingsButton.Text);

        // Validate runtime layout invariants
        form.ValidateNavigationInvariants();
    }

    [Fact]
    public void TestHeaderBadges_DoNotContainVerticalSeparatorOrOverlap()
    {
        var (form, _) = CreateForm();

        var status = new BatteryStatus(
            IsPresent: true,
            Percent: 88,
            IsCharging: false,
            IsFullyCharged: false,
            IsSleeping: false,
            ConnectionMode: ConnectionMode.BluetoothLe,
            Timestamp: DateTimeOffset.UtcNow,
            Confidence: StatusConfidence.High,
            DiagnosticMessage: "OK",
            State: ProviderState.Connected,
            ActiveTransport: "Bluetooth LE GATT"
        );

        form.UpdateUi(status);

        string treeStr = form.DumpUiTree();
        Assert.DoesNotContain("|", treeStr);
        Assert.DoesNotContain("[Подключена] [Отключено]", treeStr);
    }

    [Fact]
    public void TestStateMapping_For88PercentBle_HasZeroDisconnectedOccurrences()
    {
        var (form, _) = CreateForm();

        var status = new BatteryStatus(
            IsPresent: true,
            Percent: 88,
            IsCharging: false,
            IsFullyCharged: false,
            IsSleeping: false,
            ConnectionMode: ConnectionMode.BluetoothLe,
            Timestamp: DateTimeOffset.UtcNow,
            Confidence: StatusConfidence.High,
            DiagnosticMessage: "OK",
            State: ProviderState.Connected,
            ActiveTransport: "Bluetooth LE GATT"
        );

        form.UpdateUi(status);

        string treeStr = form.DumpUiTree();
        Assert.Contains("Bluetooth LE", treeStr);
        Assert.Contains("Подключена", treeStr);
        Assert.DoesNotContain("Отключено", treeStr);
    }

    [Fact]
    public void TestStatusCard_NoAutoEllipsisAndFullAutonomyText()
    {
        var (form, _) = CreateForm();

        var status = new BatteryStatus(
            IsPresent: true,
            Percent: 88,
            IsCharging: false,
            IsFullyCharged: false,
            IsSleeping: false,
            ConnectionMode: ConnectionMode.BluetoothLe,
            Timestamp: DateTimeOffset.UtcNow,
            Confidence: StatusConfidence.High,
            DiagnosticMessage: "OK",
            State: ProviderState.Connected,
            ActiveTransport: "Bluetooth LE GATT"
        );

        form.UpdateUi(status);

        string treeStr = form.DumpUiTree();
        Assert.Contains("≈ 3 дня", treeStr);
        Assert.DoesNotContain("Примерно 3 дн...", treeStr);
    }
}
