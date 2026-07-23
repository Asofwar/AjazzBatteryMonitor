using System.Text.Json;
using AjazzBattery.Core;
using AjazzBattery.Devices;
using AjazzBattery.Hid;

namespace AjazzBattery.DeviceProbe;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

        using var transport = new Win32HidTransport();
        var registry = new DeviceProfileRegistry();
        var provider = new AjazzMouseBatteryProvider(transport, registry);

        switch (command)
        {
            case "list":
                return await CommandListAsync(transport);

            case "inspect":
                return await CommandInspectAsync(transport, registry);

            case "read-battery":
                return await CommandReadBatteryAsync(transport, provider);

            case "monitor":
                return await CommandMonitorAsync(transport, provider);

            case "capture":
                int durationSec = 60;
                for (int i = 1; i < args.Length - 1; i++)
                {
                    if (args[i] == "--duration" && int.TryParse(args[i + 1], out int d)) durationSec = d;
                }
                return await CommandCaptureAsync(transport, provider, durationSec);

            case "export":
                return await CommandExportAsync(transport, registry);

            default:
                PrintUsage();
                return 0;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("  AjazzBattery.DeviceProbe - CLI Diagnostics ");
        Console.WriteLine("==============================================");
        Console.WriteLine("Команды:");
        Console.WriteLine("  list              - Поиск всех HID устройств AJAZZ");
        Console.WriteLine("  inspect           - Подробный инспект VID/PID, Usage, Report size");
        Console.WriteLine("  read-battery      - Разовое безопасное чтение процента батареи");
        Console.WriteLine("  monitor           - Отслеживание изменений статуса в реальном времени");
        Console.WriteLine("  capture [--duration 60] - Запись дампа пакетов за указанный период (сек)");
        Console.WriteLine("  export            - Экспорт обезличенной диагностики в JSON");
        Console.WriteLine();
    }

    private static async Task<int> CommandListAsync(IHidTransport transport)
    {
        Console.WriteLine("[+] Поиск устройств AJAZZ...");
        var devices = await transport.EnumerateDevicesAsync(CancellationToken.None);
        if (devices.Count == 0)
        {
            Console.WriteLine("[-] Устройства AJAZZ не найдены.");
            return 1;
        }

        Console.WriteLine($"[+] Найдено устройств: {devices.Count}");
        foreach (var dev in devices)
        {
            Console.WriteLine($"  * {dev.ModelName} | VID: 0x{dev.VendorId:X4} PID: 0x{dev.ProductId:X4} | Режим: {dev.ConnectionMode} | Path: {dev.DevicePath}");
        }
        return 0;
    }

    private static async Task<int> CommandInspectAsync(IHidTransport transport, DeviceProfileRegistry registry)
    {
        Console.WriteLine("[+] Подробный анализ HID интерфейсов...");
        var devices = await transport.EnumerateDevicesAsync(CancellationToken.None);
        foreach (var dev in devices)
        {
            var profile = registry.FindMatchingProfile(dev);
            Console.WriteLine($"--- Устройство: {dev.ModelName} ---");
            Console.WriteLine($"  Vendor ID:       0x{dev.VendorId:X4}");
            Console.WriteLine($"  Product ID:      0x{dev.ProductId:X4}");
            Console.WriteLine($"  Usage Page:      0x{dev.UsagePage:X4}");
            Console.WriteLine($"  Usage:           0x{dev.Usage:X4}");
            Console.WriteLine($"  Interface #:     {dev.InterfaceNumber}");
            Console.WriteLine($"  Подтвержден:     {(profile != null && profile.IsConfirmed ? "Да" : "Экспериментальный")}");
            Console.WriteLine($"  Connection Mode: {dev.ConnectionMode}");
            Console.WriteLine();
        }
        return 0;
    }

    private static async Task<int> CommandReadBatteryAsync(IHidTransport transport, IMouseBatteryProvider provider)
    {
        Console.WriteLine("[+] Безопасное чтение батареи...");
        var devices = await transport.EnumerateDevicesAsync(CancellationToken.None);
        if (devices.Count == 0)
        {
            Console.WriteLine("[-] Устройства не найдены.");
            return 1;
        }

        var status = await provider.ReadStatusAsync(devices[0], CancellationToken.None);
        Console.WriteLine($"  Модель:            {devices[0].ModelName}");
        Console.WriteLine($"  Процент батареи:   {(status.Percent.HasValue ? $"{status.Percent}%" : "Заряд неизвестен")}");
        Console.WriteLine($"  Зарядка:           {(status.IsCharging == true ? "Да" : "Нет")}");
        Console.WriteLine($"  Режим сна:         {(status.IsSleeping ? "Да" : "Нет")}");
        Console.WriteLine($"  Достоверность:     {status.Confidence}");
        Console.WriteLine($"  Диагностика:       {status.DiagnosticMessage}");
        return 0;
    }

    private static async Task<int> CommandMonitorAsync(IHidTransport transport, IMouseBatteryProvider provider)
    {
        Console.WriteLine("[+] Запуск монитора в реальном времени. Нажмите Ctrl+C для выхода.");
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        while (!cts.IsCancellationRequested)
        {
            var devices = await transport.EnumerateDevicesAsync(cts.Token);
            if (devices.Count > 0)
            {
                var status = await provider.ReadStatusAsync(devices[0], cts.Token);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Заряд: {(status.Percent.HasValue ? $"{status.Percent}%" : "Неизвестен")} | Зарядка: {status.IsCharging} | Сон: {status.IsSleeping}");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Устройство не подключено");
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
            var devices = await transport.EnumerateDevicesAsync(CancellationToken.None);
            if (devices.Count > 0)
            {
                var status = await provider.ReadStatusAsync(devices[0], CancellationToken.None);
                samples.Add(new { Timestamp = DateTime.UtcNow, status.Percent, status.IsCharging, status.IsSleeping });
            }
            await Task.Delay(2000);
        }

        string dumpJson = JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true });
        string path = "capture-dump.json";
        File.WriteAllText(path, dumpJson);
        Console.WriteLine($"[+] Дамп сохранен в {Path.GetFullPath(path)} (записей: {samples.Count})");
        return 0;
    }

    private static async Task<int> CommandExportAsync(IHidTransport transport, DeviceProfileRegistry registry)
    {
        var devices = await transport.EnumerateDevicesAsync(CancellationToken.None);
        var exportData = new
        {
            ExportedAt = DateTimeOffset.UtcNow,
            DeviceCount = devices.Count,
            Devices = devices.Select(d => new
            {
                d.ModelName,
                VendorId = $"0x{d.VendorId:X4}",
                ProductId = $"0x{d.ProductId:X4}",
                d.UsagePage,
                d.Usage,
                ConnectionMode = d.ConnectionMode.ToString()
            })
        };

        string json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        string exportFile = "ajazz-device-export.json";
        File.WriteAllText(exportFile, json);
        Console.WriteLine($"[+] Обезличенный отчет сохранен в: {Path.GetFullPath(exportFile)}");
        return 0;
    }
}
