namespace AjazzBattery.Core;

public sealed class BatteryMonitorEngine
{
    private readonly IEnumerable<IMouseBatteryProvider> _providers;
    private readonly IHidTransport _transport;
    private readonly INotificationService _notifications;
    private readonly Action<BatteryStatus> _onStatusUpdated;

    private BatteryStatus _lastStatus = BatteryStatus.CreateUnknown();
    private DateTimeOffset _lastSuccessfulRead = DateTimeOffset.MinValue;
    private int _consecutiveErrors = 0;
    private bool _notifiedLow20 = false;
    private bool _notifiedLow10 = false;
    private bool _notifiedLow5 = false;
    private bool? _wasCharging = null;

    public BatteryStatus CurrentStatus => _lastStatus;

    public BatteryMonitorEngine(
        IEnumerable<IMouseBatteryProvider> providers,
        IHidTransport transport,
        INotificationService notifications,
        Action<BatteryStatus> onStatusUpdated)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _onStatusUpdated = onStatusUpdated ?? throw new ArgumentNullException(nameof(onStatusUpdated));
    }

    public async Task<BatteryStatus> PollOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var devices = await _transport.EnumerateDevicesAsync(cancellationToken);
            if (devices.Count == 0)
            {
                var unknownStatus = BatteryStatus.CreateUnknown("Устройство AJAZZ не найдено");
                UpdateStatus(unknownStatus);
                return unknownStatus;
            }

            foreach (var device in devices)
            {
                var provider = _providers.FirstOrDefault(p => p.CanHandle(device));
                if (provider == null) continue;

                var status = await provider.ReadStatusAsync(device, cancellationToken);
                if (status.IsPresent && status.Percent.HasValue)
                {
                    _consecutiveErrors = 0;
                    _lastSuccessfulRead = status.Timestamp;
                    ProcessNotifications(status, device.ModelName);
                    UpdateStatus(status);
                    return status;
                }
                else if (status.DiagnosticMessage != null && status.DiagnosticMessage.Contains("заняло"))
                {
                    _notifications.NotifyDeviceConflict(status.DiagnosticMessage);
                    UpdateStatus(status);
                    return status;
                }
            }

            _consecutiveErrors++;
            var isStale = (DateTimeOffset.UtcNow - _lastSuccessfulRead) > TimeSpan.FromMinutes(10);
            var fallbackConfidence = isStale ? StatusConfidence.Stale : StatusConfidence.Low;

            var result = new BatteryStatus(
                IsPresent: _lastStatus.IsPresent,
                Percent: isStale ? null : _lastStatus.Percent,
                IsCharging: _lastStatus.IsCharging,
                IsFullyCharged: _lastStatus.IsFullyCharged,
                IsSleeping: _lastStatus.IsSleeping,
                ConnectionMode: _lastStatus.ConnectionMode,
                Timestamp: DateTimeOffset.UtcNow,
                Confidence: fallbackConfidence,
                DiagnosticMessage: isStale ? "Данные устарели" : "Не удалось обновить статус"
            );

            UpdateStatus(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _consecutiveErrors++;
            var errorStatus = BatteryStatus.CreateUnknown($"Ошибка опроса: {ex.Message}");
            UpdateStatus(errorStatus);
            return errorStatus;
        }
    }

    public TimeSpan GetRecommendedNextPollInterval()
    {
        if (_consecutiveErrors > 0)
        {
            var backoffSeconds = Math.Min(300, Math.Pow(2, Math.Min(_consecutiveErrors, 5)) * 5);
            return TimeSpan.FromSeconds(backoffSeconds);
        }

        if (_lastStatus.IsSleeping)
        {
            return TimeSpan.FromSeconds(60);
        }

        if (_lastStatus.IsCharging == true)
        {
            return TimeSpan.FromSeconds(15);
        }

        return TimeSpan.FromSeconds(30);
    }

    private void UpdateStatus(BatteryStatus status)
    {
        _lastStatus = status;
        _onStatusUpdated(status);
    }

    private void ProcessNotifications(BatteryStatus status, string model)
    {
        if (status.Percent.HasValue)
        {
            int pct = status.Percent.Value;
            if (pct <= 5)
            {
                if (!_notifiedLow5)
                {
                    _notifications.NotifyLowBattery(5, model);
                    _notifiedLow5 = true;
                    _notifiedLow10 = true;
                    _notifiedLow20 = true;
                }
            }
            else if (pct <= 10)
            {
                if (!_notifiedLow10)
                {
                    _notifications.NotifyLowBattery(10, model);
                    _notifiedLow10 = true;
                    _notifiedLow20 = true;
                }
            }
            else if (pct <= 20)
            {
                if (!_notifiedLow20)
                {
                    _notifications.NotifyLowBattery(20, model);
                    _notifiedLow20 = true;
                }
            }
            else
            {
                _notifiedLow5 = false;
                _notifiedLow10 = false;
                _notifiedLow20 = false;
            }
        }

        if (status.IsCharging.HasValue)
        {
            bool isCharging = status.IsCharging.Value;
            if (_wasCharging.HasValue && _wasCharging.Value != isCharging)
            {
                if (isCharging)
                {
                    _notifications.NotifyChargingStarted(model);
                }
                else if (status.IsFullyCharged == true)
                {
                    _notifications.NotifyChargingCompleted(model);
                }
            }
            _wasCharging = isCharging;
        }
    }
}
