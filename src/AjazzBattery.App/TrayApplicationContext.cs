using System.Windows.Forms;
using Microsoft.Win32;
using AjazzBattery.App.UI.Notifications;
using AjazzBattery.App.UI.Rendering;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;
using AjazzBattery.Core.Time;
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
    private readonly LaunchMode _launchMode;
    private MainForm? _mainForm;
    private Icon? _currentIcon;
    private System.Windows.Forms.Timer? _smokeTestTimer;
    private readonly bool _isSmokeTest;
    private readonly bool _isSmokeTestUi;
    private bool _hasShownTrayHint = false;

    public TrayApplicationContext(
        LaunchMode launchMode = LaunchMode.Overview,
        bool isSmokeTest = false,
        bool isSmokeTestUi = false,
        int? mockPercent = null)
    {
        _launchMode = launchMode;
        _isSmokeTest = isSmokeTest;
        _isSmokeTestUi = isSmokeTestUi;

        Interlocked.Increment(ref _activeTrayInstanceCount);
        Logger.Log("TRAY", $"Tray instance created (Active instance count: {_activeTrayInstanceCount})");

        _storage = new BatteryHistoryStorage();
        _autoStartManager = new WindowsAutoStartManager();
        _pollCts = new CancellationTokenSource();

        // 1. Create NotifyIcon & Context Menu IMMEDIATELY before hardware operations
        _notifyIcon = new NotifyIcon
        {
            Text = "AJAZZ AJ179 APEX — Заряд неизвестен",
            Visible = true
        };

        // Use the embedded app icon as the initial tray icon until battery status is available
        // TrayIconRenderer will produce dynamic battery-level icons after first poll
        UpdateTrayIconInternal(BatteryStatus.CreateUnknown());
        Logger.Log("TRAY", "Tray icon created and made visible");

        // 2. Initialize Providers & Notification Engine
        IHidTransport transport = new Win32HidTransport();
        var registry = new DeviceProfileRegistry();

        var bleProvider = new BleBatteryProvider();
        var hidProvider = new AjazzMouseBatteryProvider(transport, registry);

        var primaryTransport = new WindowsToastNotificationTransport();
        var fallbackTransport = new NotifyIconBalloonNotificationTransport(_notifyIcon);
        _notificationService = new BatteryNotificationService(
            primaryTransport,
            fallbackTransport,
            _storage.LoadNotificationSettings(),
            _storage.LoadNotificationState(),
            _storage.SaveNotificationState);

        _engine = new BatteryMonitorEngine(
            new IMouseBatteryProvider[] { bleProvider, hidProvider },
            transport,
            new LegacyNotificationServiceBridge(_notificationService),
            OnStatusUpdated
        );

        SetupContextMenu();
        _notifyIcon.DoubleClick += (s, e) => ShowOverview();

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;

        // 3. Start Background Polling Loop
        _ = RunPollingLoopAsync(_pollCts.Token);
        Logger.Log("MAIN", "Application startup completed");

        // 4. Handle launch mode
        if (_isSmokeTest)
        {
            Logger.Log("SMOKE", "Smoke test mode active - scheduling clean exit in 10s...");
            _smokeTestTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            _smokeTestTimer.Tick += (s, e) =>
            {
                _smokeTestTimer.Stop();
                Logger.Log("SMOKE", "Smoke test completed successfully");
                ExitApplication();
            };
            _smokeTestTimer.Start();
        }
        else if (_launchMode == LaunchMode.Overview || _isSmokeTestUi)
        {
            Logger.Log("STARTUP", "Normal launch mode: opening MainForm on Overview tab");
            ShowOverview();
        }
        else if (_launchMode == LaunchMode.Settings)
        {
            Logger.Log("STARTUP", "Settings launch mode: opening MainForm on Settings tab");
            ShowSettings();
        }
        else
        {
            Logger.Log("STARTUP", "Background launch mode: running silently in system tray");
        }
    }

    /// <summary>
    /// Allows Program.cs to invoke actions on the UI thread via the NotifyIcon handle.
    /// </summary>
    public void BeginInvokeOnUiThread(Action action)
    {
        if (_notifyIcon.ContextMenuStrip?.IsHandleCreated == true)
        {
            _notifyIcon.ContextMenuStrip.BeginInvoke(action);
        }
        else if (_mainForm?.IsHandleCreated == true)
        {
            _mainForm.BeginInvoke(action);
        }
        else
        {
            // Best effort: post to the thread pool and let it happen
            Logger.Log("IPC", "BeginInvokeOnUiThread: no handle available, posting to task");
            _ = Task.Run(() => { try { action(); } catch { } });
        }
    }

    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        var headerItem    = new ToolStripMenuItem("AJAZZ AJ179 APEX") { Enabled = false };
        var subHeaderItem = new ToolStripMenuItem("— · —") { Enabled = false };

        var openItem    = new ToolStripMenuItem("Открыть монитор",  null, (s, e) => ShowOverview());
        var refreshItem = new ToolStripMenuItem("Обновить заряд",   null, async (s, e) =>
        {
            var st = await _engine.PollOnceAsync(CancellationToken.None);
            OnStatusUpdated(st);
        });

        var settingsItem = new ToolStripMenuItem("Настройки",  null, (s, e) => ShowSettings());
        var diagItem     = new ToolStripMenuItem("Диагностика", null, (s, e) =>
        {
            var diagForm = new DiagnosticsForm(_engine);
            diagForm.ShowDialog();
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

        contextMenu.Items.Add(headerItem);
        contextMenu.Items.Add(subHeaderItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(refreshItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(autoStartItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(diagItem);
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Public window activation methods called by IPC dispatcher
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens (or activates) the main window and navigates to the Overview tab.
    /// Called on normal launch, repeated launch, tray double-click, and IPC ShowOverview.
    /// </summary>
    public void ShowOverview()
    {
        EnsureMainFormCreated();
        _mainForm!.SelectSection(AppSection.Overview);
        ShowMainWindowInternal();
    }

    /// <summary>
    /// Opens (or activates) the main window and navigates to the Settings tab.
    /// Called on --settings launch and IPC ShowSettings.
    /// </summary>
    public void ShowSettings()
    {
        EnsureMainFormCreated();
        _mainForm!.SelectSection(AppSection.Settings);
        ShowMainWindowInternal();
    }

    /// <summary>Legacy public accessor used by smoke tests.</summary>
    public void ShowMainForm() => ShowOverview();

    /// <summary>
    /// Graceful shutdown requested by the installer's ShutdownForUpdate IPC command.
    /// </summary>
    public void ShutdownForUpdate()
    {
        Logger.Log("IPC", "ShutdownForUpdate received — performing graceful shutdown");
        ExitApplication();
    }

    private void EnsureMainFormCreated()
    {
        if (_mainForm == null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_engine, _notificationService, _autoStartManager, _storage);
            _mainForm.FormClosing += OnMainFormClosing;
        }
    }

    private void ShowMainWindowInternal()
    {
        if (_mainForm == null) return;

        if (_mainForm.WindowState == FormWindowState.Minimized)
        {
            _mainForm.WindowState = FormWindowState.Normal;
        }

        if (!_mainForm.Visible)
        {
            _mainForm.Show();
        }

        _mainForm.BringToFront();
        _mainForm.Activate();

        // Win32 SetForegroundWindow for cases where BringToFront isn't enough
        if (_mainForm.IsHandleCreated)
        {
            NativeMethods.SetForegroundWindow(_mainForm.Handle);
        }
    }

    private void OnMainFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // Hide to tray instead of closing
            e.Cancel = true;
            _mainForm!.Hide();

            // Show one-time tray hint
            if (!_hasShownTrayHint)
            {
                _hasShownTrayHint = true;
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "AJAZZ Battery Monitor",
                    "Приложение продолжает работать в системном трее.",
                    ToolTipIcon.Info);
            }
        }
    }

    private void OnStatusUpdated(BatteryStatus status)
    {
        if (_notifyIcon.ContextMenuStrip?.IsHandleCreated == true)
        {
            if (!_notifyIcon.ContextMenuStrip.InvokeRequired)
            {
                UpdateStatusOnUiThread(status);
            }
            else
            {
                _notifyIcon.ContextMenuStrip.BeginInvoke(() => UpdateStatusOnUiThread(status));
            }
        }
        else
        {
            // Handle not yet created — store for later
            Logger.Log("STATUS_UPDATE", "UI handle not ready; status update deferred.");
        }
    }

    private void UpdateStatusOnUiThread(BatteryStatus status)
    {
        try
        {
            UpdateTrayIconInternal(status);

            string pctStr   = status.Percent.HasValue ? $"{status.Percent}%" : "заряд неизвестен";
            string stateStr = status.IsChargingConfirmed
                ? "заряжается ⚡"
                : (status.IsSleeping ? "в режиме сна" : (status.IsPresent ? "подключена" : "отключена"));
            var localTime = SystemClock.Instance.ToLocal(status.Timestamp);

            // NotifyIcon.Text is limited to 128 chars on Windows
            string tooltip = $"AJAZZ AJ179 APEX\nЗаряд: {pctStr}\nСостояние: {stateStr}\nОбновлено: {localTime:HH:mm:ss}";
            _notifyIcon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;

            string connStr = status.ActiveTransport.Contains("BLE", StringComparison.OrdinalIgnoreCase)
                ? "Bluetooth LE"
                : (status.ActiveTransport.Contains("HID", StringComparison.OrdinalIgnoreCase) ? "2.4 GHz" : "—");

            if (_notifyIcon.ContextMenuStrip?.Items.Count > 1)
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
        Icon? newIcon = null;
        try
        {
            newIcon = TrayIconRenderer.CreateTrayIcon(status);
        }
        catch (Exception ex)
        {
            Logger.Log("TRAY_ICON", $"TrayIconRenderer failed: {ex.Message} — using app icon fallback");
            try { newIcon = new Icon(AppResources.ApplicationIcon, 16, 16); } catch { }
        }

        if (newIcon == null) return;

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
        SystemEvents.SessionSwitch    -= OnSessionSwitch;

        _pollCts.Cancel();
        _smokeTestTimer?.Stop();
        _smokeTestTimer?.Dispose();
        _smokeTestTimer = null;
        if (_mainForm != null)
        {
            _mainForm.FormClosing -= OnMainFormClosing;
            _mainForm.Dispose();
            _mainForm = null;
        }

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

/// <summary>P/Invoke declarations for Win32 foreground window activation.</summary>
internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);
}
