; TopTaskBar installer script for Inno Setup 6.
; Build/publish first:
;   dotnet publish .\TopTaskBar\TopTaskBar.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=false -o .\TopTaskBar\bin\Release\manual-publish\win-x64

#define MyAppName "TopTaskBar"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TopTaskBar"
#define MyAppURL "https://github.com/"
#define MyAppExeName "TopTaskBar.exe"
#define MyAppPublishDir "C:\VS2\CSHARP\TopTaskBar\TopTaskBar\bin\Release\manual-publish\win-x64"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
AppId={{47FD43FA-1BB8-4D54-B8BC-1457379E90C2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#MyAppName}
OutputDir=C:\VS2\CSHARP\TopTaskBar\Output
OutputBaseFilename=TopTaskBar_Setup
SolidCompression=yes
WizardStyle=modern dynamic
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\VS2\CSHARP\TopTaskBar\README.md"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files.

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
