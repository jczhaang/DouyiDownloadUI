#define MyAppName "抖音下载"
#ifndef MyAppVersion
#define MyAppVersion "1.2.0"
#endif
#define MyAppPublisher "jczhaang"
#define MyAppExeName "DouyiDownloadUI.exe"

[Setup]
AppId={{6A4B9E2C-3F1D-4E8A-9C5B-7D2A0F1B3E44}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
SetupIconFile=..\src\DouyiDownloadUI\assets\app.ico
DefaultDirName={autopf}\DouyiDownloadUI
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=DouyiDownloadUI-{#MyAppVersion}-setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务："

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\tools\yt-dlp.exe"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "..\tools\ffmpeg.exe"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "..\LICENSES\*"; DestDir: "{app}\licenses"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "立即运行"; Flags: nowait postinstall skipifsilent
