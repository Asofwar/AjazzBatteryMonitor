using AjazzBattery.Core;

namespace AjazzBattery.Devices;

public static class YichipBatteryParser
{
    public static BatteryStatus ParseResponse(
        DeviceDescriptor device,
        byte[] rawResponse,
        DateTimeOffset timestamp)
    {
        if (rawResponse == null || rawResponse.Length < 8)
        {
            return BatteryStatus.CreateUnknown("Некорректный размер ответа устройства (менее 4 байт)", ProviderState.InvalidFrame);
        }

        // Check for all-zero frame (telemetry not ready)
        bool allZero = true;
        for (int i = 0; i < Math.Min(rawResponse.Length, 8); i++)
        {
            if (rawResponse[i] != 0) { allZero = false; break; }
        }

        if (allZero)
        {
            return new BatteryStatus(
                IsPresent: true,
                Percent: null,
                IsCharging: null,
                IsFullyCharged: null,
                IsSleeping: false,
                ConnectionMode: device.ConnectionMode,
                Timestamp: timestamp,
                Confidence: StatusConfidence.Low,
                DiagnosticMessage: "2.4 GHz телеметрия ещё не готова (нулевой кадр)",
                State: ProviderState.TelemetryNotReady,
                ActiveTransport: "HID 2.4G",
                RawFrameHex: rawResponse
            );
        }

        // Hardware Frame Validation:
        // frame[0] == 0x05 (Report ID 0x05)
        // frame[1] == 0x00
        // frame[2] == 0x00
        byte reportId = rawResponse[0];
        byte header1 = rawResponse[1];
        byte header2 = rawResponse[2];

        if (reportId != 0x05 || header1 != 0x00 || header2 != 0x00)
        {
            return new BatteryStatus(
                IsPresent: true,
                Percent: null,
                IsCharging: null,
                IsFullyCharged: null,
                IsSleeping: false,
                ConnectionMode: device.ConnectionMode,
                Timestamp: timestamp,
                Confidence: StatusConfidence.Low,
                DiagnosticMessage: $"Отклонен невалидный кадр [0]=0x{reportId:X2} [1]=0x{header1:X2} [2]=0x{header2:X2}",
                State: ProviderState.InvalidFrame,
                ActiveTransport: "HID 2.4G",
                RawFrameHex: rawResponse
            );
        }

        byte rawPercent = rawResponse[3];
        if (rawPercent > 100)
        {
            return new BatteryStatus(
                IsPresent: true,
                Percent: null,
                IsCharging: null,
                IsFullyCharged: null,
                IsSleeping: false,
                ConnectionMode: device.ConnectionMode,
                Timestamp: timestamp,
                Confidence: StatusConfidence.Low,
                DiagnosticMessage: $"Значение батареи вне диапазона 0..100: {rawPercent}",
                State: ProviderState.InvalidFrame,
                ActiveTransport: "HID 2.4G",
                RawFrameHex: rawResponse
            );
        }

        int percent = rawPercent;
        // The status-flag layout has not been validated on an AJ179 APEX in
        // every power state. In particular, byte 4 bit 0 is not proof of
        // charging and a 100% battery level is not proof of external power.
        // Keep both values unknown until a repeated hardware capture defines a
        // protocol-confirmed charging signal.
        bool? isCharging = null;
        bool? isFullyCharged = null;
        bool isSleeping = rawResponse.Length > 7 && (rawResponse[7] & 0x02) != 0;

        string frameHex = BitConverter.ToString(rawResponse, 0, Math.Min(8, rawResponse.Length)).Replace("-", " ");
        string diagMsg = $"RECEIVER OK | VID:PID 0x{device.VendorId:X4}:0x{device.ProductId:X4} | Frame: {frameHex} | Battery: {percent}%";

        return new BatteryStatus(
            IsPresent: true,
            Percent: percent,
            IsCharging: isCharging,
            IsFullyCharged: isFullyCharged,
            IsSleeping: isSleeping,
            ConnectionMode: device.ConnectionMode,
            Timestamp: timestamp,
            Confidence: StatusConfidence.High,
            DiagnosticMessage: diagMsg,
            State: ProviderState.Connected,
            ActiveTransport: $"HID 2.4G (0x{device.VendorId:X4}:0x{device.ProductId:X4})",
            RawFrameHex: rawResponse
        );
    }
}
