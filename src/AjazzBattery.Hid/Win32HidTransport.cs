using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AjazzBattery.Core;

namespace AjazzBattery.Hid;

public sealed class Win32HidTransport : IHidTransport
{
    private static readonly HashSet<ushort> TargetVendorIds = new() { 0x3151, 0x248A, 0x249A };
    private static readonly HashSet<ushort> KnownProductIds = new() { 0x402D, 0x5007, 0x502D, 0x5008 };

    public Task<IReadOnlyList<DeviceDescriptor>> EnumerateDevicesAsync(CancellationToken cancellationToken)
    {
        return EnumerateAllHidCollectionsAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DeviceDescriptor>> EnumerateAllHidCollectionsAsync(CancellationToken cancellationToken)
    {
        var collections = new List<DeviceDescriptor>();

        Guid hidGuid;
        Win32HidNative.HidD_GetHidGuid(out hidGuid);

        IntPtr hDevInfo = Win32HidNative.SetupDiGetClassDevs(
            ref hidGuid,
            null,
            IntPtr.Zero,
            Win32HidNative.DIGCF_PRESENT | Win32HidNative.DIGCF_DEVICEINTERFACE
        );

        if (hDevInfo == new IntPtr(-1))
        {
            return Task.FromResult<IReadOnlyList<DeviceDescriptor>>(collections);
        }

        try
        {
            var interfaceData = new Win32HidNative.SP_DEVICE_INTERFACE_DATA();
            interfaceData.cbSize = Marshal.SizeOf(interfaceData);

            uint index = 0;
            while (Win32HidNative.SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref hidGuid, index++, ref interfaceData))
            {
                cancellationToken.ThrowIfCancellationRequested();

                int requiredSize = 0;
                Win32HidNative.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);

                var detailData = new Win32HidNative.SP_DEVICE_INTERFACE_DETAIL_DATA();
                detailData.cbSize = IntPtr.Size == 8 ? 8 : 5;

                if (Win32HidNative.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, ref detailData, requiredSize, out requiredSize, IntPtr.Zero))
                {
                    string path = detailData.DevicePath;

                    ushort vid = 0;
                    ushort pid = 0;

                    var vidMatch = Regex.Match(path, @"(?:vid_|vid&)0*([0-9a-f]{4})", RegexOptions.IgnoreCase);
                    var pidMatch = Regex.Match(path, @"(?:pid_|pid&)0*([0-9a-f]{4})", RegexOptions.IgnoreCase);

                    if (vidMatch.Success) vid = Convert.ToUInt16(vidMatch.Groups[1].Value, 16);
                    if (pidMatch.Success) pid = Convert.ToUInt16(pidMatch.Groups[1].Value, 16);

                    IntPtr handle = Win32HidNative.CreateFile(
                        path,
                        0,
                        Win32HidNative.FILE_SHARE_READ | Win32HidNative.FILE_SHARE_WRITE,
                        IntPtr.Zero,
                        Win32HidNative.OPEN_EXISTING,
                        0,
                        IntPtr.Zero
                    );

                    if (handle == IntPtr.Zero || handle == new IntPtr(-1))
                    {
                        handle = Win32HidNative.CreateFile(
                            path,
                            Win32HidNative.GENERIC_READ,
                            Win32HidNative.FILE_SHARE_READ | Win32HidNative.FILE_SHARE_WRITE,
                            IntPtr.Zero,
                            Win32HidNative.OPEN_EXISTING,
                            0,
                            IntPtr.Zero
                        );
                    }

                    ushort usagePage = 0;
                    ushort usage = 0;
                    ushort featureLen = 65;
                    ushort inputLen = 0;
                    ushort outputLen = 0;
                    string productStr = "AJAZZ Mouse Device";
                    string manufStr = "AJAZZ";
                    bool canOpen = false;
                    int win32Err = 0;

                    if (handle != IntPtr.Zero && handle != new IntPtr(-1))
                    {
                        canOpen = true;
                        try
                        {
                            var attr = new Win32HidNative.HIDD_ATTRIBUTES();
                            attr.Size = Marshal.SizeOf(attr);

                            if (Win32HidNative.HidD_GetAttributes(handle, ref attr))
                            {
                                vid = attr.VendorID;
                                pid = attr.ProductID;
                            }

                            IntPtr preparsed;
                            if (Win32HidNative.HidD_GetPreparsedData(handle, out preparsed))
                            {
                                Win32HidNative.HIDP_CAPS caps;
                                Win32HidNative.HidP_GetCaps(preparsed, out caps);
                                Win32HidNative.HidD_FreePreparsedData(preparsed);

                                usagePage = caps.UsagePage;
                                usage = caps.Usage;
                                featureLen = caps.FeatureReportByteLength;
                                inputLen = caps.InputReportByteLength;
                                outputLen = caps.OutputReportByteLength;
                            }

                            productStr = ReadHidString(handle, Win32HidNative.HidD_GetProductString) ?? productStr;
                            manufStr = ReadHidString(handle, Win32HidNative.HidD_GetManufacturerString) ?? manufStr;
                        }
                        finally
                        {
                            Win32HidNative.CloseHandle(handle);
                        }
                    }
                    else
                    {
                        win32Err = Marshal.GetLastWin32Error();
                    }

                    var miMatch = Regex.Match(path, @"mi_([0-9a-f]{2})", RegexOptions.IgnoreCase);
                    int mi = miMatch.Success ? Convert.ToInt32(miMatch.Groups[1].Value, 16) : 0;

                    var connMode = pid switch
                    {
                        0x402D => ConnectionMode.Wireless24G,
                        0x5007 => ConnectionMode.DockStation,
                        0x502D => ConnectionMode.UsbCable,
                        0x5008 => ConnectionMode.Wireless24G,
                        _ => path.Contains("BTH", StringComparison.OrdinalIgnoreCase) ? ConnectionMode.BluetoothLe : ConnectionMode.Unknown
                    };

                    var confirmStatus = pid switch
                    {
                        0x5007 => ConfirmationStatus.ConfirmedOnDevice,
                        0x402D => ConfirmationStatus.HardwareConfirmedUpstream,
                        0x502D => ConfirmationStatus.HardwareConfirmedUpstream,
                        0x5008 => ConfirmationStatus.SourceCodeOnly,
                        _ => ConfirmationStatus.Experimental
                    };

                    collections.Add(new DeviceDescriptor(
                        DevicePath: path,
                        ModelName: (productStr.Contains("179") || path.Contains("179")) ? "AJAZZ AJ179 APEX" : "HID Device",
                        VendorId: vid,
                        ProductId: pid,
                        UsagePage: usagePage,
                        Usage: usage,
                        InterfaceNumber: mi,
                        ConnectionMode: connMode,
                        FeatureReportByteLength: featureLen,
                        InputReportByteLength: inputLen,
                        OutputReportByteLength: outputLen,
                        Manufacturer: manufStr,
                        Product: productStr,
                        CanOpen: canOpen,
                        LastWin32Error: win32Err,
                        ConfirmationStatus: confirmStatus
                    ));
                }
            }
        }
        finally
        {
            Win32HidNative.SetupDiDestroyDeviceInfoList(hDevInfo);
        }

        return Task.FromResult<IReadOnlyList<DeviceDescriptor>>(collections);
    }

    public Task<bool> SetFeatureReportAsync(
        DeviceDescriptor device,
        byte reportId,
        byte[] data,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IntPtr handle = Win32HidNative.CreateFile(
            device.DevicePath,
            Win32HidNative.GENERIC_READ | Win32HidNative.GENERIC_WRITE,
            Win32HidNative.FILE_SHARE_READ | Win32HidNative.FILE_SHARE_WRITE,
            IntPtr.Zero,
            Win32HidNative.OPEN_EXISTING,
            0,
            IntPtr.Zero
        );

        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            int err = Marshal.GetLastWin32Error();
            Logger.Log("HID_SET_ERR", $"CreateFile failed for {device.DevicePath}, Win32 Error: {err}");
            return Task.FromResult(false);
        }

        try
        {
            int reportLen = device.FeatureReportByteLength > 0 ? device.FeatureReportByteLength : 65;
            byte[] buffer = new byte[reportLen];
            buffer[0] = reportId; // Report ID 0x00
            Array.Copy(data, 0, buffer, 1, Math.Min(data.Length, reportLen - 1));

            bool success = Win32HidNative.HidD_SetFeature(handle, buffer, buffer.Length);
            if (!success)
            {
                int err = Marshal.GetLastWin32Error();
                Logger.Log("HID_SET_ERR", $"HidD_SetFeature failed, Win32 Error: {err}");
            }
            else
            {
                Logger.Log("HID_SET_OK", $"HidD_SetFeature 0x{data[0]:X2} succeeded on {device.DevicePath}");
            }

            return Task.FromResult(success);
        }
        finally
        {
            Win32HidNative.CloseHandle(handle);
        }
    }

    public Task<byte[]> GetFeatureReportAsync(
        DeviceDescriptor device,
        byte reportId,
        int expectedLength,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IntPtr handle = Win32HidNative.CreateFile(
            device.DevicePath,
            Win32HidNative.GENERIC_READ | Win32HidNative.GENERIC_WRITE,
            Win32HidNative.FILE_SHARE_READ | Win32HidNative.FILE_SHARE_WRITE,
            IntPtr.Zero,
            Win32HidNative.OPEN_EXISTING,
            0,
            IntPtr.Zero
        );

        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            int err = Marshal.GetLastWin32Error();
            Logger.Log("HID_GET_ERR", $"CreateFile failed for {device.DevicePath}, Win32 Error: {err}");
            throw new InvalidOperationException($"CreateFile Win32 Error {err}");
        }

        try
        {
            int reportLen = device.FeatureReportByteLength > 0 ? device.FeatureReportByteLength : Math.Max(expectedLength, 65);
            byte[] buffer = new byte[reportLen];
            buffer[0] = reportId; // Report ID 0x05

            bool success = Win32HidNative.HidD_GetFeature(handle, buffer, buffer.Length);
            if (!success)
            {
                int err = Marshal.GetLastWin32Error();
                Logger.Log("HID_GET_ERR", $"HidD_GetFeature 0x{reportId:X2} failed, Win32 Error: {err}");
                throw new InvalidOperationException($"HidD_GetFeature Win32 Error {err}");
            }

            Logger.Log("HID_GET_OK", $"HidD_GetFeature 0x{reportId:X2} succeeded. Response: {BitConverter.ToString(buffer, 0, Math.Min(16, buffer.Length))}");
            return Task.FromResult(buffer);
        }
        finally
        {
            Win32HidNative.CloseHandle(handle);
        }
    }

    public Task<byte[]> TransferFeatureReportAsync(
        DeviceDescriptor device,
        byte reportId,
        byte[] requestBuffer,
        int expectedResponseLength,
        CancellationToken cancellationToken)
    {
        return GetFeatureReportAsync(device, reportId, expectedResponseLength, cancellationToken);
    }

    private delegate bool GetStringDelegate(IntPtr handle, byte[] buffer, int bufferLength);

    private static string? ReadHidString(IntPtr handle, GetStringDelegate getter)
    {
        byte[] buffer = new byte[256];
        if (getter(handle, buffer, buffer.Length))
        {
            string str = Encoding.Unicode.GetString(buffer).TrimEnd('\0');
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }
        return null;
    }

    public void Dispose() { }
}
