; Inno Setup for .NET Framework 4.8 build
[Setup]
AppName=MyDetector
AppVersion=1.0
DefaultDirName={pf}\MyDetector
DefaultGroupName=MyDetector
OutputBaseFilename=MyDetectorInstaller-net48
Compression=lzma
SolidCompression=yes

[Files]
Source: "..\client-net48\bin\Release\Detector.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\MyDetector"; Filename: "{app}\Detector.exe"

[Registry]
Root: HKCR; Subkey: "mydetector"; ValueType: string; ValueName: ""; ValueData: "URL:MyDetector Protocol"; Flags: uninsdeletekey
Root: HKCR; Subkey: "mydetector"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: "mydetector\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\Detector.exe"",0"; Flags: uninsdeletekey
Root: HKCR; Subkey: "mydetector\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Detector.exe"" ""%1"""; Flags: uninsdeletekey

[Run]
Filename: "{app}\Detector.exe"; Description: "Run MyDetector"; Flags: nowait postinstall skipifsilent


