# Runtime Failure Analysis Report

## Reproduction Results
- **Executable**: `artifacts/AjazzBatteryMonitor-win-x64.exe`
- **Execution Command**:
  ```powershell
  $p = Start-Process -FilePath ".\artifacts\AjazzBatteryMonitor-win-x64.exe" -PassThru
  Start-Sleep -Seconds 5
  $p.Refresh()
  [PSCustomObject]@{ HasExited = $p.HasExited; ExitCode = $p.ExitCode; ProcessId = $p.Id }
  ```
- **Observed Behavior**:
  - `HasExited`: `True`
  - `ExitCode`: `-1073740771` (0xC0000374 / 0xE0434352 - Unhandled .NET Exception Crash)
  - System Tray Icon: Absent (Never rendered)
  - Window: None
  - Process Duration: Exited within < 1 second.

---

## Windows Event Viewer Trace (`Application` Log, Event ID 1026 / 1000)

```text
Application: AjazzBatteryMonitor-win-x64.exe
CoreCLR Version: 8.0.2926.32403
.NET Version: 8.0.29
Description: The process was terminated due to an unhandled exception.
Exception Info: System.DllNotFoundException: Dll was not found.
   at MS.Internal.WindowsBase.NativeMethodsSetLastError.SetWindowLongPtrWndProc(HandleRef hWnd, Int32 nIndex, WndProc dwNewLong)
   at MS.Win32.UnsafeNativeMethods.CriticalSetWindowLong(HandleRef hWnd, Int32 nIndex, WndProc dwNewLong)
   at MS.Win32.HwndSubclass.HookWindowProc(IntPtr hwnd, WndProc newWndProc, IntPtr oldWndProc)
   at MS.Win32.HwndSubclass.SubclassWndProc(IntPtr hwnd, Int32 msg, IntPtr wParam, IntPtr lParam)
```

---

## Root Cause Summary
1. **WPF & WinForms Hybrid Single-File Native DLL Extraction Failure**: Combining WPF (`UseWPF=true`) and WinForms (`UseWindowsForms=true`) in a single-file executable (`PublishSingleFile=true`) without extracting native WPF binaries (`wpfgfx_cor3.dll` / `PresentationNative_cor3.dll`) caused `System.DllNotFoundException` during WPF `HwndSubclass` window procedure hooking.
2. **Improper Application Lifecycle**: WPF application startup lacked an explicit STA message loop (`ApplicationContext`) and relied on a hidden WPF `Window` with unhandled exception gaps.
3. **No Early Startup Logging or Crash Handlers**: No log file was created before WPF initialization, preventing the user from seeing any crash details.
