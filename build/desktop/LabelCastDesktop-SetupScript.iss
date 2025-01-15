; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "LabelCastDesktop"
#define MyAppVersion "1.1"
#define MyAppExeName "LabelCastDesktop.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{CB1196AE-0EE9-49E3-8F54-9F74E8F6757D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
; "ArchitecturesAllowed=x64compatible" specifies that Setup cannot run
; on anything but x64 and Windows 11 on Arm.
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" requests that the
; install be done in "64-bit mode" on x64 or Windows 11 on Arm,
; meaning it should use the native 64-bit Program Files directory and
; the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64compatible
DefaultGroupName={#MyAppName}
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=..\..\dist\desktop\
OutputBaseFilename=LabelCast
SetupIconFile=..\..\images\barcode4.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Code]
procedure InstallDesktopRuntime;
var ResultCode: Integer;
begin
  if not Exec(ExpandConstant('{app}\windowsdesktop-runtime-8.0.11-win-x64.exe'), '', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('Windows Desktop Experience Pack installation failed with code: ' + IntToStr(ResultCode) + '. Please install it manually and try again.', mbError, MB_OK);
    Abort;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Run the .NET runtime installer after the app is installed
    InstallDesktopRuntime;
  end;
end;

[Files]
Source: "..\..\source\LabelCastDesktop\bin\Release\net8.0-windows\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\source\LabelCastDesktop\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\config\*"; DestDir: "c:\Program Info\LabelCast"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\runtimes\windowsdesktop-runtime-8.0.11-win-x64.exe"; DestDir: "{app}"; Flags: deleteafterinstall
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

