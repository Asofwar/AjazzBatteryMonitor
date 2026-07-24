; AJAZZ Battery Monitor — Inno Setup Script v1.3.0
; https://jrsoftware.org/ishelp/

#define AppName        "AJAZZ Battery Monitor"
#define AppPublisher   "AJAZZ Battery Monitor Contributors"
#define AppURL         "https://github.com/REPOSITORY_OWNER/AjazzBatteryMonitor"
#define AppId          "{{B1A09FE7-9E84-4A3A-A14B-4143A6B9E1E2}"
#define PublishDir     GetEnv("AJAZZ_PUBLISH_DIR")
#define OutputDir      GetEnv("AJAZZ_INSTALLER_OUTPUT")
#define MyAppExeName   "AjazzBatteryMonitor.exe"
#define MyAppVersion   GetStringFileInfo(PublishDir + "\" + MyAppExeName, "ProductVersion")
#define IconFile       "..\installer\assets\AppIcon.ico"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#MyAppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
LicenseFile=..\LICENSE
DefaultDirName={localappdata}\Programs\AJAZZ Battery Monitor
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#OutputDir}
OutputBaseFilename=AjazzBatteryMonitor-Setup-v{#MyAppVersion}
SetupIconFile={#IconFile}
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=AjazzBatteryMonitor.exe

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Desktop shortcut — unchecked by default
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

; Autostart with Windows — checked by default on first install, not reset on upgrade.
; checkedonce: shown checked on first install; upgrade preserves previous state.
Name: "autostart"; Description: "Запускать AJAZZ Battery Monitor вместе с Windows"; GroupDescription: "Дополнительные параметры:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; DestName: "THIRD-PARTY-NOTICES.txt"; Flags: ignoreversion
Source: "..\installer\assets\AppIcon.ico"; DestDir: "{app}"; DestName: "AppIcon.ico"; Flags: ignoreversion

[Icons]
; Start Menu shortcut — no --background: manual launch opens the main window
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\AppIcon.ico"
; Desktop shortcut — conditional on desktopicon task
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\AppIcon.ico"; Tasks: desktopicon

[Registry]
; Autostart entry: runs with --background so Windows login starts silently in the tray.
; The closing quote is BEFORE --background so Windows correctly handles paths with spaces.
; Expected value: "C:\Users\...\AjazzBatteryMonitor.exe" --background
Root: HKCU; \
    Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; \
    ValueName: "AjazzBatteryMonitor"; \
    ValueData: """{app}\{#MyAppExeName}"" --background"; \
    Flags: uninsdeletevalue; \
    Tasks: autostart

[Run]
; Post-install launch — no --background: opens the main window on Overview tab
Filename: "{app}\{#MyAppExeName}"; \
    Description: "Запустить {#AppName}"; \
    Flags: nowait postinstall skipifsilent

[Code]
var
  RemoveDataCheckBox: TNewCheckBox;

procedure InitializeUninstallProgressForm;
begin
  RemoveDataCheckBox := TNewCheckBox.Create(UninstallProgressForm);
  RemoveDataCheckBox.Parent := UninstallProgressForm;
  RemoveDataCheckBox.Caption := 'Also remove user settings and history';
  RemoveDataCheckBox.Left := UninstallProgressForm.StatusLabel.Left;
  RemoveDataCheckBox.Top := UninstallProgressForm.StatusLabel.Top + ScaleY(28);
  RemoveDataCheckBox.Width := UninstallProgressForm.StatusLabel.Width;
  RemoveDataCheckBox.Checked := False;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usUninstall) and Assigned(RemoveDataCheckBox) and RemoveDataCheckBox.Checked then
    DelTree(ExpandConstant('{localappdata}\AjazzBatteryMonitor'), True, True, True);
end;
