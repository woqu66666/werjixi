; Inno Setup script - 注册 mydetector 协议并安装 Detector.exe
[Setup]
AppName=MyDetector
AppVersion=1.0
DefaultDirName={pf}\MyDetector
DefaultGroupName=MyDetector
OutputBaseFilename=MyDetectorInstaller
Compression=lzma
SolidCompression=yes

[Files]
Source: "..\client\bin\Release\net7.0\win-x64\publish\Detector.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\MyDetector"; Filename: "{app}\Detector.exe"

[Registry]
Root: HKCR; Subkey: "mydetector"; ValueType: string; ValueName: ""; ValueData: "URL:MyDetector Protocol"; Flags: uninsdeletekey
Root: HKCR; Subkey: "mydetector"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: uninsdeletevalue
Root: HKCR; Subkey: "mydetector\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\Detector.exe"",0"; Flags: uninsdeletekey
Root: HKCR; Subkey: "mydetector\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Detector.exe"" ""%1"""; Flags: uninsdeletekey

[Run]
Filename: "{app}\Detector.exe"; Description: "Run MyDetector"; Flags: nowait postinstall skipifsilent

; 说明：
; - 请把编译后 publish 的 Detector.exe 放到 client/bin/Release/net7.0/win-x64/publish/ 路径或相应修改 Source 路径。
; - 安装后会注册 mydetector:// 协议，浏览器点击 mydetector://start?session=xxx 时会把 URL 作为参数传给 Detector.exe。



