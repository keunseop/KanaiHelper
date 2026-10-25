; KanaiHelper Inno Setup Script
; Free installer creator: https://jrsoftware.org/isdl.php

#define MyAppName "KanaiHelper"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "KanaiHelper Project"
#define MyAppExeName "KanaiHelper.exe"

[Setup]
AppId={{E6B5D7A9-10B4-4D62-817E-692B82F662F1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={userappdata}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\release
OutputBaseFilename=KanaiHelper_v{#MyAppVersion}_Setup
SetupIconFile=..\assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\KanaiHelper.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Hotkeys.ini"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
Source: "..\USAGE_KO.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\assets\app.ico"; DestDir: "{app}\assets"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent


