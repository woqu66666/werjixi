部署到宝塔（BT 面板）步骤：

1) 服务器准备
- 安装 Node.js（建议 LTS 版本）
- 安装 PM2：`npm i -g pm2`

2) 上传代码
- 把 `server`、`web`、`artifacts` 三个目录上传到同一根目录（保持结构不变）
- 在 `server` 目录执行：`npm ci`
- 初始化数据库：`npm run init-db`

3) 运行（PM2）
- 进入 `server` 目录，执行：`pm2 start ecosystem.config.js`
- 或自定义端口：`PORT=3001 pm2 start ecosystem.config.js`
- 查看：`pm2 logs mydetector-server`

4) 宝塔反向代理（可选）
- 在宝塔面板新建站点，反代到 `http://127.0.0.1:3000`
- 若需自定义域名/SSL，在站点设置里开启

5) 客户端 EXE 放置
- 将构建出的 `Detector.exe` 放到 `artifacts/Detector.exe`
- 下载接口：`/download/exe?session=xxx&endpoint=https://你的域名/api/report`

6) 健康检查
- 访问 `http://<服务器IP>:3000/healthz` 返回 `{ ok: true }` 表示服务正常


