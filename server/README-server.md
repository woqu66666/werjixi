本地运行：
1. 安装 Node.js (>=16)
2. 在 server/ 目录运行：
   npm install
   node init-db.js
   node index.js
3. 默认监听 http://localhost:3000

测试：
- POST /api/session 创建 session：
  curl -X POST http://localhost:3000/api/session
- POST /api/report 上传（客户端会自动上传）
- GET /api/result?session=xxx 查询结果

注意：生产部署请加 HTTPS（反代或使用证书），并对 API 做鉴权与访问控制。



