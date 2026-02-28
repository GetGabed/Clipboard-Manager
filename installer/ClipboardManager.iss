; Clipboard Manager — Inno Setup 6 script
; Compile:  iscc installer\ClipboardManager.iss
; Output:   release\ClipboardManager-Setup-v0.7.0.exe

[Setup]
AppName=Clipboard Manager
AppVersion=0.7.0
AppPublisher=GetGabed
AppPublisherURL=https://github.com/GetGabed/Clipboard-Manager
AppSupportURL=https://github.com/GetGabed/Clipboard-Manager/issues
DefaultDirName={autopf}\ClipboardManager
DefaultGroupName=Clipboard Manager
OutputDir=..\release
OutputBaseFilename=ClipboardManager-Setup-v0.7.0
Compression=lzma2/ultra64
SolidCompression=yes
SetupIconFile=..\src\ClipboardManager\Resources\Icons\app.ico
UninstallDisplayIcon={app}\ClipboardManager.exe
; No elevation required — installs per-user to %ProgramFiles% via autopf
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\win-x64\ClipboardManager.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Clipboard Manager";        Filename: "{app}\ClipboardManager.exe"
Name: "{group}\Uninstall Clipboard Manager"; Filename: "{uninstallexe}"
Name: "{userstartup}\Clipboard Manager";  Filename: "{app}\ClipboardManager.exe"; Tasks: startupicon

[Tasks]
Name: startupicon; Description: "Start Clipboard Manager automatically with Windows"; Flags: unchecked

[Run]
Filename: "{app}\ClipboardManager.exe"; Description: "Launch Clipboard Manager"; Flags: postinstall nowait

[UninstallDelete]
; Remove per-user settings written to %APPDATA%
Type: filesandordirs; Name: "{userappdata}\ClipboardManager"
