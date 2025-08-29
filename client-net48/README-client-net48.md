面向公众的小体积客户端（.NET Framework 4.8）

优点：
- 体积小（数 MB 级，取决于依赖），大部分 Windows 10/11 已内置 .NET 4.8 运行时
- 启动快，兼容度高

构建（Windows 上）：
1) 安装 Visual Studio（或 Build Tools），确保“.NET 桌面开发”工作负载和 .NET Framework 4.8 SDK
2) 在 `client-net48` 目录执行：
   - 用 VS 打开并构建 Release
   - 或使用 msbuild：
     `msbuild DetectorNet48.csproj /p:Configuration=Release`

发布产物：
- `client-net48\bin\Release\Detector.exe`

测试运行：
`Detector.exe <session>` 或由 `mydetector://start?session=...` 触发

改服务器地址：
- 不需要重打包：可通过以下顺序覆盖上报地址（高→低）：
  1) 协议/参数：`mydetector://start?session=xxx&endpoint=https%3A%2F%2Fapi.example.com%2Fapi%2Freport` 或命令行 `--endpoint=https://...`
  2) 环境变量：`MYDETECTOR_ENDPOINT=https://...`
  3) 同目录 `config.json`：`{"serverEndpoint":"https://.../api/report"}`
  4) 默认：`http://localhost:3000/api/report`


