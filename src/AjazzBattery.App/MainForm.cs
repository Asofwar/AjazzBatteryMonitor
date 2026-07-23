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

    // Root Layout
    private readonly TableLayoutPanel _tblRoot;
    private readonly TableLayoutPanel _tblHeader;
    private readonly FlowLayoutPanel _flpHeaderTitle;
    private readonly FlowLayoutPanel _flpNavTabs;

    // Header Controls
    private readonly Label _lblModel;
    private readonly Label _lblBadgeConnection;
    private readonly Button _btnTabOverview;
    private readonly Button _btnTabHistory;
    private readonly Button _btnTabSettings;

    // Tab Body Container
    private readonly Panel _pnlBody;

    // Tab 1: Overview Layout
    private readonly TableLayoutPanel _tblOverview;
    private readonly BatteryGaugeControl _gaugeControl;
    private readonly TableLayoutPanel _tblCardsGrid;
    private readonly StatusCard _cardConnection;
    private readonly StatusCard _cardStatus;
    private readonly StatusCard _cardUpdated;
    private readonly StatusCard _cardBatteryLife;

    // Tab 2: History Layout
    private readonly TableLayoutPanel _tblHistory;
    private readonly FlowLayoutPanel _flpHistoryRanges;
    private readonly BatteryHistoryChart _historyChart;
    private readonly Button _btnRange24h;
    private readonly Button _btnRange7d;
    private readonly Button _btnRange30d;

    // Tab 3: Settings Layout
    private readonly Panel _pnlSettings;
    private readonly TableLayoutPanel _tblSettingsContent;
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

        Text = "AJAZZ AJ179 APEX — Монитор батареи (v1.1.1)";
        Size = new Size(660, 500);
        MinimumSize = new Size(580, 440);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // -------------------------------------------------------------
        // 1. Root TableLayoutPanel (Row 0: Header AutoSize, Row 1: Body Fill)
        // -------------------------------------------------------------
        _tblRoot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };
        _tblRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _tblRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _tblRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // -------------------------------------------------------------
        // 2. Header TableLayoutPanel (Col 0: Title & Badges, Col 1: Nav Tabs)
        // -------------------------------------------------------------
        _tblHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 12, 16, 12),
            AutoSize = true
        };
        _tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        _tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        _tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Left Header: Title + Badge
        _flpHeaderTitle = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        _lblModel = new Label
        {
            Text = "AJAZZ AJ179 APEX",
            Font = new Font("Segoe UI Variable Display", 13f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 0)
        };

        _lblBadgeConnection = new Label
        {
            Text = "[Подключена] [Bluetooth LE]",
            Font = new Font("Segoe UI Variable Text", 9f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(10, 6, 0, 0)
        };

        _flpHeaderTitle.Controls.Add(_lblModel);
        _flpHeaderTitle.Controls.Add(_lblBadgeConnection);

        // Right Header: Navigation Tabs
        _flpNavTabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        _btnTabSettings = CreateNavTabButton("Настройки");
        _btnTabHistory = CreateNavTabButton("История");
        _btnTabOverview = CreateNavTabButton("Обзор");

        _btnTabOverview.Click += (s, e) => SwitchTab(0);
        _btnTabHistory.Click += (s, e) => SwitchTab(1);
        _btnTabSettings.Click += (s, e) => SwitchTab(2);

        _flpNavTabs.Controls.Add(_btnTabSettings);
        _flpNavTabs.Controls.Add(_btnTabHistory);
        _flpNavTabs.Controls.Add(_btnTabOverview);

        _tblHeader.Controls.Add(_flpHeaderTitle, 0, 0);
        _tblHeader.Controls.Add(_flpNavTabs, 1, 0);

        _tblRoot.Controls.Add(_tblHeader, 0, 0);

        // -------------------------------------------------------------
        // 3. Body Panel Container (Dock = Fill)
        // -------------------------------------------------------------
        _pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 16) };

        // --- Tab 1: Overview ---
        _tblOverview = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        _tblOverview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        _tblOverview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        _tblOverview.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _gaugeControl = new BatteryGaugeControl { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };

        _tblCardsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0)
        };
        _tblCardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _tblCardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _tblCardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        _tblCardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        _cardConnection = new StatusCard { Dock = DockStyle.Fill, Margin = new Padding(4), CardTitle = "ПОДКЛЮЧЕНИЕ" };
        _cardStatus = new StatusCard { Dock = DockStyle.Fill, Margin = new Padding(4), CardTitle = "СОСТОЯНИЕ" };
        _cardUpdated = new StatusCard { Dock = DockStyle.Fill, Margin = new Padding(4), CardTitle = "ОБНОВЛЕНО" };
        _cardBatteryLife = new StatusCard { Dock = DockStyle.Fill, Margin = new Padding(4), CardTitle = "АВТОНОМНОСТЬ" };

        _tblCardsGrid.Controls.Add(_cardConnection, 0, 0);
        _tblCardsGrid.Controls.Add(_cardStatus, 1, 0);
        _tblCardsGrid.Controls.Add(_cardUpdated, 0, 1);
        _tblCardsGrid.Controls.Add(_cardBatteryLife, 1, 1);

        _tblOverview.Controls.Add(_gaugeControl, 0, 0);
        _tblOverview.Controls.Add(_tblCardsGrid, 1, 0);

        // --- Tab 2: History ---
        _tblHistory = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Visible = false,
            Margin = new Padding(0)
        };
        _tblHistory.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _tblHistory.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _tblHistory.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _flpHistoryRanges = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        _btnRange24h = new Button { Text = "24 часа", AutoSize = true, Padding = new Padding(8, 4, 8, 4), FlatStyle = FlatStyle.Flat };
        _btnRange7d = new Button { Text = "7 дней", AutoSize = true, Padding = new Padding(8, 4, 8, 4), FlatStyle = FlatStyle.Flat };
        _btnRange30d = new Button { Text = "30 дней", AutoSize = true, Padding = new Padding(8, 4, 8, 4), FlatStyle = FlatStyle.Flat };

        _btnRange24h.Click += (s, e) => SetHistoryRange(TimeSpan.FromHours(24));
        _btnRange7d.Click += (s, e) => SetHistoryRange(TimeSpan.FromDays(7));
        _btnRange30d.Click += (s, e) => SetHistoryRange(TimeSpan.FromDays(30));

        _flpHistoryRanges.Controls.Add(_btnRange24h);
        _flpHistoryRanges.Controls.Add(_btnRange7d);
        _flpHistoryRanges.Controls.Add(_btnRange30d);

        _historyChart = new BatteryHistoryChart { Dock = DockStyle.Fill, Margin = new Padding(0) };

        _tblHistory.Controls.Add(_flpHistoryRanges, 0, 0);
        _tblHistory.Controls.Add(_historyChart, 0, 1);

        // --- Tab 3: Settings ---
        _pnlSettings = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false };

        _tblSettingsContent = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(8)
        };
        _tblSettingsContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // Theme
        _tblSettingsContent.Controls.Add(new Label { Text = "Тема оформления:", AutoSize = true, Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold), Margin = new Padding(0, 4, 0, 4) });
        _cboTheme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AutoSize = true, Width = 220 };
        _cboTheme.Items.AddRange(new object[] { "Как в Windows", "Светлая", "Тёмная" });
        _cboTheme.SelectedIndex = (int)ThemeManager.CurrentMode;
        _cboTheme.SelectedIndexChanged += (s, e) => ThemeManager.CurrentMode = (AppThemeMode)_cboTheme.SelectedIndex;
        _tblSettingsContent.Controls.Add(_cboTheme);

        // Notifications
        _tblSettingsContent.Controls.Add(new Label { Text = "Настройки уведомлений:", AutoSize = true, Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold), Margin = new Padding(0, 16, 0, 4) });
        _chkNotificationsEnabled = new CheckBox { Text = "Уведомлять о низком заряде", Checked = _notificationService.Settings.NotificationsEnabled, AutoSize = true };
        _chkThreshold20 = new CheckBox { Text = "20% (Низкий заряд)", Checked = _notificationService.Settings.Thresholds.Contains(20), AutoSize = true, Margin = new Padding(16, 2, 0, 2) };
        _chkThreshold10 = new CheckBox { Text = "10% (Очень низкий заряд)", Checked = _notificationService.Settings.Thresholds.Contains(10), AutoSize = true, Margin = new Padding(16, 2, 0, 2) };
        _chkThreshold5 = new CheckBox { Text = "5% (Критический заряд)", Checked = _notificationService.Settings.Thresholds.Contains(5), AutoSize = true, Margin = new Padding(16, 2, 0, 2) };

        _chkCriticalReminder = new CheckBox { Text = "Повторять критическое уведомление (5%)", Checked = _notificationService.Settings.CriticalReminderEnabled, AutoSize = true, Margin = new Padding(0, 8, 0, 2) };
        _cboReminderInterval = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        _cboReminderInterval.Items.AddRange(new object[] { "15 минут", "30 минут", "60 минут", "120 минут" });
        _cboReminderInterval.SelectedIndex = 1;

        _chkChargingStarted = new CheckBox { Text = "Уведомлять о начале зарядки", Checked = _notificationService.Settings.NotifyChargingStarted, AutoSize = true, Margin = new Padding(0, 4, 0, 2) };
        _chkFullyCharged = new CheckBox { Text = "Уведомлять о завершении зарядки (100%)", Checked = _notificationService.Settings.NotifyFullyCharged, AutoSize = true, Margin = new Padding(0, 2, 0, 4) };

        _btnTestNotification = new ModernButton { Text = "Проверить уведомление", Width = 180, IsPrimary = false, Margin = new Padding(0, 8, 0, 8) };
        _btnTestNotification.Click += async (s, e) => await _notificationService.SendTestNotificationAsync();

        _chkAutoStart = new CheckBox { Text = "Запускать с Windows", Checked = _autoStartManager.IsAutoStartEnabled(), AutoSize = true, Margin = new Padding(0, 12, 0, 4) };
        _chkAutoStart.CheckedChanged += (s, e) => _autoStartManager.SetAutoStart(_chkAutoStart.Checked);

        _tblSettingsContent.Controls.Add(_chkNotificationsEnabled);
        _tblSettingsContent.Controls.Add(_chkThreshold20);
        _tblSettingsContent.Controls.Add(_chkThreshold10);
        _tblSettingsContent.Controls.Add(_chkThreshold5);
        _tblSettingsContent.Controls.Add(_chkCriticalReminder);
        _tblSettingsContent.Controls.Add(_cboReminderInterval);
        _tblSettingsContent.Controls.Add(_chkChargingStarted);
        _tblSettingsContent.Controls.Add(_chkFullyCharged);
        _tblSettingsContent.Controls.Add(_btnTestNotification);
        _tblSettingsContent.Controls.Add(_chkAutoStart);

        _pnlSettings.Controls.Add(_tblSettingsContent);

        _pnlBody.Controls.Add(_tblOverview);
        _pnlBody.Controls.Add(_tblHistory);
        _pnlBody.Controls.Add(_pnlSettings);

        _tblRoot.Controls.Add(_pnlBody, 0, 1);
        Controls.Add(_tblRoot);

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

        bool isPresent = status.IsPresent && status.Percent.HasValue;

        // Consistent Transport String Mapping
        string connType;
        if (!isPresent)
        {
            connType = "Отключено";
        }
        else if (status.ConnectionMode == ConnectionMode.BluetoothLe || status.ActiveTransport.Contains("BLE", StringComparison.OrdinalIgnoreCase))
        {
            connType = "Bluetooth LE";
        }
        else if (status.ConnectionMode == ConnectionMode.Wireless24G || status.ConnectionMode == ConnectionMode.DockStation || status.ActiveTransport.Contains("HID", StringComparison.OrdinalIgnoreCase))
        {
            connType = "2.4 GHz HID";
        }
        else if (status.ConnectionMode == ConnectionMode.UsbCable)
        {
            connType = "USB Кабель";
        }
        else
        {
            connType = "Подключена";
        }

        // Consistent Device State Mapping
        string stVal;
        if (!isPresent)
        {
            stVal = "Отключена";
        }
        else if (status.IsCharging == true)
        {
            stVal = "Заряжается ⚡";
        }
        else if (status.IsSleeping)
        {
            stVal = "Мышь спит";
        }
        else
        {
            stVal = "Подключена";
        }

        _cardConnection.CardValue = connType;
        _cardConnection.CardSubText = isPresent ? "Связь стабильна" : "Нет связи";

        _cardStatus.CardValue = stVal;
        _cardStatus.CardSubText = isPresent ? "Опрос без ошибок" : "Ожидание устройства";
        _cardStatus.ValueColor = ThemeManager.GetBatteryLevelColor(status.Percent, status.IsCharging == true, status.IsSleeping);

        _cardUpdated.CardValue = status.Timestamp.ToString("HH:mm:ss");
        _cardUpdated.CardSubText = (DateTimeOffset.UtcNow - status.Timestamp).TotalSeconds < 10 ? "Только что" : $"{Math.Round((DateTimeOffset.UtcNow - status.Timestamp).TotalMinutes)} мин назад";

        _cardBatteryLife.CardValue = isPresent ? (status.Percent > 50 ? "Примерно 3 дня" : "Примерно 1 день") : "Недоступно";
        _cardBatteryLife.CardSubText = "Оценка расхода";

        // Consistent Header Badge Text
        _lblBadgeConnection.Text = isPresent ? $"[{stVal}] [{connType}]" : "[Отключена] [Нет связи]";
        _lblBadgeConnection.ForeColor = isPresent
            ? ThemeManager.GetBatteryLevelColor(status.Percent, status.IsCharging == true, status.IsSleeping)
            : ThemeManager.Palette.MutedText;

        var history = _storage.LoadHistory();
        _historyChart.SetHistoryData(history);
    }

    private Button CreateNavTabButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(4, 0, 4, 0),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
    }

    private void SwitchTab(int tabIndex)
    {
        _tblOverview.Visible = tabIndex == 0;
        _tblHistory.Visible = tabIndex == 1;
        _pnlSettings.Visible = tabIndex == 2;

        var pal = ThemeManager.Palette;
        _btnTabOverview.ForeColor = tabIndex == 0 ? pal.Accent : pal.SecondaryText;
        _btnTabHistory.ForeColor = tabIndex == 1 ? pal.Accent : pal.SecondaryText;
        _btnTabSettings.ForeColor = tabIndex == 2 ? pal.Accent : pal.SecondaryText;
    }

    private void SetHistoryRange(TimeSpan range)
    {
        _historyChart.TimeRange = range;
    }

    public override void ApplyTheme()
    {
        base.ApplyTheme();
        var pal = ThemeManager.Palette;

        _tblHeader.BackColor = pal.Surface;
        _lblModel.ForeColor = pal.PrimaryText;

        _btnTabOverview.BackColor = pal.Surface;
        _btnTabHistory.BackColor = pal.Surface;
        _btnTabSettings.BackColor = pal.Surface;

        SwitchTab(_tblOverview.Visible ? 0 : (_tblHistory.Visible ? 1 : 2));

        _btnRange24h.BackColor = pal.SurfaceElevated; _btnRange24h.ForeColor = pal.PrimaryText;
        _btnRange7d.BackColor = pal.SurfaceElevated; _btnRange7d.ForeColor = pal.PrimaryText;
        _btnRange30d.BackColor = pal.SurfaceElevated; _btnRange30d.ForeColor = pal.PrimaryText;
    }
}
