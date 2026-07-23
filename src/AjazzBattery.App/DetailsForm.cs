using System.Windows.Forms;
using AjazzBattery.Core;
using AjazzBattery.Storage;

namespace AjazzBattery.App;

public sealed class DetailsForm : Form
{
    private readonly BatteryMonitorEngine _engine;
    private readonly IAutoStartManager _autoStartManager;
    private readonly BatteryHistoryStorage _storage;

    private readonly Label _lblPercent;
    private readonly Label _lblStatus;
    private readonly Label _lblUpdated;
    private readonly Label _lblDiag;
    private readonly CheckBox _chkAutoStart;
    private readonly ListView _historyListView;

    public DetailsForm(
        BatteryMonitorEngine engine,
        IAutoStartManager autoStartManager,
        BatteryHistoryStorage storage)
    {
        _engine = engine;
        _autoStartManager = autoStartManager;
        _storage = storage;

        Text = $"AJAZZ AJ179 APEX — Монитор батареи v{AppVersion.Display}";
        Size = new Size(580, 460);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(30, 30, 46);
        ForeColor = Color.FromArgb(205, 214, 244);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Top,
            Height = 350
        };

        // Tab 1: Overview
        var tabOverview = new TabPage("Обзор") { BackColor = Color.FromArgb(24, 24, 37) };
        var pnlOverview = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(20)
        };

        pnlOverview.Controls.Add(new Label { Text = "AJAZZ AJ179 APEX", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(245, 224, 220), AutoSize = true }, 0, 0);
        pnlOverview.SetColumnSpan(pnlOverview.GetControlFromPosition(0, 0)!, 2);

        pnlOverview.Controls.Add(new Label { Text = "Текущий заряд:", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(186, 194, 222), AutoSize = true }, 0, 1);
        _lblPercent = new Label { Text = "74%", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(166, 227, 161), AutoSize = true };
        pnlOverview.Controls.Add(_lblPercent, 1, 1);

        pnlOverview.Controls.Add(new Label { Text = "Состояние:", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(186, 194, 222), AutoSize = true }, 0, 2);
        _lblStatus = new Label { Text = "Подключена", Font = new Font("Segoe UI", 11), ForeColor = Color.White, AutoSize = true };
        pnlOverview.Controls.Add(_lblStatus, 1, 2);

        pnlOverview.Controls.Add(new Label { Text = "Последнее обновление:", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(186, 194, 222), AutoSize = true }, 0, 3);
        _lblUpdated = new Label { Text = "-", Font = new Font("Segoe UI", 11), ForeColor = Color.White, AutoSize = true };
        pnlOverview.Controls.Add(_lblUpdated, 1, 3);

        pnlOverview.Controls.Add(new Label { Text = "Диагностика:", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(186, 194, 222), AutoSize = true }, 0, 4);
        _lblDiag = new Label { Text = "Опрос выполняется без ошибок.", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(137, 180, 250), AutoSize = true };
        pnlOverview.Controls.Add(_lblDiag, 1, 4);

        tabOverview.Controls.Add(pnlOverview);

        // Tab 2: History
        var tabHistory = new TabPage("История") { BackColor = Color.FromArgb(24, 24, 37) };
        _historyListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            BackColor = Color.FromArgb(30, 30, 46),
            ForeColor = Color.White
        };
        _historyListView.Columns.Add("Время", 160);
        _historyListView.Columns.Add("Заряд", 90);
        _historyListView.Columns.Add("Зарядка", 90);
        _historyListView.Columns.Add("Режим", 120);
        tabHistory.Controls.Add(_historyListView);

        // Tab 3: Settings
        var tabSettings = new TabPage("Настройки") { BackColor = Color.FromArgb(24, 24, 37) };
        var pnlSettings = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(20)
        };
        _chkAutoStart = new CheckBox
        {
            Text = "Запускать вместе с Windows (HKCU Run)",
            AutoSize = true,
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            Checked = _autoStartManager.IsAutoStartEnabled()
        };
        _chkAutoStart.CheckedChanged += (s, e) => _autoStartManager.SetAutoStart(_chkAutoStart.Checked);
        pnlSettings.Controls.Add(_chkAutoStart);
        tabSettings.Controls.Add(pnlSettings);

        tabControl.TabPages.Add(tabOverview);
        tabControl.TabPages.Add(tabHistory);
        tabControl.TabPages.Add(tabSettings);

        // Footer buttons
        var pnlFooter = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };

        var btnClose = new Button
        {
            Text = "Скрыть в трей",
            Width = 130,
            Height = 30,
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnClose.Click += (s, e) => Hide();

        var btnRefresh = new Button
        {
            Text = "Обновить сейчас",
            Width = 140,
            Height = 30,
            BackColor = Color.FromArgb(69, 71, 90),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnRefresh.Click += async (s, e) =>
        {
            var st = await _engine.PollOnceAsync(CancellationToken.None);
            UpdateUi(st);
        };

        pnlFooter.Controls.Add(btnClose);
        pnlFooter.Controls.Add(btnRefresh);

        Controls.Add(tabControl);
        Controls.Add(pnlFooter);

        UpdateUi(_engine.CurrentStatus);
    }

    public void UpdateUi(BatteryStatus status)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateUi(status));
            return;
        }

        _lblPercent.Text = status.Percent.HasValue ? $"{status.Percent}%" : "Заряд неизвестен";
        _lblStatus.Text = status.IsChargingConfirmed ? "Заряжается" : (status.IsSleeping ? "Режим сна" : (status.IsPresent ? "Подключена" : "Отключена"));
        _lblUpdated.Text = status.Timestamp.ToString("HH:mm:ss");
        _lblDiag.Text = status.DiagnosticMessage ?? "Опрос выполняется без ошибок.";

        // Populate history
        _historyListView.Items.Clear();
        var history = _storage.LoadHistory();
        foreach (var entry in history.TakeLast(30).Reverse())
        {
            var item = new ListViewItem(entry.Timestamp.ToString("HH:mm:ss"));
            item.SubItems.Add(entry.Percent.HasValue ? $"{entry.Percent}%" : "?");
            item.SubItems.Add(entry.IsCharging switch { true => "Да", false => "Нет", null => "Нет данных" });
            item.SubItems.Add(entry.ConnectionMode);
            _historyListView.Items.Add(item);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }
}
