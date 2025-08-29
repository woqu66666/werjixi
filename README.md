快速上手（本地测试）
1. 启动示例服务器
   cd server
   npm install
   node init-db.js
   node index.js

2. 构建客户端
   - 安装 .NET 7 SDK
   cd client
   dotnet restore
   dotnet publish -c Release -r win-x64 --self-contained false

   publish 后的 Detector.exe 在 client/bin/Release/net7.0/win-x64/publish/

3. (可选) 构建安装器
   - 安装 Inno Setup
   - 修改 installer/mydetector.iss 中 Source 路径指向上一步的 publish 目录
   - 用 Inno Setup 编译生成 installer.exe

4. 打开 web/index.html（或把 web 放入静态服务器），点击 “开始检测”。

安全/发布建议
- 生产环境请使用 HTTPS 并对 server 做认证与访问限制。
- 为避免 SmartScreen / Windows Defender 弹窗，建议对 exe 做代码签名（EV Code Signing）。如果需要，我可以给出签名流程与建议供应商。

说明与注意事项（重要）
- 默认客户端代码中 SERVER_ENDPOINT 为 http://localhost:3000/api/report。若你部署到公网请改成 https://yourdomain/api/report 并重新编译。生产请强制使用 HTTPS。
- 浏览器安全：有时浏览器会询问是否打开外部应用；这属于浏览器/OS 保护，无法通过网页完全免除。
- 未签名的 exe 在外部用户使用时可能被 SmartScreen 标记。若面向真实用户，建议代码签名证书（EV）。
- 隐私合规：示例中没有采集 MAC 地址、硬盘序列号或用户名等敏感信息；若你要采集这些，请确认合规性并在隐私声明中明确告知用户。
- 我把示例实现做得尽量简单、易改：你可扩展客户端以收集更多信息（例如 CPU 指令集：需 native CPUID 实现）、或使用 DXGI/DirectX API 获取显卡功能细节（需 native C++ 或增加库）。

CI（GitHub Actions）
- 已提供 Windows 构建工作流：`.github/workflows/build-windows.yml`
- 触发后将在 windows-latest 上构建 `client-net48/Detector.exe` 与安装器，并在 Actions 页面产出工件下载。
- 如需代码签名，可在仓库 Secrets 中配置 `CODESIGN_PFX`（Base64）与 `CODESIGN_PFX_PASSWORD`，然后开启工作流中的签名步骤。



