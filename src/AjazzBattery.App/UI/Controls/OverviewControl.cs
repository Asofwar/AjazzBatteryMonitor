using System.Windows.Forms;
using AjazzBattery.App.UI.Theme;
using AjazzBattery.Core;

namespace AjazzBattery.App.UI.Controls;

public sealed class OverviewControl : UserControl
{
    private readonly TableLayoutPanel _overviewLayout;
    private readonly Panel _pnlBatteryHost;
    private readonly BatteryGaugeControl _gaugeControl;
    private readonly TableLayoutPanel _detailsPanel;

    private readonly StatusCard _cardConnection;
    private readonly StatusCard _cardStatus;
    private readonly StatusCard _cardUpdated;
    private readonly StatusCard _cardBatteryLife;

    public BatteryGaugeControl GaugeControl => _gaugeControl;

    public OverviewControl()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        Padding = new Padding(20);

        _overviewLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        ApplyTwoColumnLayout();

        // 1. BatteryPanel (Left Column)
        _pnlBatteryHost = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0)
        };
        _gaugeControl = new BatteryGaugeControl
        {
            Dock = DockStyle.Fill,
            MinimumSize = new Size(220, 220),
            Margin = new Padding(0)
        };
        _pnlBatteryHost.Controls.Add(_gaugeControl);

        // 2. DetailsPanel (Right Column 2x2 Grid)
        _detailsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3, // Row 0 & 1 for cards (AutoSize), Row 2 for extra space (100%)
            Margin = new Padding(10, 0, 0, 0),
            Padding = new Padding(0)
        };

        _detailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _detailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        _detailsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _detailsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _detailsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Push blank space down!

        _cardConnection = new StatusCard { Dock = DockStyle.Fill, Margin = new Padding(6), CardTitle = "ПОДКЛЮЧЕНИЕ" };
        _cardStatus = new StatusCard { Dock = DockStyle.Fill, Margin = new Padding(6), CardTitle = "СОСТОЯНИЕ" };
        _cardUpdated = new StatusCard { Dock = DockStyle.Fill, Margin = new Padding(6), CardTitle = "ОБНОВЛЕНО" };
        _cardBatteryLife = new StatusCard { Dock = DockStyle.Fill, Margin = new Padding(6), CardTitle = "АВТОНОМНОСТЬ" };

        _detailsPanel.Controls.Add(_cardConnection, 0, 0);
        _detailsPanel.Controls.Add(_cardStatus, 1, 0);
        _detailsPanel.Controls.Add(_cardUpdated, 0, 1);
        _detailsPanel.Controls.Add(_cardBatteryLife, 1, 1);

        _overviewLayout.Controls.Add(_pnlBatteryHost, 0, 0);
        _overviewLayout.Controls.Add(_detailsPanel, 1, 0);

        Controls.Add(_overviewLayout);

        SizeChanged += OnSizeChangedInternal;
    }

    private void OnSizeChangedInternal(object? sender, EventArgs e)
    {
        if (Width >= 740)
        {
            if (_overviewLayout.ColumnCount != 2) ApplyTwoColumnLayout();
        }
        else
        {
            if (_overviewLayout.ColumnCount != 1) ApplySingleColumnLayout();
        }
    }

    private void ApplyTwoColumnLayout()
    {
        _overviewLayout.Controls.Clear();
        _overviewLayout.ColumnCount = 2;
        _overviewLayout.RowCount = 1;
        _overviewLayout.ColumnStyles.Clear();
        _overviewLayout.RowStyles.Clear();

        _overviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        _overviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        _overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        if (_pnlBatteryHost != null && _detailsPanel != null)
        {
            _overviewLayout.Controls.Add(_pnlBatteryHost, 0, 0);
            _overviewLayout.Controls.Add(_detailsPanel, 1, 0);
        }
    }

    private void ApplySingleColumnLayout()
    {
        _overviewLayout.Controls.Clear();
        _overviewLayout.ColumnCount = 1;
        _overviewLayout.RowCount = 2;
        _overviewLayout.ColumnStyles.Clear();
        _overviewLayout.RowStyles.Clear();

        _overviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
        _overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));

        if (_pnlBatteryHost != null && _detailsPanel != null)
        {
            _overviewLayout.Controls.Add(_pnlBatteryHost, 0, 0);
            _overviewLayout.Controls.Add(_detailsPanel, 0, 1);
        }
    }

    public void UpdateStatus(BatteryStatus status)
    {
        _gaugeControl.Status = status;

        bool isPresent = status.IsPresent && status.Percent.HasValue;

        // Strict 88% BLE Mapping
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
        _cardConnection.CardSubText = isPresent ? "Связь установлена" : "Нет связи";

        _cardStatus.CardValue = stVal;
        _cardStatus.CardSubText = isPresent ? "Опрос без ошибок" : "Ожидание устройства";
        _cardStatus.ValueColor = ThemeManager.GetBatteryLevelColor(status.Percent, status.IsCharging == true, status.IsSleeping);

        _cardUpdated.CardValue = status.Timestamp.ToString("HH:mm:ss");
        _cardUpdated.CardSubText = (DateTimeOffset.UtcNow - status.Timestamp).TotalSeconds < 10 ? "Только что" : $"{Math.Round((DateTimeOffset.UtcNow - status.Timestamp).TotalMinutes)} мин назад";

        _cardBatteryLife.CardValue = isPresent ? (status.Percent > 50 ? "≈ 3 дня" : "≈ 1 день") : "Недоступно";
        _cardBatteryLife.CardSubText = "Оценочное значение";
    }
}
