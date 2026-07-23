#define AppName "AJAZZ Battery Monitor"
#define AppPublisher "AJAZZ Battery Monitor Contributors"
#define AppURL "https://github.com/REPOSITORY_OWNER/AjazzBatteryMonitor"
#define AppId "{{B1A09FE7-9E84-4A3A-A14B-4143A6B9E1E2}"
#define PublishDir GetEnv("AJAZZ_PUBLISH_DIR")
#define OutputDir GetEnv("AJAZZ_INSTALLER_OUTPUT")
#define MyAppExeName "AjazzBattery.App.exe"
#define MyAppVersion GetStringFileInfo(PublishDir + "\\" + MyAppExeName, "ProductVersion")

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
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; DestName: "THIRD-PARTY-NOTICES.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

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
