using System.Windows.Forms;
using Microsoft.Win32;
using AjazzBattery.Core;
using AjazzBattery.Devices;
using AjazzBattery.Hid;
using AjazzBattery.Bluetooth;
using AjazzBattery.Storage;

namespace AjazzBattery.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly BatteryMonitorEngine _engine;
    private readonly BatteryHistoryStorage _storage;
    private readonly WindowsAutoStartManager _autoStartManager;
    private readonly CancellationTokenSource _pollCts;
    private DetailsForm? _detailsForm;
    private Icon? _currentIcon;
    private readonly bool _isSmokeTest;

    public TrayApplicationContext(bool isSmokeTest)
    {
        _isSmokeTest = isSmokeTest;

        Logger.Log("TRAY", "Tray initialization started");

        _storage = new BatteryHistoryStorage();
        _autoStartManager = new WindowsAutoStartManager();
        _pollCts = new CancellationTokenSource();

        // 1. Create NotifyIcon and Context Menu IMMEDIATELY before starting hardware operations
        _notifyIcon = new NotifyIcon
        {
            Text = "AJAZZ Battery Monitor — Заряд неизвестен",
            Visible = true
        };

        UpdateTrayIconInternal(BatteryStatus.CreateUnknown());
        Logger.Log("TRAY", "Tray icon created");
        Logger.Log("TRAY", "Tray icon made visible");

        SetupContextMenu();
        _notifyIcon.DoubleClick += (s, e) => ShowDetailsForm();

        // 2. Initialize Providers & Engine
        Logger.Log("TRAY", "Monitor engine starting");
        IHidTransport transport = new Win32HidTransport();
        var registry = new DeviceProfileRegistry();

        Logger.Log("BLE", "BLE enumeration started");
        var bleProvider = new BleBatteryProvider();

        Logger.Log("HID", "HID enumeration started");
        var hidProvider = new AjazzMouseBatteryProvider(transport, registry);

        var notificationService = new WindowsNotificationService(_notifyIcon);

        // BLE provider takes priority over HID when paired/connected
        _engine = new BatteryMonitorEngine(
            new IMouseBatteryProvider[] { bleProvider, hidProvider },
            transport,
            notificationService,
            OnStatusUpdated
        );

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;

        // 3. Start Background Polling
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
    }

    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem("Открыть", null, (s, e) => ShowDetailsForm());
        var refreshItem = new ToolStripMenuItem("Обновить заряд", null, async (s, e) =>
        {
            var st = await _engine.PollOnceAsync(CancellationToken.None);
            OnStatusUpdated(st);
        });

        var diagItem = new ToolStripMenuItem("Диагностика", null, (s, e) =>
        {
            var diagForm = new DiagnosticsForm(_engine.CurrentStatus, "BleOrHidProvider");
            diagForm.ShowDialog();
        });

        var logsItem = new ToolStripMenuItem("Открыть папку логов", null, (s, e) =>
        {
            try { System.Diagnostics.Process.Start("explorer.exe", Logger.LogDirectoryPath); } catch { }
        });

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

        var exitItem = new ToolStripMenuItem("Выход", null, (s, e) => ExitApplication());

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(refreshItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(diagItem);
        contextMenu.Items.Add(logsItem);
        contextMenu.Items.Add(autoStartItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void ShowDetailsForm()
    {
        if (_detailsForm == null || _detailsForm.IsDisposed)
        {
            _detailsForm = new DetailsForm(_engine, _autoStartManager, _storage);
        }
        _detailsForm.Show();
        _detailsForm.BringToFront();
        _detailsForm.Activate();
    }

    private void OnStatusUpdated(BatteryStatus status)
    {
        try
        {
            UpdateTrayIconInternal(status);

            string pctStr = status.Percent.HasValue ? $"{status.Percent}%" : "заряд неизвестен";
            string stateStr = status.IsCharging == true ? "заряжается" : (status.IsSleeping ? "в режиме сна" : (status.IsPresent ? "подключена" : "отключена"));
            string timeStr = status.Timestamp.ToString("HH:mm:ss");

            _notifyIcon.Text = $"AJAZZ AJ179 APEX\nЗаряд: {pctStr}\nСостояние: {stateStr}\nОбновлено: {timeStr}";

            _storage.AppendHistory(status.Percent, status.IsCharging, status.ConnectionMode.ToString());
            _detailsForm?.UpdateUi(status);
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

        // Dispose old icon after switching handle to prevent GDI leak
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

        ExitThread();
    }
}
