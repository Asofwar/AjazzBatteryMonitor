using System.Windows;
using AjazzBattery.Core;
using AjazzBattery.Storage;

namespace AjazzBattery.App;

public partial class MainWindow : Window
{
    private readonly BatteryMonitorEngine _engine;
    private readonly IAutoStartManager _autoStartManager;
    private readonly BatteryHistoryStorage _storage;

    public MainWindow(
        BatteryMonitorEngine engine,
        IAutoStartManager autoStartManager,
        BatteryHistoryStorage storage)
    {
        InitializeComponent();
        _engine = engine;
        _autoStartManager = autoStartManager;
        _storage = storage;

        AutoStartCheckBox.IsChecked = _autoStartManager.IsAutoStartEnabled();
        UpdateUi(_engine.CurrentStatus);
    }

    public void UpdateUi(BatteryStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            if (status.Percent.HasValue)
            {
                PercentText.Text = $"{status.Percent.Value}%";
            }
            else
            {
                PercentText.Text = "Заряд неизвестен";
            }

            StatusBadgeText.Text = status.IsPresent ? "[Подключена]" : "[Отключена]";
            StateText.Text = status.IsCharging == true ? "Заряжается" : (status.IsSleeping ? "Режим сна" : "Активна");
            LastUpdatedText.Text = status.Timestamp.ToString("HH:mm:ss");
            DiagMessageText.Text = status.DiagnosticMessage ?? "Опрос без ошибок.";

            HistoryGrid.ItemsSource = _storage.LoadHistory();
        });
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        var status = await _engine.PollOnceAsync(CancellationToken.None);
        UpdateUi(status);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (AutoStartCheckBox.IsChecked.HasValue)
        {
            _autoStartManager.SetAutoStart(AutoStartCheckBox.IsChecked.Value);
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
