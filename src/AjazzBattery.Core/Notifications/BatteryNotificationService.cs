namespace AjazzBattery.Core.Notifications;

public sealed class BatteryNotificationService
{
    private readonly INotificationTransport _primaryTransport;
    private readonly INotificationTransport _fallbackTransport;
    private readonly BatteryNotificationSettings _settings;
    private readonly BatteryNotificationState _state;
    private readonly Action<BatteryNotificationSettings, BatteryNotificationState>? _persist;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public BatteryNotificationSettings Settings => _settings;
    public BatteryNotificationState State => _state;
    public string ActiveTransportName => _primaryTransport.TransportName;

    public BatteryNotificationService(
        INotificationTransport primaryTransport,
        INotificationTransport fallbackTransport,
        BatteryNotificationSettings? settings = null,
        BatteryNotificationState? state = null,
        Action<BatteryNotificationSettings, BatteryNotificationState>? persist = null)
    {
        _primaryTransport = primaryTransport ?? throw new ArgumentNullException(nameof(primaryTransport));
        _fallbackTransport = fallbackTransport ?? throw new ArgumentNullException(nameof(fallbackTransport));
        _settings = settings ?? new BatteryNotificationSettings();
        _state = state ?? new BatteryNotificationState();
        _persist = persist;

        _settings.ValidateAndSanitize();
    }

    public async Task ProcessBatteryUpdateAsync(
        BatteryStatus status,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await ProcessBatteryUpdateCoreAsync(status, cancellationToken);
        }
        finally
        {
            Persist();
            _gate.Release();
        }
    }

    public void Persist() => _persist?.Invoke(_settings, _state);

    private async Task ProcessBatteryUpdateCoreAsync(
        BatteryStatus status,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.NotificationsEnabled) return;
        if (!status.IsPresent || !status.Percent.HasValue || status.IsSleeping) return;
        if (status.Confidence == StatusConfidence.Stale || status.State == ProviderState.InvalidFrame) return;

        int currentPercent = status.Percent.Value;
        bool isCharging = status.IsCharging == true;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // 1. Handle Charging Notifications
        if (isCharging)
        {
            if (_state.WasCharging != true && _settings.NotifyChargingStarted)
            {
                await SendNotificationAsync(
                    "AJAZZ AJ179 APEX",
                    $"Зарядка началась. Текущий уровень: {currentPercent}%.",
                    "charging_started",
                    cancellationToken
                );
            }
            else if (currentPercent == 100 && _state.LastPercent < 100 && _settings.NotifyFullyCharged)
            {
                await SendNotificationAsync(
                    "AJAZZ AJ179 APEX заряжена",
                    "Заряд достиг 100%.",
                    "fully_charged",
                    cancellationToken
                );
            }

            _state.WasCharging = true;
            _state.LastPercent = currentPercent;
            return; // Skip low battery notifications while charging
        }
        else
        {
            _state.WasCharging = false;
        }

        // 2. Anomaly Filter (>20% drop in a single poll)
        if (_state.LastPercent.HasValue && (_state.LastPercent.Value - currentPercent) > 20)
        {
            if (_state.PendingAnomalyPercent != currentPercent)
            {
                Logger.Log("NOTIFY_ANOMALY", $"Single-poll drop from {_state.LastPercent}% to {currentPercent}%. Awaiting confirmation read...");
                _state.PendingAnomalyPercent = currentPercent;
                return; // Skip this cycle
            }
            else
            {
                Logger.Log("NOTIFY_ANOMALY", $"Confirmed rapid discharge to {currentPercent}%. Proceeding with notification.");
                _state.PendingAnomalyPercent = null;
            }
        }
        else
        {
            _state.PendingAnomalyPercent = null;
        }

        // 3. Re-arm Thresholds via Hysteresis
        foreach (int t in _settings.Thresholds)
        {
            if (_state.TriggeredThresholds.Contains(t) && currentPercent >= (t + _settings.HysteresisMargin))
            {
                _state.TriggeredThresholds.Remove(t);
                Logger.Log("NOTIFY_REARM", $"Threshold {t}% re-armed at {currentPercent}%");
            }
        }

        // 4. Threshold Crossing & First Launch Handling
        if (!_state.InitialReadingProcessed)
        {
            _state.InitialReadingProcessed = true;
            // Find lowest crossed threshold at first launch (e.g. at 4%, select 5% threshold)
            int? lowestCrossed = _settings.Thresholds.Where(t => currentPercent <= t).OrderBy(t => t).Cast<int?>().FirstOrDefault();
            if (lowestCrossed.HasValue && !_state.TriggeredThresholds.Contains(lowestCrossed.Value))
            {
                await TriggerThresholdNotificationAsync(lowestCrossed.Value, currentPercent, cancellationToken);
                _state.TriggeredThresholds.Add(lowestCrossed.Value);
            }
        }
        else if (_state.LastPercent.HasValue)
        {
            int prevPercent = _state.LastPercent.Value;
            var newlyCrossed = _settings.Thresholds
                .Where(t => prevPercent > t && currentPercent <= t && !_state.TriggeredThresholds.Contains(t))
                .OrderBy(t => t) // Most critical (lowest) first
                .ToList();

            if (newlyCrossed.Count > 0)
            {
                int targetThreshold = newlyCrossed.First();
                await TriggerThresholdNotificationAsync(targetThreshold, currentPercent, cancellationToken);
                _state.TriggeredThresholds.Add(targetThreshold);
            }
        }

        // 5. Repeat Critical Reminder (5% and below)
        if (currentPercent <= 5 && _settings.CriticalReminderEnabled)
        {
            if (!_state.LastCriticalReminderTime.HasValue || (now - _state.LastCriticalReminderTime.Value) >= TimeSpan.FromMinutes(_settings.CriticalReminderIntervalMinutes))
            {
                if (_state.LastPercent.HasValue && _state.LastPercent.Value <= 5) // Skip if this was the initial threshold trigger
                {
                    await SendNotificationAsync(
                        "AJAZZ AJ179 APEX — критический заряд",
                        $"Осталось {currentPercent}%. Подключите мышь к зарядке.",
                        "critical_reminder",
                        cancellationToken
                    );
                    _state.LastCriticalReminderTime = now;
                }
            }
        }

        _state.LastPercent = currentPercent;
    }

    public async Task SendTestNotificationAsync(CancellationToken cancellationToken = default)
    {
        await SendNotificationAsync(
            "Тест уведомлений AJAZZ Battery Monitor",
            "Уведомления работают корректно. Это тестовое сообщение.",
            "test_notification",
            cancellationToken
        );
    }

    private async Task TriggerThresholdNotificationAsync(int threshold, int currentPercent, CancellationToken cancellationToken)
    {
        string title;
        string message;

        if (threshold <= 5)
        {
            title = "AJAZZ AJ179 APEX — критический заряд";
            message = $"Осталось {currentPercent}%. Подключите мышь к зарядке.";
        }
        else if (threshold <= 10)
        {
            title = "AJAZZ AJ179 APEX — очень низкий заряд";
            message = $"Осталось {currentPercent}%. Рекомендуется подключить мышь к зарядке.";
        }
        else
        {
            title = "AJAZZ AJ179 APEX — низкий заряд";
            message = $"Осталось {currentPercent}%. Скоро потребуется зарядка.";
        }

        await SendNotificationAsync(title, message, $"threshold_{threshold}", cancellationToken);
    }

    private async Task SendNotificationAsync(
        string title,
        string message,
        string category,
        CancellationToken cancellationToken)
    {
        bool success = false;
        try
        {
            success = await _primaryTransport.ShowNotificationAsync(title, message, category, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogException("NOTIFY_PRIMARY_ERR", ex);
        }

        if (!success)
        {
            Logger.Log("NOTIFY_FALLBACK", $"Primary transport failed. Falling back to {_fallbackTransport.TransportName}");
            try
            {
                await _fallbackTransport.ShowNotificationAsync(title, message, category, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogException("NOTIFY_FALLBACK_ERR", ex);
            }
        }

        _state.LastNotificationTime = DateTimeOffset.UtcNow;
    }
}
