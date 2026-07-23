using System.Text;
using System.Windows.Forms;
using AjazzBattery.App.UI.Controls;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Core;
using AjazzBattery.Core.Notifications;
using AjazzBattery.Core.Time;
using AjazzBattery.Storage;

namespace AjazzBattery.App;

public sealed class MainForm : ThemeAwareForm
{
    private readonly BatteryMonitorEngine _engine;
    private readonly BatteryNotificationService _notificationService;
    private readonly IAutoStartManager _autoStartManager;
    private readonly BatteryHistoryStorage _storage;

    // 4-Row Root Hierarchy
    private readonly TableLayoutPanel _tblRoot;
    private readonly TableLayoutPanel _tblHeader;
    private readonly FlowLayoutPanel _flpTitleGroup;
    private readonly FlowLayoutPanel _flpBadgesGroup;
    private readonly FlowLayoutPanel _flpNavigation;
    private readonly Panel _pnlContentHost;
    private readonly TableLayoutPanel _tblFooter;

    // Header Controls
    private readonly Label _lblModelTitle;
    private readonly Label _lblSubTitle;
    private readonly Label _lblBadgeStatus;
    private readonly Label _lblBadgeTransport;

    // Navigation Controls
    private readonly Button _btnTabOverview;
    private readonly Button _btnTabHistory;
    private readonly Button _btnTabSettings;

    // Content Panels
    private readonly OverviewControl _overviewControl;
    private readonly TableLayoutPanel _tblHistory;
    private readonly FlowLayoutPanel _flpHistoryRanges;
    private readonly BatteryHistoryChart _historyChart;
    private readonly Button _btnRange24h;
    private readonly Button _btnRange7d;
    private readonly Button _btnRange30d;

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

    // Footer Controls
    private readonly Label _lblFooterDot;
    private readonly Label _lblFooterStatus;
    private readonly ModernButton _btnRefreshNow;

    public Button TabOverviewButton => _btnTabOverview;
    public Button TabHistoryButton => _btnTabHistory;
    public Button TabSettingsButton => _btnTabSettings;
    public OverviewControl OverviewControlRef => _overviewControl;

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

        Text = $"AJAZZ AJ179 APEX — Монитор батареи (v{AppVersion.Display})";
        Size = new Size(780, 520);
        MinimumSize = new Size(680, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // =============================================================
        // RootLayout — TableLayoutPanel (1 Column, 4 Rows)
        // Row 0: HeaderLayout (68px)
        // Row 1: NavigationLayout (48px)
        // Row 2: ContentHost (100%)
        // Row 3: FooterLayout (44px)
        // =============================================================
        _tblRoot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        _tblRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        _tblRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f)); // HeaderLayout
        _tblRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f)); // NavigationLayout
        _tblRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // ContentHost
        _tblRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f)); // FooterLayout

        // -------------------------------------------------------------
        // Row 0: HeaderLayout
        // -------------------------------------------------------------
        _tblHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20, 10, 20, 10),
            Margin = new Padding(0)
        };
        _tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
        _tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
        _tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _flpTitleGroup = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        _lblModelTitle = new Label
        {
            Text = "AJAZZ AJ179 APEX",
            Font = new Font("Segoe UI Variable Display", 13.5f, FontStyle.Bold),
            AutoSize = true,
            AutoEllipsis = false,
            Margin = new Padding(0, 0, 0, 1)
        };

        _lblSubTitle = new Label
        {
            Text = "Монитор батареи",
            Font = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Regular),
            ForeColor = ThemeManager.Palette.MutedText,
            AutoSize = true,
            AutoEllipsis = false,
            Margin = new Padding(0)
        };

        _flpTitleGroup.Controls.Add(_lblModelTitle);
        _flpTitleGroup.Controls.Add(_lblSubTitle);

        _flpBadgesGroup = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        _lblBadgeTransport = CreatePillBadge("Bluetooth LE", ThemeManager.Palette.Accent);
        _lblBadgeStatus = CreatePillBadge("Подключена", ThemeManager.Palette.Success);

        _flpBadgesGroup.Controls.Add(_lblBadgeTransport);
        _flpBadgesGroup.Controls.Add(_lblBadgeStatus);

        _tblHeader.Controls.Add(_flpTitleGroup, 0, 0);
        _tblHeader.Controls.Add(_flpBadgesGroup, 1, 0);

        _tblRoot.Controls.Add(_tblHeader, 0, 0);

        // -------------------------------------------------------------
        // Row 1: NavigationLayout
        // -------------------------------------------------------------
        _flpNavigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(20, 4, 20, 4),
            Margin = new Padding(0)
        };

        _btnTabOverview = CreateNavigationButton("Обзор");
        _btnTabHistory = CreateNavigationButton("История");
        _btnTabSettings = CreateNavigationButton("Настройки");

        _btnTabOverview.Click += (s, e) => SwitchTab(0);
        _btnTabHistory.Click += (s, e) => SwitchTab(1);
        _btnTabSettings.Click += (s, e) => SwitchTab(2);

        _flpNavigation.Controls.Add(_btnTabOverview);
        _flpNavigation.Controls.Add(_btnTabHistory);
        _flpNavigation.Controls.Add(_btnTabSettings);

        _tblRoot.Controls.Add(_flpNavigation, 0, 1);

        // -------------------------------------------------------------
        // Row 2: ContentHost
        // -------------------------------------------------------------
        _pnlContentHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        // Tab 0: OverviewControl
        _overviewControl = new OverviewControl { Dock = DockStyle.Fill, Visible = true };

        // Tab 1: History Panel
        _tblHistory = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Visible = false,
            Padding = new Padding(20),
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
            Margin = new Padding(0, 0, 0, 10)
        };

        _btnRange24h = new Button { Text = "24 часа", AutoSize = true, Padding = new Padding(10, 4, 10, 4), FlatStyle = FlatStyle.Flat };
        _btnRange7d = new Button { Text = "7 дней", AutoSize = true, Padding = new Padding(10, 4, 10, 4), FlatStyle = FlatStyle.Flat };
        _btnRange30d = new Button { Text = "30 дней", AutoSize = true, Padding = new Padding(10, 4, 10, 4), FlatStyle = FlatStyle.Flat };

        _btnRange24h.Click += (s, e) => SetHistoryRange(TimeSpan.FromHours(24));
        _btnRange7d.Click += (s, e) => SetHistoryRange(TimeSpan.FromDays(7));
        _btnRange30d.Click += (s, e) => SetHistoryRange(TimeSpan.FromDays(30));

        _flpHistoryRanges.Controls.Add(_btnRange24h);
        _flpHistoryRanges.Controls.Add(_btnRange7d);
        _flpHistoryRanges.Controls.Add(_btnRange30d);

        _historyChart = new BatteryHistoryChart { Dock = DockStyle.Fill, Margin = new Padding(0) };

        _tblHistory.Controls.Add(_flpHistoryRanges, 0, 0);
        _tblHistory.Controls.Add(_historyChart, 0, 1);

        // Tab 2: Settings Panel
        _pnlSettings = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false, Padding = new Padding(20) };

        _tblSettingsContent = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(0)
        };
        _tblSettingsContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        _tblSettingsContent.Controls.Add(new Label { Text = "Тема оформления:", AutoSize = true, Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold), Margin = new Padding(0, 4, 0, 4) });
        _cboTheme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        _cboTheme.Items.AddRange(new object[] { "Как в Windows", "Светлая", "Тёмная" });
        _cboTheme.SelectedIndex = (int)ThemeManager.CurrentMode;
        _cboTheme.SelectedIndexChanged += (s, e) => ThemeManager.CurrentMode = (AppThemeMode)_cboTheme.SelectedIndex;
        _tblSettingsContent.Controls.Add(_cboTheme);

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

        EventHandler notificationSettingChanged = (s, e) => SaveNotificationSettings();
        _chkNotificationsEnabled.CheckedChanged += notificationSettingChanged;
        _chkThreshold20.CheckedChanged += notificationSettingChanged;
        _chkThreshold10.CheckedChanged += notificationSettingChanged;
        _chkThreshold5.CheckedChanged += notificationSettingChanged;
        _chkCriticalReminder.CheckedChanged += notificationSettingChanged;
        _chkChargingStarted.CheckedChanged += notificationSettingChanged;
        _chkFullyCharged.CheckedChanged += notificationSettingChanged;
        _cboReminderInterval.SelectedIndexChanged += notificationSettingChanged;

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

        _pnlContentHost.Controls.Add(_overviewControl);
        _pnlContentHost.Controls.Add(_tblHistory);
        _pnlContentHost.Controls.Add(_pnlSettings);

        _tblRoot.Controls.Add(_pnlContentHost, 0, 2);

        // -------------------------------------------------------------
        // Row 3: FooterLayout
        // -------------------------------------------------------------
        _tblFooter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20, 6, 20, 6),
            Margin = new Padding(0)
        };
        _tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var flpFooterLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        _lblFooterDot = new Label
        {
            Text = "●",
            Font = new Font("Segoe UI Variable Text", 9f, FontStyle.Bold),
            ForeColor = ThemeManager.Palette.Success,
            AutoSize = true,
            Margin = new Padding(0, 2, 4, 0)
        };

        _lblFooterStatus = new Label
        {
            Text = "Bluetooth LE · Обновлено только что",
            Font = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Regular),
            ForeColor = ThemeManager.Palette.SecondaryText,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 0)
        };

        flpFooterLeft.Controls.Add(_lblFooterDot);
        flpFooterLeft.Controls.Add(_lblFooterStatus);

        _btnRefreshNow = new ModernButton
        {
            Text = "Обновить",
            Width = 95,
            Height = 28,
            IsPrimary = false,
            Margin = new Padding(0)
        };
        _btnRefreshNow.Click += async (s, e) =>
        {
            var st = await _engine.PollOnceAsync(CancellationToken.None);
            UpdateUi(st);
        };

        _tblFooter.Controls.Add(flpFooterLeft, 0, 0);
        _tblFooter.Controls.Add(_btnRefreshNow, 1, 0);

        _tblRoot.Controls.Add(_tblFooter, 0, 3);

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
        ValidateNavigationInvariants();
    }

    public void ValidateNavigationInvariants()
    {
        var navs = _flpNavigation.Controls.OfType<Button>().Select(b => b.Text).ToList();
        if (navs.Count != 3 || navs[0] != "Обзор" || navs[1] != "История" || navs[2] != "Настройки")
        {
            throw new InvalidOperationException($"Runtime layout invariant failed! Expected nav labels ['Обзор', 'История', 'Настройки'], got [{string.Join(", ", navs)}]");
        }
    }

    private void SaveNotificationSettings()
    {
        var settings = _notificationService.Settings;
        settings.NotificationsEnabled = _chkNotificationsEnabled.Checked;
        settings.Thresholds = new[]
        {
            (_chkThreshold20.Checked, 20),
            (_chkThreshold10.Checked, 10),
            (_chkThreshold5.Checked, 5)
        }
        .Where(x => x.Item1)
        .Select(x => x.Item2)
        .ToList();
        settings.CriticalReminderEnabled = _chkCriticalReminder.Checked;
        settings.CriticalReminderIntervalMinutes = _cboReminderInterval.SelectedIndex switch
        {
            0 => 15,
            2 => 60,
            3 => 120,
            _ => 30
        };
        settings.NotifyChargingStarted = _chkChargingStarted.Checked;
        settings.NotifyFullyCharged = _chkFullyCharged.Checked;
        settings.ValidateAndSanitize();
        _notificationService.Persist();
    }

    public void UpdateUi(BatteryStatus status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateUi(status));
            return;
        }

        _overviewControl.UpdateStatus(status);

        bool isPresent = status.IsPresent && status.Percent.HasValue;

        // Header Badges
        if (isPresent)
        {
            string stStr = status.IsCharging == true ? "Заряжается ⚡" : (status.IsSleeping ? "Мышь спит" : "Подключена");
            _lblBadgeStatus.Text = stStr;
            _lblBadgeStatus.ForeColor = ThemeManager.GetBatteryLevelColor(status.Percent, status.IsCharging == true, status.IsSleeping);

            string trStr = status.ConnectionMode == ConnectionMode.BluetoothLe || status.ActiveTransport.Contains("BLE", StringComparison.OrdinalIgnoreCase) ? "Bluetooth LE" : "2.4 GHz HID";
            _lblBadgeTransport.Text = trStr;
            _lblBadgeTransport.Visible = true;

            _lblFooterDot.ForeColor = ThemeManager.GetBatteryLevelColor(status.Percent, status.IsCharging == true, status.IsSleeping);
            _lblFooterStatus.Text = $"{trStr} · Обновлено {SystemClock.Instance.FormatRelativeTime(status.Timestamp)}";
        }
        else
        {
            _lblBadgeStatus.Text = "Отключена";
            _lblBadgeStatus.ForeColor = ThemeManager.Palette.MutedText;
            _lblBadgeTransport.Visible = false;

            _lblFooterDot.ForeColor = ThemeManager.Palette.MutedText;
            _lblFooterStatus.Text = "Устройство отключено";
        }

        var history = _storage.LoadHistory();
        _historyChart.SetHistoryData(history);
    }

    private Button CreateNavigationButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18, 6, 18, 6),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Bold),
            UseMnemonic = false,
            AutoEllipsis = false,
            Cursor = Cursors.Hand
        };
    }

    private Label CreatePillBadge(string text, Color textColor)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Bold),
            ForeColor = textColor,
            BackColor = Color.FromArgb(30, textColor),
            AutoSize = true,
            AutoEllipsis = false,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(6, 0, 0, 0)
        };
    }

    private void SwitchTab(int tabIndex)
    {
        _overviewControl.Visible = tabIndex == 0;
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

    public string DumpUiTree()
    {
        var sb = new StringBuilder();
        sb.AppendLine("MainForm");
        DumpControlRecursive(this, 1, sb);
        return sb.ToString();
    }

    private static void DumpControlRecursive(Control parent, int indent, StringBuilder sb)
    {
        foreach (Control c in parent.Controls)
        {
            string indentStr = new string(' ', indent * 2);
            sb.AppendLine($"{indentStr}{c.GetType().Name} [Name: '{c.Name}', Text: '{c.Text}', Visible: {c.Visible}, Bounds: {c.Bounds}]");
            if (c.HasChildren)
            {
                DumpControlRecursive(c, indent + 1, sb);
            }
        }
    }

    public override void ApplyTheme()
    {
        base.ApplyTheme();
        var pal = ThemeManager.Palette;

        _tblHeader.BackColor = pal.Surface;
        _tblFooter.BackColor = pal.Surface;
        _lblModelTitle.ForeColor = pal.PrimaryText;

        _btnTabOverview.BackColor = pal.Background;
        _btnTabHistory.BackColor = pal.Background;
        _btnTabSettings.BackColor = pal.Background;

        SwitchTab(_overviewControl.Visible ? 0 : (_tblHistory.Visible ? 1 : 2));

        _btnRange24h.BackColor = pal.SurfaceElevated; _btnRange24h.ForeColor = pal.PrimaryText;
        _btnRange7d.BackColor = pal.SurfaceElevated; _btnRange7d.ForeColor = pal.PrimaryText;
        _btnRange30d.BackColor = pal.SurfaceElevated; _btnRange30d.ForeColor = pal.PrimaryText;
    }
}
