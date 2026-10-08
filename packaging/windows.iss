#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
[Setup]
AppId={{C583686C-360E-4F04-915F-BBFCC1B1ACD2}
AppName=Voxen
AppVersion={#AppVersion}
AppPublisher=EliasArruda
AppPublisherURL=https://github.com/EliasArruda/Voxen
DefaultDirName={localappdata}\Programs\Voxen
DefaultGroupName=Voxen
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\release
OutputBaseFilename=Voxen-{#AppVersion}-windows-x64-setup
Compression=lzma2
SolidCompression=yes
SetupIconFile=..\Assets\voxen.ico
UninstallDisplayIcon={app}\Voxen.exe
[Files]
Source: "..\artifacts\Voxen-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Voxen"; Filename: "{app}\Voxen.exe"; IconFilename: "{app}\Voxen.exe"
[Run]
Filename: "https://go.microsoft.com/fwlink/p/?LinkId=2124703"; Description: "Install Microsoft Edge WebView2 Runtime if needed"; Flags: shellexec postinstall unchecked skipifsilent
Filename: "{app}\Voxen.exe"; Description: "Open Voxen"; Flags: nowait postinstall skipifsilent
