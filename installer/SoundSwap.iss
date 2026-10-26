; Inno Setup 6 script. Built by scripts/publish.ps1 -Installer (and by the release workflow).
; The version comes from the SOUNDSWAP_VERSION environment variable (set from the git tag).
#define AppName "SoundSwap"
#define AppVersion GetEnv("SOUNDSWAP_VERSION")
#if AppVersion == ""
  #define AppVersion "0.0.0"
#endif

[Setup]
AppId={{950F557F-F888-4E14-852A-D6C068EF61D7}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Stevehunn
AppPublisherURL=https://sounddeck.stream/soundswap
AppSupportURL=https://github.com/Stevehunn/soundswap/issues
AppUpdatesURL=https://github.com/Stevehunn/soundswap/releases
VersionInfoVersion={#AppVersion}
; Per-user install: no administrator rights, files go to %LOCALAPPDATA%\Programs\SoundSwap.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=SoundSwap-Setup-{#AppVersion}
SetupIconFile=..\src\SoundSwap.App\Assets\SoundSwap.ico
UninstallDisplayIcon={app}\SoundSwap.exe
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\SoundSwap.exe"

[Run]
Filename: "{app}\SoundSwap.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// The app registers itself in HKCU\...\Run when the user enables "Start with Windows":
// remove that entry (and stop a running instance) when SoundSwap is uninstalled.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
    Exec('taskkill.exe', '/F /IM SoundSwap.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'SoundSwap');
end;
