using System.Text.Json;
using AjazzBattery.Core;
using AjazzBattery.Devices;
using AjazzBattery.Hid;
using AjazzBattery.Bluetooth;

namespace AjazzBattery.DeviceProbe;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

        using var transport = new Win32HidTransport();
        var registry = new DeviceProfileRegistry();
        var bleProvider = new BleBatteryProvider();
        var hidProvider = new AjazzMouseBatteryProvider(transport, registry);

        switch (command)
        {
            case "list":
                return await CommandListAsync(transport);

            case "inspect":
                return await CommandInspectAsync(transport, registry);

            case "probe-aj179-apex":
                return await CommandProbeAj179ApexAsync(transport, hidProvider);

            case "read-battery":
                return await CommandReadBatteryAsync(transport, bleProvider, hidProvider);

            case "monitor":
                return await CommandMonitorAsync(transport, bleProvider, hidProvider);

            case "capture":
                int durationSec = 60;
                for (int i = 1; i < args.Length - 1; i++)
                {
                    if (args[i] == "--duration" && int.TryParse(args[i + 1], out int d)) durationSec = d;
                }
                return await CommandCaptureAsync(transport, hidProvider, durationSec);

            case "capture-power-states":
                return await CommandCapturePowerStatesAsync(transport, hidProvider, args);

            case "export":
                return await CommandExportAsync(transport, registry);

            default:
                PrintUsage();
                return 0;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("==================================================");
        Console.WriteLine($"  AjazzBattery.DeviceProbe CLI Diagnostics v{AppVersion.Display} ");
        Console.WriteLine("==================================================");
        Console.WriteLine("Команды:");
        Console.WriteLine("  list              - Поиск и перечисление всех HID коллекций");
        Console.WriteLine("  inspect           - Подробный инспект VID/PID, Usage, Report sizes");
        Console.WriteLine("  probe-aj179-apex  - Аппаратная проверка 0xF7 + 0x05 протокола");
        Console.WriteLine("  read-battery      - Разовое чтение процента через BLE/HID");
        Console.WriteLine("  monitor           - Отслеживание изменений статуса в реальном времени");
        Console.WriteLine("  capture [--duration 60] - Запись дампа пакетов за период (сек)");
        Console.WriteLine("  export            - Экспорт обезличенной диагностики в JSON");
        Console.WriteLine();
    }

    private static async Task<int> CommandListAsync(IHidTransport transport)
    {
        Console.WriteLine("[+] Поиск HID коллекций (SetupDi HID Class Guidance)...");
        var collections = await transport.EnumerateAllHidCollectionsAsync(CancellationToken.None);
        if (collections.Count == 0)
        {
            Console.WriteLine("[-] Устройства или коллекции не найдены.");
            return 1;
        }

        Console.WriteLine($"[+] Найдено коллекций: {collections.Count}");
        foreach (var col in collections)
        {
            Console.WriteLine($"  * [{col.ModelName}] VID: 0x{col.VendorId:X4} PID: 0x{col.ProductId:X4} | UsagePage: 0x{col.UsagePage:X4} Usage: 0x{col.Usage:X4} | FeatureLen: {col.FeatureReportByteLength} | Path: {col.DevicePath}");
        }
        return 0;
    }

    private static async Task<int> CommandInspectAsync(IHidTransport transport, DeviceProfileRegistry registry)
    {
        Console.WriteLine("[+] Подробный инспект HID коллекций...");
        var collections = await transport.EnumerateAllHidCollectionsAsync(CancellationToken.None);
        foreach (var col in collections)
        {
            Console.WriteLine($"--- Коллекция: {col.ModelName} ---");
            Console.WriteLine($"  Device Path:             {col.DevicePath}");
            Console.WriteLine($"  Vendor ID:               0x{col.VendorId:X4}");
            Console.WriteLine($"  Product ID:              0x{col.ProductId:X4}");
            Console.WriteLine($"  Manufacturer:            {col.Manufacturer}");
            Console.WriteLine($"  Product:                 {col.Product}");
            Console.WriteLine($"  Interface #:             {col.InterfaceNumber}");
            Console.WriteLine($"  Usage Page:              0x{col.UsagePage:X4}");
            Console.WriteLine($"  Usage:                   0x{col.Usage:X4}");
            Console.WriteLine($"  FeatureReportByteLength: {col.FeatureReportByteLength}");
            Console.WriteLine($"  InputReportByteLength:   {col.InputReportByteLength}");
            Console.WriteLine($"  OutputReportByteLength:  {col.OutputReportByteLength}");
            Console.WriteLine($"  Статус подтверждения:    {col.ConfirmationStatus}");
            Console.WriteLine();
        }
        return 0;
    }

    private static async Task<int> CommandProbeAj179ApexAsync(IHidTransport transport, IMouseBatteryProvider hidProvider)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("  Аппаратный Probe AJ179 APEX (0xF7 + 0x05)      ");
        Console.WriteLine("==================================================");

        var collections = await transport.EnumerateAllHidCollectionsAsync(CancellationToken.None);
        if (collections.Count == 0)
        {
            Console.WriteLine("[-] Устройства AJAZZ не найдены в PnP.");
            return 1;
        }

        int index = 1;
        foreach (var col in collections)
        {
            Console.WriteLine($"\nCollection {index++}:");
            Console.WriteLine($"  Path:      {col.DevicePath}");
            Console.WriteLine($"  VID:PID:   0x{col.VendorId:X4}:0x{col.ProductId:X4}");
            Console.WriteLine($"  UsagePage: 0x{col.UsagePage:X4}");
            Console.WriteLine($"  Usage:     0x{col.Usage:X4}");
            Console.WriteLine($"  FeatureLen:{col.FeatureReportByteLength}");

            if (col.UsagePage == 0x0001 && col.Usage == 0x0002)
            {
                Console.WriteLine("  Result:    Skipped — standard mouse input collection (0x0001/0x0002)");
                continue;
            }

            Console.WriteLine("  [1] Sending SET_FEATURE (Report 0x00, Opcode 0xF7)...");
            bool setOk = await transport.SetFeatureReportAsync(col, 0x00, new byte[] { 0xF7 }, CancellationToken.None);
            Console.WriteLine($"      SET_FEATURE 0xF7 Result: {setOk}");

            if (!setOk)
            {
                Console.WriteLine("  Result:    Failed to send SET_FEATURE 0xF7");
                continue;
            }

            Console.WriteLine("  [2] Waiting 30ms for hardware telemetry response...");
            await Task.Delay(30);

            Console.WriteLine("  [3] Executing GET_FEATURE (Report ID 0x05)...");
            try
            {
                byte[] rawFrame = await transport.GetFeatureReportAsync(col, 0x05, 65, CancellationToken.None);
                string frameHex = BitConverter.ToString(rawFrame, 0, Math.Min(16, rawFrame.Length)).Replace("-", " ");
                Console.WriteLine($"      Raw GET_FEATURE 0x05 Frame: {frameHex}");

                var status = YichipBatteryParser.ParseResponse(col, rawFrame, DateTimeOffset.UtcNow);
                if (status.Percent.HasValue)
                {
                    Console.WriteLine($"  Result:    SUCCESS! Real Battery Percentage: {status.Percent}%");
                    Console.WriteLine($"             Status: {(status.IsCharging == true ? "Charging" : "Discharging")} | Sleeping: {status.IsSleeping}");
                }
                else
                {
                    Console.WriteLine($"  Result:    Frame received but invalid or zero: {status.DiagnosticMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Result:    GET_FEATURE 0x05 Exception: {ex.Message}");
            }
        }

        return 0;
    }

    private static async Task<int> CommandReadBatteryAsync(
        IHidTransport transport,
        IMouseBatteryProvider bleProvider,
        IMouseBatteryProvider hidProvider)
    {
        Console.WriteLine("[+] Разовое чтение батареи...");

        // Try BLE first
        var dummyBleDesc = new DeviceDescriptor(
            DevicePath: "BLE_PROBE",
            ModelName: "AJAZZ AJ179 APEX",
            VendorId: 0x3151,
            ProductId: 0x5007,
            UsagePage: 0xFFFF,
            Usage: 0x0002,
            InterfaceNumber: 0,
            ConnectionMode: ConnectionMode.BluetoothLe
        );

        var bleStatus = await bleProvider.ReadStatusAsync(dummyBleDesc, CancellationToken.None);
        if (bleStatus.IsPresent && bleStatus.Percent.HasValue)
        {
            PrintStatus("Bluetooth LE GATT", bleStatus);
            return 0;
        }

        // Fallback to HID probe
        var collections = await transport.EnumerateAllHidCollectionsAsync(CancellationToken.None);
        foreach (var col in collections)
        {
            if (col.UsagePage == 0x0001 && col.Usage == 0x0002) continue;

            var status = await hidProvider.ReadStatusAsync(col, CancellationToken.None);
            if (status.IsPresent && status.Percent.HasValue)
            {
                PrintStatus($"HID 2.4G (0x{col.VendorId:X4}:0x{col.ProductId:X4})", status);
                return 0;
            }
        }

        Console.WriteLine("[-] Не удалось получить процент заряда ни через BLE, ни через HID.");
        return 1;
    }

    private static void PrintStatus(string transportName, BatteryStatus status)
    {
        Console.WriteLine($"  Активный транспорт: {transportName}");
        Console.WriteLine($"  Процент батареи:    {(status.Percent.HasValue ? $"{status.Percent}%" : "Заряд неизвестен")}");
        Console.WriteLine($"  Зарядка:            {(status.IsCharging == true ? "Да" : "Нет")}");
        Console.WriteLine($"  Режим сна:          {(status.IsSleeping ? "Да" : "Нет")}");
        Console.WriteLine($"  Состояние:          {status.State}");
        Console.WriteLine($"  Диагностика:        {status.DiagnosticMessage}");
    }

    private static async Task<int> CommandMonitorAsync(
        IHidTransport transport,
        IMouseBatteryProvider bleProvider,
        IMouseBatteryProvider hidProvider)
    {
        Console.WriteLine($"[+] Запуск монитора в реальном времени (v{AppVersion.Display}). Нажмите Ctrl+C для выхода.");
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        while (!cts.IsCancellationRequested)
        {
            var collections = await transport.EnumerateAllHidCollectionsAsync(cts.Token);
            bool found = false;

            foreach (var col in collections)
            {
                if (col.UsagePage == 0x0001 && col.Usage == 0x0002) continue;
                var status = await hidProvider.ReadStatusAsync(col, cts.Token);
                if (status.IsPresent && status.Percent.HasValue)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Заряд: {status.Percent}% | Зарядка: {status.IsCharging} | Сон: {status.IsSleeping}");
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Телеметрия не готова / устройство не отвечает");
            }

            try { await Task.Delay(5000, cts.Token); } catch { break; }
        }
        return 0;
    }

    private static async Task<int> CommandCaptureAsync(IHidTransport transport, IMouseBatteryProvider provider, int durationSec)
    {
        Console.WriteLine($"[+] Запись дампа пакетов за {durationSec} секунд...");
        var samples = new List<object>();
        var endAt = DateTime.UtcNow.AddSeconds(durationSec);

        while (DateTime.UtcNow < endAt)
        {
            var collections = await transport.EnumerateAllHidCollectionsAsync(CancellationToken.None);
            foreach (var col in collections)
            {
                if (col.UsagePage == 0x0001 && col.Usage == 0x0002) continue;
                var status = await provider.ReadStatusAsync(col, CancellationToken.None);
                samples.Add(new { Timestamp = DateTime.UtcNow, status.Percent, status.IsCharging, status.IsSleeping, status.RawFrameHex });
            }
            await Task.Delay(2000);
        }

        string dumpJson = JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true });
        string path = "capture-dump.json";
        File.WriteAllText(path, dumpJson);
        Console.WriteLine($"[+] Дамп сохранен в {Path.GetFullPath(path)} (записей: {samples.Count})");
        return 0;
    }

    private static async Task<int> CommandCapturePowerStatesAsync(IHidTransport transport, IMouseBatteryProvider provider, string[] args)
    {
        int stateIndex = Array.IndexOf(args, "--state");
        if (stateIndex < 0 || stateIndex + 1 >= args.Length) return 2;

        string physicalState = args[stateIndex + 1];
        var samples = new List<object>();
        for (int reading = 0; reading < 5; reading++)
        {
            var collection = (await transport.EnumerateAllHidCollectionsAsync(CancellationToken.None)).FirstOrDefault(provider.CanHandle);
            if (collection is null) return 1;
            var status = await provider.ReadStatusAsync(collection, CancellationToken.None);
            samples.Add(new
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                PhysicalState = physicalState,
                Transport = "HID 2.4G",
                FrameLength = status.RawFrameHex?.Length,
                ReportId = status.RawFrameHex?.FirstOrDefault(),
                RawFrame = status.RawFrameHex is null ? null : BitConverter.ToString(status.RawFrameHex),
                status.Percent,
                status.IsCharging,
                status.IsSleeping,
                status.Confidence,
                status.State
            });
            if (reading < 4) await Task.Delay(TimeSpan.FromSeconds(1));
        }
        File.WriteAllText($"power-state-{physicalState}.json", JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static async Task<int> CommandExportAsync(IHidTransport transport, DeviceProfileRegistry registry)
    {
        var collections = await transport.EnumerateAllHidCollectionsAsync(CancellationToken.None);
        var exportData = new
        {
            ExportedAt = DateTimeOffset.UtcNow,
            CollectionCount = collections.Count,
            Collections = collections.Select(c => new
            {
                c.ModelName,
                VendorId = $"0x{c.VendorId:X4}",
                ProductId = $"0x{c.ProductId:X4}",
                UsagePage = $"0x{c.UsagePage:X4}",
                Usage = $"0x{c.Usage:X4}",
                c.FeatureReportByteLength,
                c.InputReportByteLength,
                c.OutputReportByteLength,
                c.ConfirmationStatus
            })
        };

        string json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        string exportFile = "ajazz-device-export.json";
        File.WriteAllText(exportFile, json);
        Console.WriteLine($"[+] Обезличенный отчет сохранен в: {Path.GetFullPath(exportFile)}");
        return 0;
    }
}
