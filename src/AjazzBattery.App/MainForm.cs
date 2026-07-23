using System.Windows.Forms;
using AjazzBattery.App.UI.Controls;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;
using AjazzBattery.Storage;

namespace AjazzBattery.App;

public sealed class MainForm : ThemeAwareForm
{
    private readonly BatteryMonitorEngine _engine;
    private readonly BatteryNotificationService _notificationService;
    private readonly IAutoStartManager _autoStartManager;
    private readonly BatteryHistoryStorage _storage;

    // Navigation & Header
    private readonly Panel _pnlHeader;
    private readonly Label _lblModel;
    private readonly Label _lblBadgeConnection;
    private readonly Button _btnTabOverview;
    private readonly Button _btnTabHistory;
    private readonly Button _btnTabSettings;
    private readonly Panel _pnlNavIndicator;

    // Tab Panels
    private readonly Panel _pnlOverview;
    private readonly Panel _pnlHistory;
    private readonly Panel _pnlSettings;

    // Overview Controls
    private readonly BatteryGaugeControl _gaugeControl;
    private readonly StatusCard _cardConnection;
    private readonly StatusCard _cardStatus;
    private readonly StatusCard _cardUpdated;
    private readonly StatusCard _cardBatteryLife;

    // History Controls
    private readonly BatteryHistoryChart _historyChart;
    private readonly Button _btnRange24h;
    private readonly Button _btnRange7d;
    private readonly Button _btnRange30d;

    // Settings Controls
    private readonly ComboBox _cboTheme;
    private readonly CheckBox _chkNotificationsEnabled;
    private readonly CheckBox _chkThreshold20;
    private readonly CheckBox _chkThreshold10;
    private readonly CheckBox _chkThreshold5;
    private readonly CheckBox _chkCriticalReminder;
    private readonly ComboBox _cboReminderInterval;
    private readonly CheckBox _chkChargingStarted;
    private readonly CheckBox _chkFullyCharged;
    private readonly ModernButton _btnTestNotification;
    private readonly CheckBox _chkAutoStart;

    public MainForm(
        BatteryMonitorEngine engine,
        BatteryNotificationService notificationService,
        IAutoStartManager autoStartManager,
        BatteryHistoryStorage storage)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _autoStartManager = autoStartManager ?? throw new ArgumentNullException(nameof(autoStartManager));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));

        Text = "AJAZZ AJ179 APEX — Монитор батареи (v1.1.0)";
        Size = new Size(640, 480);
        MinimumSize = new Size(580, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // 1. Header & Navigation Panel
        _pnlHeader = new Panel { Dock = DockStyle.Top, Height = 65 };
        _lblModel = new Label
        {
            Text = "AJAZZ AJ179 APEX",
            Font = new Font("Segoe UI Variable Display", 14f, FontStyle.Bold),
            Location = new Point(16, 12),
            AutoSize = true
        };

        _lblBadgeConnection = new Label
        {
            Text = "[Подключена] [Bluetooth LE]",
            Font = new Font("Segoe UI Variable Text", 9f, FontStyle.Bold),
            Location = new Point(220, 16),
            AutoSize = true
        };

        _btnTabOverview = CreateNavButton("Обзор", 360);
        _btnTabHistory = CreateNavButton("История", 440);
        _btnTabSettings = CreateNavButton("Настройки", 520);

        _btnTabOverview.Click += (s, e) => SwitchTab(0);
        _btnTabHistory.Click += (s, e) => SwitchTab(1);
        _btnTabSettings.Click += (s, e) => SwitchTab(2);

        _pnlNavIndicator = new Panel { Height = 3, Width = 65, Location = new Point(360, 58) };

        _pnlHeader.Controls.Add(_lblModel);
        _pnlHeader.Controls.Add(_lblBadgeConnection);
        _pnlHeader.Controls.Add(_btnTabOverview);
        _pnlHeader.Controls.Add(_btnTabHistory);
        _pnlHeader.Controls.Add(_btnTabSettings);
        _pnlHeader.Controls.Add(_pnlNavIndicator);

        // 2. Tab 1: Overview Panel
        _pnlOverview = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        _gaugeControl = new BatteryGaugeControl { Location = new Point(16, 16), Size = new Size(220, 220) };

        _cardConnection = new StatusCard { Location = new Point(260, 16), CardTitle = "ПОДКЛЮЧЕНИЕ", CardValue = "Bluetooth LE", CardSubText = "Связь стабильна" };
        _cardStatus = new StatusCard { Location = new Point(430, 16), CardTitle = "СОСТОЯНИЕ", CardValue = "Подключена", CardSubText = "Опрос без ошибок" };
        _cardUpdated = new StatusCard { Location = new Point(260, 120), CardTitle = "ОБНОВЛЕНО", CardValue = "Только что", CardSubText = "Интервал: 30 сек" };
        _cardBatteryLife = new StatusCard { Location = new Point(430, 120), CardTitle = "АВТОНОМНОСТЬ", CardValue = "Примерно 3 дня", CardSubText = "Оценка расхода" };

        _pnlOverview.Controls.Add(_gaugeControl);
        _pnlOverview.Controls.Add(_cardConnection);
        _pnlOverview.Controls.Add(_cardStatus);
        _pnlOverview.Controls.Add(_cardUpdated);
        _pnlOverview.Controls.Add(_cardBatteryLife);

        // 3. Tab 2: History Panel
        _pnlHistory = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), Visible = false };
        _historyChart = new BatteryHistoryChart { Location = new Point(16, 50), Size = new Size(580, 260) };

        _btnRange24h = new Button { Text = "24 часа", Location = new Point(16, 12), Size = new Size(85, 28) };
        _btnRange7d = new Button { Text = "7 дней", Location = new Point(108, 12), Size = new Size(85, 28) };
        _btnRange30d = new Button { Text = "30 дней", Location = new Point(200, 12), Size = new Size(85, 28) };

        _btnRange24h.Click += (s, e) => SetHistoryRange(TimeSpan.FromHours(24));
        _btnRange7d.Click += (s, e) => SetHistoryRange(TimeSpan.FromDays(7));
        _btnRange30d.Click += (s, e) => SetHistoryRange(TimeSpan.FromDays(30));

        _pnlHistory.Controls.Add(_btnRange24h);
        _pnlHistory.Controls.Add(_btnRange7d);
        _pnlHistory.Controls.Add(_btnRange30d);
        _pnlHistory.Controls.Add(_historyChart);

        // 4. Tab 3: Settings Panel
        _pnlSettings = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), Visible = false };

        var pnlSettingsScroll = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        // Theme setting
        pnlSettingsScroll.Controls.Add(new Label { Text = "Тема оформления:", AutoSize = true, Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold) });
        _cboTheme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        _cboTheme.Items.AddRange(new object[] { "Как в Windows", "Светлая", "Тёмная" });
        _cboTheme.SelectedIndex = (int)ThemeManager.CurrentMode;
        _cboTheme.SelectedIndexChanged += (s, e) => ThemeManager.CurrentMode = (AppThemeMode)_cboTheme.SelectedIndex;
        pnlSettingsScroll.Controls.Add(_cboTheme);

        // Notification settings
        pnlSettingsScroll.Controls.Add(new Label { Text = "Настройки уведомлений:", AutoSize = true, Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold), Margin = new Padding(0, 12, 0, 4) });
        _chkNotificationsEnabled = new CheckBox { Text = "Уведомлять о низком заряде", Checked = _notificationService.Settings.NotificationsEnabled, AutoSize = true };
        _chkThreshold20 = new CheckBox { Text = "20% (Низкий заряд)", Checked = _notificationService.Settings.Thresholds.Contains(20), AutoSize = true, Margin = new Padding(16, 0, 0, 0) };
        _chkThreshold10 = new CheckBox { Text = "10% (Очень низкий заряд)", Checked = _notificationService.Settings.Thresholds.Contains(10), AutoSize = true, Margin = new Padding(16, 0, 0, 0) };
        _chkThreshold5 = new CheckBox { Text = "5% (Критический заряд)", Checked = _notificationService.Settings.Thresholds.Contains(5), AutoSize = true, Margin = new Padding(16, 0, 0, 0) };

        _chkCriticalReminder = new CheckBox { Text = "Повторять критическое уведомление (5%)", Checked = _notificationService.Settings.CriticalReminderEnabled, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        _cboReminderInterval = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        _cboReminderInterval.Items.AddRange(new object[] { "15 минут", "30 минут", "60 минут", "120 минут" });
        _cboReminderInterval.SelectedIndex = 1;

        _chkChargingStarted = new CheckBox { Text = "Уведомлять о начале зарядки", Checked = _notificationService.Settings.NotifyChargingStarted, AutoSize = true };
        _chkFullyCharged = new CheckBox { Text = "Уведомлять о завершении зарядки (100%)", Checked = _notificationService.Settings.NotifyFullyCharged, AutoSize = true };

        _btnTestNotification = new ModernButton { Text = "Проверить уведомление", Width = 180, IsPrimary = false };
        _btnTestNotification.Click += async (s, e) => await _notificationService.SendTestNotificationAsync();

        _chkAutoStart = new CheckBox { Text = "Запускать с Windows", Checked = _autoStartManager.IsAutoStartEnabled(), AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        _chkAutoStart.CheckedChanged += (s, e) => _autoStartManager.SetAutoStart(_chkAutoStart.Checked);

        pnlSettingsScroll.Controls.Add(_chkNotificationsEnabled);
        pnlSettingsScroll.Controls.Add(_chkThreshold20);
        pnlSettingsScroll.Controls.Add(_chkThreshold10);
        pnlSettingsScroll.Controls.Add(_chkThreshold5);
        pnlSettingsScroll.Controls.Add(_chkCriticalReminder);
        pnlSettingsScroll.Controls.Add(_cboReminderInterval);
        pnlSettingsScroll.Controls.Add(_chkChargingStarted);
        pnlSettingsScroll.Controls.Add(_chkFullyCharged);
        pnlSettingsScroll.Controls.Add(_btnTestNotification);
        pnlSettingsScroll.Controls.Add(_chkAutoStart);

        _pnlSettings.Controls.Add(pnlSettingsScroll);

        Controls.Add(_pnlOverview);
        Controls.Add(_pnlHistory);
        Controls.Add(_pnlSettings);
        Controls.Add(_pnlHeader);

        FormClosing += (s, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };

        UpdateUi(_engine.CurrentStatus);
    }

    public void UpdateUi(BatteryStatus status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateUi(status));
            return;
        }

        _gaugeControl.Status = status;

        string connType = status.ActiveTransport.Contains("BLE") ? "Bluetooth LE" : (status.ActiveTransport.Contains("HID") ? "2.4 GHz HID" : "Отключено");
        _cardConnection.CardValue = connType;
        _cardConnection.CardSubText = status.IsPresent ? "Связь стабильна" : "Нет связи";

        string stVal = status.IsCharging == true ? "Заряжается ⚡" : (status.IsSleeping ? "Мышь спит" : (status.IsPresent ? "Подключена" : "Отключена"));
        _cardStatus.CardValue = stVal;
        _cardStatus.ValueColor = ThemeManager.GetBatteryLevelColor(status.Percent, status.IsCharging == true, status.IsSleeping);

        _cardUpdated.CardValue = status.Timestamp.ToString("HH:mm:ss");
        _cardUpdated.CardSubText = (DateTimeOffset.UtcNow - status.Timestamp).TotalSeconds < 10 ? "Только что" : $"{Math.Round((DateTimeOffset.UtcNow - status.Timestamp).TotalMinutes)} мин назад";

        _lblBadgeConnection.Text = $"[{stVal}] [{connType}]";
        _lblBadgeConnection.ForeColor = ThemeManager.GetBatteryLevelColor(status.Percent, status.IsCharging == true, status.IsSleeping);

        var history = _storage.LoadHistory();
        _historyChart.SetHistoryData(history);
    }

    private Button CreateNavButton(string text, int x)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, 20),
            Size = new Size(75, 32),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
    }

    private void SwitchTab(int tabIndex)
    {
        _pnlOverview.Visible = tabIndex == 0;
        _pnlHistory.Visible = tabIndex == 1;
        _pnlSettings.Visible = tabIndex == 2;

        int indicatorX = tabIndex switch { 0 => 360, 1 => 440, _ => 520 };
        _pnlNavIndicator.Location = new Point(indicatorX, 58);
    }

    private void SetHistoryRange(TimeSpan range)
    {
        _historyChart.TimeRange = range;
    }

    public override void ApplyTheme()
    {
        base.ApplyTheme();
        var pal = ThemeManager.Palette;

        _pnlHeader.BackColor = pal.Surface;
        _lblModel.ForeColor = pal.PrimaryText;
        _pnlNavIndicator.BackColor = pal.Accent;

        _btnTabOverview.ForeColor = _pnlOverview.Visible ? pal.Accent : pal.SecondaryText;
        _btnTabHistory.ForeColor = _pnlHistory.Visible ? pal.Accent : pal.SecondaryText;
        _btnTabSettings.ForeColor = _pnlSettings.Visible ? pal.Accent : pal.SecondaryText;

        _btnRange24h.BackColor = pal.SurfaceElevated; _btnRange24h.ForeColor = pal.PrimaryText;
        _btnRange7d.BackColor = pal.SurfaceElevated; _btnRange7d.ForeColor = pal.PrimaryText;
        _btnRange30d.BackColor = pal.SurfaceElevated; _btnRange30d.ForeColor = pal.PrimaryText;
    }
}
