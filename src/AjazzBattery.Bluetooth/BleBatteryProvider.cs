using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using AjazzBattery.Core;

namespace AjazzBattery.Bluetooth;

public sealed class BleBatteryProvider : IMouseBatteryProvider
{
    private static readonly Guid BatteryServiceUuid = GattDeviceService.ConvertShortIdToUuid(0x180F);
    private static readonly Guid BatteryLevelCharUuid = GattCharacteristicUuids.BatteryLevel;

    public string ProviderId => "AjazzBleBatteryProvider";

    public bool CanHandle(DeviceDescriptor device)
    {
        return device.ConnectionMode == ConnectionMode.BluetoothLe ||
               device.ModelName.Contains("AJ179", StringComparison.OrdinalIgnoreCase) ||
               device.ModelName.Contains("AJAZZ", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<BatteryStatus> ReadStatusAsync(
        DeviceDescriptor device,
        CancellationToken cancellationToken)
    {
        try
        {
            string selector = GattDeviceService.GetDeviceSelectorFromUuid(BatteryServiceUuid);
            var devices = await DeviceInformation.FindAllAsync(selector).AsTask(cancellationToken);

            if (devices.Count == 0)
            {
                return BatteryStatus.CreateUnknown("BLE устройство с Battery Service не найдено");
            }

            DeviceInformation? targetDev = devices.FirstOrDefault(d =>
                d.Name.Contains("AJ179", StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains("AJAZZ", StringComparison.OrdinalIgnoreCase)
            ) ?? devices[0];

            using var bleDevice = await BluetoothLEDevice.FromIdAsync(targetDev.Id).AsTask(cancellationToken);
            if (bleDevice == null)
            {
                return BatteryStatus.CreateUnknown("Не удалось открыть BluetoothLEDevice");
            }

            if (bleDevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                return new BatteryStatus(
                    IsPresent: true,
                    Percent: null,
                    IsCharging: false,
                    IsFullyCharged: false,
                    IsSleeping: true,
                    ConnectionMode: ConnectionMode.BluetoothLe,
                    Timestamp: DateTimeOffset.UtcNow,
                    Confidence: StatusConfidence.Low,
                    DiagnosticMessage: "BLE устройство в режиме сна / не подключено"
                );
            }

            var servicesResult = await bleDevice.GetGattServicesForUuidAsync(BatteryServiceUuid, BluetoothCacheMode.Uncached).AsTask(cancellationToken);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            {
                return BatteryStatus.CreateUnknown($"Ошибка чтения GATT Service: {servicesResult.Status}");
            }

            var service = servicesResult.Services[0];
            using (service)
            {
                var charResult = await service.GetCharacteristicsForUuidAsync(BatteryLevelCharUuid, BluetoothCacheMode.Uncached).AsTask(cancellationToken);
                if (charResult.Status != GattCommunicationStatus.Success || charResult.Characteristics.Count == 0)
                {
                    return BatteryStatus.CreateUnknown($"Ошибка чтения GATT Characteristic: {charResult.Status}");
                }

                var characteristic = charResult.Characteristics[0];
                var readResult = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken);

                if (readResult.Status == GattCommunicationStatus.Success && readResult.Value != null)
                {
                    var reader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value);
                    if (reader.UnconsumedBufferLength >= 1)
                    {
                        byte rawPercent = reader.ReadByte();
                        if (rawPercent <= 100)
                        {
                            return new BatteryStatus(
                                IsPresent: true,
                                Percent: rawPercent,
                                IsCharging: false,
                                IsFullyCharged: rawPercent == 100,
                                IsSleeping: false,
                                ConnectionMode: ConnectionMode.BluetoothLe,
                                Timestamp: DateTimeOffset.UtcNow,
                                Confidence: StatusConfidence.High,
                                DiagnosticMessage: "Заряд считан через BLE GATT 0x2A19"
                            );
                        }
                    }
                }
            }

            return BatteryStatus.CreateUnknown("Некорректный ответ BLE GATT характеристики");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return BatteryStatus.CreateUnknown($"Ошибка BLE: {ex.Message}");
        }
    }
}
