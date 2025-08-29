# Docker 部署指南

## 快速开始

### 1. 构建并启动容器
```bash
# 在项目根目录执行
docker-compose up -d
```

### 2. 查看服务状态
```bash
# 查看容器状态
docker-compose ps

# 查看日志
docker-compose logs -f mydetector

# 查看健康检查状态
docker-compose ps
```

### 3. 放置客户端 EXE
将构建好的 `Detector.exe` 放到 `artifacts/Detector.exe`：
```bash
# 从 GitHub Actions 下载的 Detector.exe 放到这里
cp /path/to/Detector.exe artifacts/Detector.exe
```

### 4. 访问服务
- 网页界面：http://localhost:3000
- 健康检查：http://localhost:3000/healthz
- API 文档：
  - 创建会话：POST http://localhost:3000/api/session
  - 上报数据：POST http://localhost:3000/api/report
  - 查询结果：GET http://localhost:3000/api/result?session=xxx
  - 下载 EXE：GET http://localhost:3000/download/exe?session=xxx

## 宝塔 Docker 部署

### 1. 在宝塔面板中
- 进入 "Docker" 管理页面
- 点击 "添加容器"
- 选择 "从镜像创建"

### 2. 构建镜像
```bash
# SSH 连接到服务器，在项目目录执行
docker build -t mydetector:latest .
```

### 3. 创建容器
```bash
docker run -d \
  --name mydetector-server \
  -p 3000:3000 \
  -v $(pwd)/server/data.db:/app/data.db \
  -v $(pwd)/artifacts:/app/artifacts \
  -e PORT=3000 \
  -e HOST=0.0.0.0 \
  -e CORS_ORIGIN=* \
  --restart unless-stopped \
  mydetector:latest
```

### 4. 宝塔反向代理（可选）
- 在宝塔面板创建站点
- 设置反向代理到 `http://127.0.0.1:3000`
- 启用 SSL 证书

## 常用命令

```bash
# 停止服务
docker-compose down

# 重新构建并启动
docker-compose up -d --build

# 进入容器调试
docker exec -it mydetector-server sh

# 查看数据库
docker exec -it mydetector-server sqlite3 /app/data.db ".tables"

# 备份数据库
docker cp mydetector-server:/app/data.db ./backup_data.db
```

## 环境变量

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| PORT | 3000 | 服务端口 |
| HOST | 0.0.0.0 | 监听地址 |
| CORS_ORIGIN | * | 跨域来源 |

## 数据持久化

- 数据库文件：`./server/data.db` 挂载到容器内 `/app/data.db`
- 客户端文件：`./artifacts/` 挂载到容器内 `/app/artifacts/`

## 故障排除

1. **端口被占用**：修改 `docker-compose.yml` 中的端口映射
2. **权限问题**：确保 `artifacts` 目录有读写权限
3. **数据库损坏**：删除 `server/data.db` 重新初始化
4. **容器无法启动**：查看日志 `docker-compose logs mydetector`
