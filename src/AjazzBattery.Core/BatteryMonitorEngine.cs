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
            // Step 1: Try BLE provider first if available
            var bleProvider = _providers.FirstOrDefault(p => p.ProviderId == "AjazzBleBatteryProvider");
            if (bleProvider != null)
            {
                var bleDummyDescriptor = new DeviceDescriptor(
                    DevicePath: "BLE_TARGET",
                    ModelName: "AJAZZ AJ179 APEX",
                    VendorId: 0x3151,
                    ProductId: 0x5007,
                    UsagePage: 0xFFFF,
                    Usage: 0x0002,
                    InterfaceNumber: 0,
                    ConnectionMode: ConnectionMode.BluetoothLe
                );

                var bleStatus = await bleProvider.ReadStatusAsync(bleDummyDescriptor, cancellationToken);
                if (bleStatus.IsPresent && bleStatus.Percent.HasValue)
                {
                    _consecutiveErrors = 0;
                    _lastSuccessfulRead = bleStatus.Timestamp;
                    ProcessNotifications(bleStatus, "AJAZZ AJ179 APEX");
                    UpdateStatus(bleStatus);
                    return bleStatus;
                }
            }

            // Step 2: Enumerate HID collections
            var collections = await _transport.EnumerateAllHidCollectionsAsync(cancellationToken);
            if (collections.Count == 0)
            {
                var noDeviceStatus = BatteryStatus.CreateUnknown("Устройство или ресивер AJAZZ не найдены в PnP", ProviderState.DeviceNotFound);
                UpdateStatus(noDeviceStatus);
                return noDeviceStatus;
            }

            var hidProvider = _providers.FirstOrDefault(
                p => p.ProviderId != "AjazzBleBatteryProvider" && collections.Any(p.CanHandle));

            if (hidProvider == null)
            {
                var noProviderStatus = BatteryStatus.CreateUnknown("Подходящий HID провайдер не найден", ProviderState.UnsupportedProtocol);
                UpdateStatus(noProviderStatus);
                return noProviderStatus;
            }

            // Prioritize Vendor-defined Control Collections (UsagePage 0xFFFF / 0xFF00)
            var sortedCollections = collections
                .OrderByDescending(c => c.UsagePage >= 0xFF00 ? 10 : 0)
                .ThenByDescending(c => c.ConfirmationStatus == ConfirmationStatus.ConfirmedOnDevice ? 5 : 0)
                .ToList();

            foreach (var collection in sortedCollections)
            {
                // Skip standard mouse input collection (0x0001 / 0x0002)
                if (collection.UsagePage == 0x0001 && collection.Usage == 0x0002) continue;
                if (!hidProvider.CanHandle(collection)) continue;

                var status = await hidProvider.ReadStatusAsync(collection, cancellationToken);
                if (status.IsPresent && status.Percent.HasValue)
                {
                    _consecutiveErrors = 0;
                    _lastSuccessfulRead = status.Timestamp;
                    ProcessNotifications(status, collection.ModelName);
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

            // Step 3: Handle Fallback if no valid percentage was obtained
            _consecutiveErrors++;
            var isStale = (DateTimeOffset.UtcNow - _lastSuccessfulRead) > TimeSpan.FromMinutes(10);

            var fallbackStatus = new BatteryStatus(
                IsPresent: true,
                Percent: isStale ? null : _lastStatus.Percent,
                IsCharging: _lastStatus.IsCharging,
                IsFullyCharged: _lastStatus.IsFullyCharged,
                IsSleeping: _lastStatus.IsSleeping,
                ConnectionMode: sortedCollections[0].ConnectionMode,
                Timestamp: DateTimeOffset.UtcNow,
                Confidence: isStale ? StatusConfidence.Stale : StatusConfidence.Low,
                DiagnosticMessage: isStale ? "Данные батареи устарели (>10 мин без ответа)" : $"Ресивер найден (0x{sortedCollections[0].VendorId:X4}:0x{sortedCollections[0].ProductId:X4}), но кадр 0x05 не содержит заряд",
                State: ProviderState.TelemetryNotReady,
                ActiveTransport: $"HID (0x{sortedCollections[0].VendorId:X4}:0x{sortedCollections[0].ProductId:X4})"
            );

            UpdateStatus(fallbackStatus);
            return fallbackStatus;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _consecutiveErrors++;
            var errStatus = BatteryStatus.CreateUnknown($"Ошибка опроса: {ex.Message}", ProviderState.Error);
            UpdateStatus(errStatus);
            return errStatus;
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
