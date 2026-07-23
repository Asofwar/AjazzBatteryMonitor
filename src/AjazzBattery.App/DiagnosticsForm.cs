using System.Text;
using System.Windows.Forms;
using AjazzBattery.Core;
using AjazzBattery.Core.Time;
using AjazzBattery.Hid;
using AjazzBattery.Bluetooth;
using AjazzBattery.Devices;

namespace AjazzBattery.App;

public sealed class DiagnosticsForm : Form
{
    private readonly TextBox _txtDiagnostics;
    private readonly Button _btnCopy;
    private readonly Button _btnHidProbe;
    private readonly Button _btnBleProbe;
    private readonly Button _btnRefresh;
    private readonly Button _btnOpenLog;
    private readonly BatteryMonitorEngine _engine;

    public DiagnosticsForm(BatteryMonitorEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));

        Text = $"AJAZZ AJ179 APEX — Аппаратная диагностика (v{AppVersion.Display})";
        Size = new Size(720, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Icon = SystemIcons.Information;

        _txtDiagnostics = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9.5f),
            Dock = DockStyle.Top,
            Height = 440
        };

        var panelButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            Padding = new Padding(10, 10, 10, 10),
            FlowDirection = FlowDirection.LeftToRight
        };

        _btnCopy = new Button { Text = "Скопировать диагностику", Width = 170, Height = 32 };
        _btnHidProbe = new Button { Text = "Выполнить HID probe", Width = 140, Height = 32 };
        _btnBleProbe = new Button { Text = "Выполнить BLE probe", Width = 140, Height = 32 };
        _btnRefresh = new Button { Text = "Обновить заряд", Width = 110, Height = 32 };
        _btnOpenLog = new Button { Text = "Открыть лог", Width = 100, Height = 32 };

        _btnCopy.Click += (s, e) => CopyDiagnostics();
        _btnHidProbe.Click += async (s, e) => await RunHidProbeAsync();
        _btnBleProbe.Click += async (s, e) => await RunBleProbeAsync();
        _btnRefresh.Click += async (s, e) => await RefreshStatusAsync();
        _btnOpenLog.Click += (s, e) => OpenLogFile();

        panelButtons.Controls.Add(_btnCopy);
        panelButtons.Controls.Add(_btnHidProbe);
        panelButtons.Controls.Add(_btnBleProbe);
        panelButtons.Controls.Add(_btnRefresh);
        panelButtons.Controls.Add(_btnOpenLog);

        Controls.Add(_txtDiagnostics);
        Controls.Add(panelButtons);

        LoadDiagnostics();
    }

    private void LoadDiagnostics()
    {
        var sb = new StringBuilder();
        var status = _engine.CurrentStatus;

        sb.AppendLine("=========================================================");
        sb.AppendLine($"  AJAZZ AJ179 APEX Battery Monitor — Диагностика v{AppVersion.Display}  ");
        sb.AppendLine("=========================================================");
        sb.AppendLine($"ОС:           {Environment.OSVersion}");
        sb.AppendLine($"64-bit OS:    {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"Лог-файл:     {Logger.LogFilePath}");
        sb.AppendLine();

        sb.AppendLine("--- ДИАГНОСТИКА ВРЕМЕНИ И ЧАСОВОГО ПОЯСА ---");
        sb.AppendLine($"Timestamp stored UTC:     {status.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}");
        sb.AppendLine($"Timestamp displayed local: {SystemClock.Instance.ToLocal(status.Timestamp):yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Current UTC:              {SystemClock.Instance.UtcNow:yyyy-MM-dd HH:mm:ssZ}");
        sb.AppendLine($"Current local:            {SystemClock.Instance.LocalNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Local time zone:          {TimeZoneInfo.Local.Id}");
        sb.AppendLine($"UTC offset:               {TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow)}");
        sb.AppendLine();

        sb.AppendLine("--- ТЕКУЩИЙ СТАТУС ТЕЛЕМЕТРИИ ---");
        sb.AppendLine($"Активный транспорт: {status.ActiveTransport}");
        sb.AppendLine($"Состояние:          {status.State}");
        sb.AppendLine($"Процент заряда:     {(status.Percent.HasValue ? $"{status.Percent}%" : "Неизвестен")}");
        sb.AppendLine($"Зарядка:            {(status.IsCharging == true ? "Да" : "Нет")}");
        sb.AppendLine($"Режим сна:          {(status.IsSleeping ? "Да" : "Нет")}");
        sb.AppendLine($"Достоверность:      {status.Confidence}");
        sb.AppendLine($"Диагностика:        {Logger.RedactSensitiveData(status.DiagnosticMessage ?? string.Empty)}");

        if (status.RawFrameHex != null && status.RawFrameHex.Length > 0)
        {
            string hexStr = BitConverter.ToString(status.RawFrameHex, 0, Math.Min(16, status.RawFrameHex.Length)).Replace("-", " ");
            sb.AppendLine($"Сырой кадр 0x05:    {hexStr}");
        }
        sb.AppendLine();

        sb.AppendLine("--- БЛУТУЗ (Bluetooth LE GATT) ---");
        sb.AppendLine("Поддерживаемый Service UUID: 0x180F (Battery Service)");
        sb.AppendLine("Поддерживаемый Char UUID:    0x2A19 (Battery Level)");
        sb.AppendLine("Статус проверки BLE:        (нажмите 'Выполнить BLE probe' для теста)");
        sb.AppendLine();

        sb.AppendLine("--- HID (2.4 GHz Receiver / Charging Dock) ---");
        sb.AppendLine("Поддерживаемые VIDs:  0x3151, 0x248A, 0x249A");
        sb.AppendLine("Поддерживаемые PIDs:  0x5007 (Dock), 0x402D (2.4G Receiver), 0x502D (USB Cable), 0x5008 (Alt 2.4G)");
        sb.AppendLine("Vendor Collection:    UsagePage 0xFFFF / Usage 0x0002");
        sb.AppendLine("Протокол опроса:      SET_FEATURE 0x00 [0xF7] -> Delay 30ms -> GET_FEATURE 0x05");
        sb.AppendLine("Статус проверки HID:        (нажмите 'Выполнить HID probe' для теста)");
        sb.AppendLine("=========================================================");

        _txtDiagnostics.Text = sb.ToString();
    }

    private async Task RunHidProbeAsync()
    {
        _txtDiagnostics.Text = "Выполняется проверка HID 2.4 GHz ресивера и док-станции...\r\n";
        using var transport = new Win32HidTransport();
        var registry = new DeviceProfileRegistry();
        var provider = new AjazzMouseBatteryProvider(transport, registry);

        var collections = await transport.EnumerateAllHidCollectionsAsync(CancellationToken.None);
        var sb = new StringBuilder();
        sb.AppendLine("=========================================================");
        sb.AppendLine("  РЕЗУЛЬТАТЫ HID PROBE (0xF7 + 0x05)                     ");
        sb.AppendLine("=========================================================");
        sb.AppendLine($"Найдено коллекций в PnP: {collections.Count}\r\n");

        if (collections.Count == 0)
        {
            sb.AppendLine("[-] 2.4 GHz ресивер или док-станция не подключены к USB порту.");
            sb.AppendLine("    Проверьте, что 2.4G USB-донгл вставлен в USB порт компьютера.");
        }
        else
        {
            int idx = 1;
            foreach (var col in collections)
            {
                sb.AppendLine($"Коллекция #{idx++}:");
                sb.AppendLine($"  Модель:     {col.ModelName}");
                sb.AppendLine($"  VID:PID:    0x{col.VendorId:X4}:0x{col.ProductId:X4}");
                sb.AppendLine($"  UsagePage:  0x{col.UsagePage:X4}");
                sb.AppendLine($"  Usage:      0x{col.Usage:X4}");
                sb.AppendLine($"  FeatureLen: {col.FeatureReportByteLength}");
                sb.AppendLine($"  DevicePath: {Logger.RedactSensitiveData(col.DevicePath)}");

                if (col.UsagePage == 0x0001 && col.Usage == 0x0002)
                {
                    sb.AppendLine("  Результат:  Пропущена — стандартная коллекция мыши (0x0001/0x0002)\r\n");
                    continue;
                }

                var status = await provider.ReadStatusAsync(col, CancellationToken.None);
                sb.AppendLine($"  Состояние:  {status.State}");
                sb.AppendLine($"  Диагностика:{status.DiagnosticMessage}");
                if (status.Percent.HasValue)
                {
                    sb.AppendLine($"  >>> УСПЕХ! Реальный процент заряда: {status.Percent}% <<<");
                }
                sb.AppendLine();
            }
        }

        _txtDiagnostics.Text = sb.ToString();
    }

    private async Task RunBleProbeAsync()
    {
        _txtDiagnostics.Text = "Выполняется проверка Bluetooth LE GATT...\r\n";
        var bleProvider = new BleBatteryProvider();
        var dummyDesc = new DeviceDescriptor(
            DevicePath: "BLE_DIAG_PROBE",
            ModelName: "AJAZZ AJ179 APEX",
            VendorId: 0x3151,
            ProductId: 0x5007,
            UsagePage: 0xFFFF,
            Usage: 0x0002,
            InterfaceNumber: 0,
            ConnectionMode: ConnectionMode.BluetoothLe
        );

        var status = await bleProvider.ReadStatusAsync(dummyDesc, CancellationToken.None);
        var sb = new StringBuilder();
        sb.AppendLine("=========================================================");
        sb.AppendLine("  РЕЗУЛЬТАТЫ BLE PROBE (GATT 0x180F / 0x2A19)            ");
        sb.AppendLine("=========================================================");
        sb.AppendLine($"Состояние BLE: {status.State}");
        sb.AppendLine($"Диагностика:   {status.DiagnosticMessage}");
        if (status.Percent.HasValue)
        {
            sb.AppendLine($">>> УСПЕХ! Реальный процент заряда по BLE: {status.Percent}% <<<");
        }
        else
        {
            sb.AppendLine("\r\nРекомендация: Если мышь используется по 2.4 GHz ресиверу, убедитесь, что USB-донгл вставлен в ПК.");
        }

        _txtDiagnostics.Text = sb.ToString();
    }

    private async Task RefreshStatusAsync()
    {
        _txtDiagnostics.Text = "Обновление статуса батареи...";
        await _engine.PollOnceAsync(CancellationToken.None);
        LoadDiagnostics();
    }

    private void CopyDiagnostics()
    {
        Clipboard.SetText(_txtDiagnostics.Text);
        MessageBox.Show("Диагностические данные успешно скопированы в буфер обмена.", "Диагностика", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenLogFile()
    {
        string logPath = Logger.LogFilePath;
        if (File.Exists(logPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{logPath}\"");
        }
        else
        {
            MessageBox.Show($"Лог-файл еще не создан:\n{logPath}", "Лог", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
