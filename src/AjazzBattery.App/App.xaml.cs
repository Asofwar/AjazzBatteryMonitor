using System.Windows;
using Microsoft.Win32;
using AjazzBattery.Core;
using AjazzBattery.Devices;
using AjazzBattery.Hid;
using AjazzBattery.Storage;
using WinForms = System.Windows.Forms;

namespace AjazzBattery.App;

public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstanceMutex;
    private WinForms.NotifyIcon? _notifyIcon;
    private BatteryMonitorEngine? _engine;
    private MainWindow? _mainWindow;
    private CancellationTokenSource? _pollCts;
    private BatteryHistoryStorage? _storage;
    private WindowsAutoStartManager? _autoStartManager;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool createdNew;
        _singleInstanceMutex = new Mutex(true, "AjazzBatteryMonitor_SingleInstance_Mutex", out createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show(
                "Приложение AJAZZ Battery Monitor уже запущено в системном трее.",
                "AJAZZ Battery Monitor",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            Shutdown();
            return;
        }

        _storage = new BatteryHistoryStorage();
        _autoStartManager = new WindowsAutoStartManager();

        _notifyIcon = new WinForms.NotifyIcon
        {
            Visible = true,
            Text = "AJAZZ AJ179 APEX - Заряд неизвестен"
        };

        var notificationService = new WindowsNotificationService(_notifyIcon);
        IHidTransport transport = new Win32HidTransport();
        var registry = new DeviceProfileRegistry();
        var provider = new AjazzMouseBatteryProvider(transport, registry);

        _engine = new BatteryMonitorEngine(
            new[] { provider },
            transport,
            notificationService,
            OnStatusUpdated
        );

        _mainWindow = new MainWindow(_engine, _autoStartManager, _storage);

        SetupContextMenu();
        _notifyIcon.DoubleClick += (s, ev) => ShowMainWindow();

        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        _pollCts = new CancellationTokenSource();
        _ = RunPollingLoopAsync(_pollCts.Token);

        bool isAutostart = e.Args.Contains("--autostart");
        if (!isAutostart)
        {
            _mainWindow.Show();
        }
    }

    private void SetupContextMenu()
    {
        if (_notifyIcon == null) return;

        var contextMenu = new WinForms.ContextMenuStrip();

        var refreshItem = new WinForms.ToolStripMenuItem("Обновить сейчас", null, async (s, e) =>
        {
            if (_engine != null)
            {
                var st = await _engine.PollOnceAsync(CancellationToken.None);
                _mainWindow?.UpdateUi(st);
            }
        });

        var detailsItem = new WinForms.ToolStripMenuItem("Открыть сведения", null, (s, e) => ShowMainWindow());
        var autoStartItem = new WinForms.ToolStripMenuItem("Запуск вместе с Windows", null, (s, e) => ToggleAutoStart(s));
        autoStartItem.Checked = _autoStartManager?.IsAutoStartEnabled() ?? false;

        var exitItem = new WinForms.ToolStripMenuItem("Выход", null, (s, e) =>
        {
            _pollCts?.Cancel();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Shutdown();
        });

        contextMenu.Items.Add(detailsItem);
        contextMenu.Items.Add(refreshItem);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add(autoStartItem);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void ToggleAutoStart(object? sender)
    {
        if (sender is WinForms.ToolStripMenuItem item && _autoStartManager != null)
        {
            bool nextState = !item.Checked;
            _autoStartManager.SetAutoStart(nextState);
            item.Checked = nextState;
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }

    private void OnStatusUpdated(BatteryStatus status)
    {
        if (_notifyIcon == null) return;

        try
        {
            var icon = TrayIconRenderer.CreateTrayIcon(status);
            _notifyIcon.Icon = icon;

            string pctStr = status.Percent.HasValue ? $"{status.Percent}%" : "заряд неизвестен";
            string stateStr = status.IsCharging == true ? "заряжается" : (status.IsSleeping ? "в режиме сна" : "подключена");
            string timeStr = status.Timestamp.ToString("HH:mm:ss");

            _notifyIcon.Text = $"AJAZZ AJ179 APEX\nЗаряд: {pctStr}\nСостояние: {stateStr}\nОбновлено: {timeStr}";

            _storage?.AppendHistory(status.Percent, status.IsCharging, status.ConnectionMode.ToString());
            _mainWindow?.UpdateUi(status);
        }
        catch { }
    }

    private async Task RunPollingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_engine != null)
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
    }

    private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume && _engine != null)
        {
            await Task.Delay(2000);
            await _engine.PollOnceAsync(CancellationToken.None);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _singleInstanceMutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
