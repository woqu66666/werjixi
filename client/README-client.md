构建：
- 需要 .NET 7 SDK（https://dotnet.microsoft.com/download）
- 在 client/ 目录运行：
    dotnet restore
    dotnet build -c Release

生成可执行（框架依赖）：
    dotnet publish -c Release -r win-x64 --self-contained false

生成单文件自包含 exe（较大，但不依赖系统已安装 .NET）：
    dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true

运行测试（本地 server 为 http://localhost:3000）：
    Detector.exe my-session-123
或让浏览器通过 mydetector://start?session=my-session-123 调用（需安装注册协议，见 installer/mydetector.iss）

注意：
- 上报地址可外部配置（优先级高→低）：
  1) 协议/参数：`mydetector://start?session=xxx&endpoint=https%3A%2F%2Fapi.example.com%2Fapi%2Freport` 或命令行 `--endpoint=https://...`
  2) 环境变量：`MYDETECTOR_ENDPOINT=https://...`
  3) 同目录 `config.json`：`{"serverEndpoint":"https://.../api/report"}`
  4) 默认：`http://localhost:3000/api/report`
- 需要允许程序弹窗以同意隐私提示。



