using System.Windows.Forms;
using AjazzBattery.Core;

namespace AjazzBattery.App;

public sealed class DiagnosticsForm : Form
{
    private readonly TextBox _textBox;

    public DiagnosticsForm(BatteryStatus status, string providerId)
    {
        Text = "AJAZZ Battery Monitor - Диагностика v1.0.1";
        Size = new Size(550, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 46);
        ForeColor = Color.FromArgb(205, 214, 244);

        _textBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Top,
            Height = 310,
            BackColor = Color.FromArgb(24, 24, 37),
            ForeColor = Color.FromArgb(166, 227, 161),
            Font = new Font("Consolas", 9.5f, FontStyle.Regular)
        };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };

        var btnClose = new Button
        {
            Text = "Закрыть",
            DialogResult = DialogResult.OK,
            Width = 100,
            Height = 30,
            BackColor = Color.FromArgb(69, 71, 90),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        var btnCopy = new Button
        {
            Text = "Копировать",
            Width = 110,
            Height = 30,
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnCopy.Click += (s, e) =>
        {
            Clipboard.SetText(_textBox.Text);
            MessageBox.Show("Диагностические данные скопированы в буфер обмена.", "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var btnOpenLogs = new Button
        {
            Text = "Папка логов",
            Width = 120,
            Height = 30,
            BackColor = Color.FromArgb(49, 50, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnOpenLogs.Click += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", Logger.LogDirectoryPath);
            }
            catch { }
        };

        btnPanel.Controls.Add(btnClose);
        btnPanel.Controls.Add(btnCopy);
        btnPanel.Controls.Add(btnOpenLogs);

        Controls.Add(_textBox);
        Controls.Add(btnPanel);

        PopulateDiagnostics(status, providerId);
    }

    private void PopulateDiagnostics(BatteryStatus status, string providerId)
    {
        string text = $@"==================================================
  AJAZZ AJ179 APEX Battery Monitor Diagnostic Info
==================================================
Версия приложения:      1.0.1
Версия .NET Runtime:    {Environment.Version}
Операционная система:   {Environment.OSVersion}
Архитектура процесса:   {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}

--- Состояние Монитора ---
Активный провайдер:     {providerId}
Устройство подключено:  {status.IsPresent}
Процент заряда:         {(status.Percent.HasValue ? $"{status.Percent}%" : "Заряд неизвестен")}
Состояние зарядки:      {status.IsCharging}
Полный заряд:           {status.IsFullyCharged}
Режим сна:              {status.IsSleeping}
Режим подключения:      {status.ConnectionMode}
Достоверность данных:   {status.Confidence}
Время обновления:       {status.Timestamp:yyyy-MM-dd HH:mm:ss UTC}
Сообщение диагностики:  {status.DiagnosticMessage ?? "Опрос выполняется без ошибок."}

--- Пути к файлам ---
Лог запуска:            {Logger.LogFilePath}
Папка хранилища:        {Logger.LogDirectoryPath}
==================================================";

        _textBox.Text = text;
    }
}
