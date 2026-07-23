using System.Windows.Forms;
using Microsoft.Win32;
using AjazzBattery.App.UI.Notifications;
using AjazzBattery.App.UI.Rendering;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;
using AjazzBattery.Devices;
using AjazzBattery.Hid;
using AjazzBattery.Bluetooth;
using AjazzBattery.Storage;

namespace AjazzBattery.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private static int _activeTrayInstanceCount = 0;

    private readonly NotifyIcon _notifyIcon;
    private readonly BatteryMonitorEngine _engine;
    private readonly BatteryNotificationService _notificationService;
    private readonly BatteryHistoryStorage _storage;
    private readonly WindowsAutoStartManager _autoStartManager;
    private readonly CancellationTokenSource _pollCts;
    private MainForm? _mainForm;
    private Icon? _currentIcon;
    private readonly bool _isSmokeTest;
    private readonly bool _isSmokeTestUi;

    public TrayApplicationContext(bool isSmokeTest = false, bool isSmokeTestUi = false, int? mockPercent = null)
    {
        _isSmokeTest = isSmokeTest;
        _isSmokeTestUi = isSmokeTestUi;

        Interlocked.Increment(ref _activeTrayInstanceCount);
        Logger.Log("TRAY", $"Tray instance created (Active instance count: {_activeTrayInstanceCount})");

        _storage = new BatteryHistoryStorage();
        _autoStartManager = new WindowsAutoStartManager();
        _pollCts = new CancellationTokenSource();

        // 1. Create NotifyIcon & Context Menu IMMEDIATELY before starting hardware operations
        _notifyIcon = new NotifyIcon
        {
            Text = "AJAZZ AJ179 APEX — Заряд неизвестен",
            Visible = true
        };

        UpdateTrayIconInternal(BatteryStatus.CreateUnknown());
        Logger.Log("TRAY", "Tray icon created and made visible");

        // 2. Initialize Providers & Notification Engine
        IHidTransport transport = new Win32HidTransport();
        var registry = new DeviceProfileRegistry();

        var bleProvider = new BleBatteryProvider();
        var hidProvider = new AjazzMouseBatteryProvider(transport, registry);

        var primaryTransport = new WindowsToastNotificationTransport();
        var fallbackTransport = new NotifyIconBalloonNotificationTransport(_notifyIcon);
        _notificationService = new BatteryNotificationService(primaryTransport, fallbackTransport);

        _engine = new BatteryMonitorEngine(
            new IMouseBatteryProvider[] { bleProvider, hidProvider },
            transport,
            new LegacyNotificationServiceBridge(_notificationService),
            OnStatusUpdated
        );

        SetupContextMenu();
        _notifyIcon.DoubleClick += (s, e) => ShowMainForm();

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;

        // 3. Start Background Polling Loop
        _ = RunPollingLoopAsync(_pollCts.Token);
        Logger.Log("MAIN", "Application startup completed");

        if (_isSmokeTest)
        {
            Logger.Log("SMOKE", "Smoke test mode active - scheduling clean exit in 10s...");
            var timer = new System.Windows.Forms.Timer { Interval = 10000 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Logger.Log("SMOKE", "Smoke test completed successfully");
                ExitApplication();
            };
            timer.Start();
        }
        else if (_isSmokeTestUi)
        {
            Logger.Log("SMOKE_UI", "Smoke test UI mode active - opening MainForm...");
            ShowMainForm();
        }
    }

    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        var headerItem = new ToolStripMenuItem("AJAZZ AJ179 APEX") { Enabled = false };
        var subHeaderItem = new ToolStripMenuItem("— · —") { Enabled = false };

        var openItem = new ToolStripMenuItem("Открыть монитор", null, (s, e) => ShowMainForm());
        var refreshItem = new ToolStripMenuItem("Обновить заряд", null, async (s, e) =>
        {
            var st = await _engine.PollOnceAsync(CancellationToken.None);
            OnStatusUpdated(st);
        });

        var historyItem = new ToolStripMenuItem("История", null, (s, e) => ShowMainForm());

        var autoStartItem = new ToolStripMenuItem("Автозапуск", null, (s, e) =>
        {
            if (s is ToolStripMenuItem item)
            {
                bool nextState = !item.Checked;
                _autoStartManager.SetAutoStart(nextState);
                item.Checked = nextState;
            }
        });
        autoStartItem.Checked = _autoStartManager.IsAutoStartEnabled();

        var diagItem = new ToolStripMenuItem("Диагностика", null, (s, e) =>
        {
            var diagForm = new DiagnosticsForm(_engine);
            diagForm.ShowDialog();
        });

        var exitItem = new ToolStripMenuItem("Выход", null, (s, e) => ExitApplication());

        contextMenu.Items.Add(headerItem);
        contextMenu.Items.Add(subHeaderItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(refreshItem);
        contextMenu.Items.Add(historyItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(autoStartItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(diagItem);
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    public void ShowMainForm()
    {
        if (_mainForm == null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_engine, _notificationService, _autoStartManager, _storage);
        }
        _mainForm.Show();
        _mainForm.BringToFront();
        _mainForm.Activate();
    }

    private void OnStatusUpdated(BatteryStatus status)
    {
        try
        {
            UpdateTrayIconInternal(status);

            string pctStr = status.Percent.HasValue ? $"{status.Percent}%" : "заряд неизвестен";
            string stateStr = status.IsCharging == true ? "заряжается ⚡" : (status.IsSleeping ? "в режиме сна" : (status.IsPresent ? "подключена" : "отключена"));
            string connStr = status.ActiveTransport.Contains("BLE") ? "Bluetooth LE" : (status.ActiveTransport.Contains("HID") ? "2.4 GHz" : "—");

            _notifyIcon.Text = $"AJAZZ AJ179 APEX\nЗаряд: {pctStr}\nСостояние: {stateStr}\nТранспорт: {connStr}";

            if (_notifyIcon.ContextMenuStrip != null && _notifyIcon.ContextMenuStrip.Items.Count > 1)
            {
                _notifyIcon.ContextMenuStrip.Items[1].Text = $"{pctStr} · {connStr}";
            }

            _storage.AppendHistory(status.Percent, status.IsCharging, status.ConnectionMode.ToString());
            _mainForm?.UpdateUi(status);

            _ = _notificationService.ProcessBatteryUpdateAsync(status);
        }
        catch (Exception ex)
        {
            Logger.LogException("STATUS_UPDATE", ex);
        }
    }

    private void UpdateTrayIconInternal(BatteryStatus status)
    {
        Icon newIcon = TrayIconRenderer.CreateTrayIcon(status);
        Icon? oldIcon = _currentIcon;
        _currentIcon = newIcon;
        _notifyIcon.Icon = newIcon;

        // Dispose previous Icon AFTER handle assignment to prevent GDI leaks
        oldIcon?.Dispose();
    }

    private async Task RunPollingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _engine.PollOnceAsync(cancellationToken);
            var interval = _engine.GetRecommendedNextPollInterval();
            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Logger.Log("POWER", "System resumed from sleep - polling device...");
            await Task.Delay(2000);
            await _engine.PollOnceAsync(CancellationToken.None);
        }
    }

    private async void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            Logger.Log("POWER", "Session unlocked - polling device...");
            await Task.Delay(2000);
            await _engine.PollOnceAsync(CancellationToken.None);
        }
    }

    private void ExitApplication()
    {
        Logger.Log("SHUTDOWN", "Application shutting down...");
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;

        _pollCts.Cancel();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _currentIcon?.Dispose();

        Interlocked.Decrement(ref _activeTrayInstanceCount);
        Logger.Log("TRAY", $"Tray instance disposed (Remaining count: {_activeTrayInstanceCount})");

        ExitThread();
    }

    private class LegacyNotificationServiceBridge : INotificationService
    {
        private readonly BatteryNotificationService _service;
        public LegacyNotificationServiceBridge(BatteryNotificationService service) => _service = service;

        public void NotifyLowBattery(int percentage, string model) { }
        public void NotifyChargingStarted(string model) { }
        public void NotifyChargingCompleted(string model) { }
        public void NotifyLongDisconnection(string model) { }
        public void NotifyDeviceConflict(string message) { }
    }
}
