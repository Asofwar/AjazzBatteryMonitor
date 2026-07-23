using System.Runtime.InteropServices;

namespace AjazzBattery.Hid;

internal static class Win32HidNative
{
    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint OPEN_EXISTING = 3;
    public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

    public const int ERROR_SHARING_VIOLATION = 32;

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDD_ATTRIBUTES
    {
        public int Size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HIDP_CAPS
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    [DllImport("hid.dll", SetLastError = true)]
    public static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetAttributes(IntPtr hidDeviceObject, ref HIDD_ATTRIBUTES attributes);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetPreparsedData(IntPtr hidDeviceObject, out IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern int HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS capabilities);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetFeature(IntPtr hidDeviceObject, byte[] reportBuffer, int reportBufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_SetFeature(IntPtr hidDeviceObject, byte[] reportBuffer, int reportBufferLength);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);
}
