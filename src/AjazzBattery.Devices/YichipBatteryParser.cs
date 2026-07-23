using AjazzBattery.Core;

namespace AjazzBattery.Devices;

public static class YichipBatteryParser
{
    public static BatteryStatus ParseResponse(
        DeviceDescriptor device,
        byte[] rawResponse,
        DateTimeOffset timestamp)
    {
        if (rawResponse == null || rawResponse.Length < 5)
        {
            return BatteryStatus.CreateUnknown("Некорректный размер ответа устройства");
        }

        // Opcode validation: expecting 0x20 or 0xF7 in response[1]
        byte opcode = rawResponse[1];
        if (opcode != 0x20 && opcode != 0xF7)
        {
            return BatteryStatus.CreateUnknown($"Неизвестный report opcode: 0x{opcode:X2}");
        }

        byte statusByte = rawResponse[3];
        byte rawPercent = rawResponse[4];

        bool isSleeping = (statusByte & 0x02) != 0;
        bool isCharging = (statusByte & 0x01) != 0;
        bool isFullyCharged = rawPercent == 0xFF;

        int? percent = null;
        StatusConfidence confidence = StatusConfidence.High;
        string? diagMsg = null;

        if (isFullyCharged)
        {
            percent = 100;
            isCharging = true;
            diagMsg = "Зарядка завершена / 100%";
        }
        else if (rawPercent <= 100)
        {
            percent = rawPercent;
            diagMsg = isCharging ? "Заряжается" : (isSleeping ? "Мышь в режиме сна" : "Активна");
        }
        else
        {
            confidence = StatusConfidence.Low;
            diagMsg = $"Значение батареи вне диапазонов: {rawPercent}";
        }

        return new BatteryStatus(
            IsPresent: true,
            Percent: percent,
            IsCharging: isCharging,
            IsFullyCharged: isFullyCharged,
            IsSleeping: isSleeping,
            ConnectionMode: device.ConnectionMode,
            Timestamp: timestamp,
            Confidence: confidence,
            DiagnosticMessage: diagMsg
        );
    }
}
