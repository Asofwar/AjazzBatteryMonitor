using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using AjazzBattery.Core;

namespace AjazzBattery.Bluetooth;

public sealed class BleBatteryProvider : IMouseBatteryProvider
{
    public string ProviderId => "AjazzBleBatteryProvider";

    public bool CanHandle(DeviceDescriptor device)
    {
        return device.ConnectionMode == ConnectionMode.BluetoothLe || device.DevicePath.Contains("BTH", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<BatteryStatus> ReadStatusAsync(
        DeviceDescriptor device,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.Log("BLE_PROBE", "Searching paired Bluetooth LE devices...");

            string selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
            var devices = await DeviceInformation.FindAllAsync(selector).AsTask(cancellationToken);

            if (devices.Count == 0)
            {
                Logger.Log("BLE_PROBE", "No paired Bluetooth LE devices found in Windows registry");
                return BatteryStatus.CreateUnknown("Сопряженные устройства Bluetooth LE не найдены", ProviderState.DeviceNotFound);
            }

            DeviceInformation? targetDeviceInfo = null;
            foreach (var d in devices)
            {
                if (d.Name.Contains("179", StringComparison.OrdinalIgnoreCase) ||
                    d.Name.Contains("AJAZZ", StringComparison.OrdinalIgnoreCase) ||
                    d.Id.Contains("3151", StringComparison.OrdinalIgnoreCase))
                {
                    targetDeviceInfo = d;
                    break;
                }
            }

            if (targetDeviceInfo == null)
            {
                Logger.Log("BLE_PROBE", $"Found {devices.Count} BLE devices, but none matched AJAZZ/AJ179.");
                return BatteryStatus.CreateUnknown($"Устройство AJAZZ не найдено среди {devices.Count} сопряженных BLE устройств", ProviderState.BluetoothPairedButDisconnected);
            }

            Logger.Log("BLE_PROBE", $"Found matching BLE Device: {targetDeviceInfo.Name} ({targetDeviceInfo.Id})");

            using var bleDevice = await BluetoothLEDevice.FromIdAsync(targetDeviceInfo.Id).AsTask(cancellationToken);
            if (bleDevice == null)
            {
                Logger.Log("BLE_PROBE", "BluetoothLEDevice.FromIdAsync returned null");
                return BatteryStatus.CreateUnknown("Не удалось открыть BluetoothLEDevice", ProviderState.BluetoothPairedButDisconnected);
            }

            if (bleDevice.ConnectionStatus != BluetoothConnectionStatus.Connected)
            {
                Logger.Log("BLE_PROBE", $"BLE Device {bleDevice.Name} is PAIRED but currently DISCONNECTED (ConnectionStatus: {bleDevice.ConnectionStatus})");
                return BatteryStatus.CreateUnknown($"Мышь сопряжена по BLE ({bleDevice.Name}), но отключена (Disconnected)", ProviderState.BluetoothPairedButDisconnected);
            }

            Logger.Log("BLE_PROBE", $"BLE Device {bleDevice.Name} is CONNECTED. Querying GATT Battery Service 0x180F...");

            var gattResult = await bleDevice.GetGattServicesForUuidAsync(GattServiceUuids.Battery).AsTask(cancellationToken);
            if (gattResult.Status != GattCommunicationStatus.Success || gattResult.Services.Count == 0)
            {
                Logger.Log("BLE_PROBE", $"GattServices query status: {gattResult.Status}");
                return BatteryStatus.CreateUnknown("Battery Service (0x180F) недоступен на подключенном BLE устройстве", ProviderState.UnsupportedProtocol);
            }

            var batteryService = gattResult.Services[0];
            var charResult = await batteryService.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel).AsTask(cancellationToken);
            if (charResult.Status != GattCommunicationStatus.Success || charResult.Characteristics.Count == 0)
            {
                Logger.Log("BLE_PROBE", $"BatteryLevel Characteristic query status: {charResult.Status}");
                return BatteryStatus.CreateUnknown("Характеристика BatteryLevel (0x2A19) недоступна", ProviderState.UnsupportedProtocol);
            }

            var batteryChar = charResult.Characteristics[0];
            var readResult = await batteryChar.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken);
            if (readResult.Status != GattCommunicationStatus.Success)
            {
                Logger.Log("BLE_PROBE", $"GATT ReadValueAsync status: {readResult.Status}");
                return BatteryStatus.CreateUnknown($"Ошибка чтения GATT: {readResult.Status}", ProviderState.Timeout);
            }

            if (readResult.Value is null || readResult.Value.Length < 1)
            {
                return BatteryStatus.CreateUnknown("GATT returned an empty BatteryLevel value", ProviderState.InvalidFrame);
            }

            var reader = DataReader.FromBuffer(readResult.Value);
            byte percent = reader.ReadByte();

            if (percent > 100)
            {
                return BatteryStatus.CreateUnknown($"GATT вернул невалидное значение батареи: {percent}", ProviderState.InvalidFrame);
            }

            Logger.Log("BLE_SUCCESS", $"Successfully read battery over Bluetooth LE: {percent}%");

            var timestamp = DateTimeOffset.UtcNow;
            return new BatteryStatus(
                IsPresent: true,
                Percent: percent,
                IsCharging: null,
                IsFullyCharged: null,
                IsSleeping: false,
                ConnectionMode: ConnectionMode.BluetoothLe,
                Timestamp: timestamp,
                Confidence: StatusConfidence.High,
                DiagnosticMessage: $"GATT BLE OK | Battery: {percent}%",
                State: ProviderState.Connected,
                ActiveTransport: "Bluetooth LE GATT (0x180F/0x2A19)",
                BatteryTimestamp: timestamp,
                ConnectionStateTimestamp: timestamp
            );
        }
        catch (Exception ex)
        {
            Logger.Log("BLE_EX", $"Exception during BLE read: {ex.Message}");
            return BatteryStatus.CreateUnknown($"Ошибка BLE: {ex.Message}", ProviderState.Error);
        }
    }
}
