#define AppName "AlphaTray"
#define AppPublisher "Private"
#define AppVersion GetEnv("ALPHATRAY_VERSION")
#if AppVersion == ""
  #define AppVersion "0.1.0"
#endif
#define PublishDir GetEnv("ALPHATRAY_PUBLISH_DIR")
#if PublishDir == ""
  #define PublishDir "..\publish\AlphaTray"
#endif
#define OutputDir GetEnv("ALPHATRAY_OUTPUT_DIR")
#if OutputDir == ""
  #define OutputDir "..\dist\installer"
#endif

[Setup]
AppId={{5B109A1D-9372-4F65-B7F1-81594D3D1A6A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=AlphaTray-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=..\assets\default_purple\default_purple.ico
UninstallDisplayIcon={app}\AlphaTray.App.exe
CloseApplications=yes
CloseApplicationsFilter=AlphaTray.App.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start AlphaTray when Windows starts"; GroupDescription: "Startup options:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AlphaTray"; Filename: "{app}\AlphaTray.App.exe"; WorkingDir: "{app}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AlphaTray"; ValueData: """{app}\AlphaTray.App.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\AlphaTray.App.exe"; Description: "Launch AlphaTray"; Flags: nowait postinstall skipifsilent unchecked
