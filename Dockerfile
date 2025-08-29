FROM node:18-alpine

WORKDIR /app

# 复制 package.json 并安装依赖
COPY server/package*.json ./
RUN npm ci --only=production

# 复制服务端代码
COPY server/ ./

# 复制静态网页
COPY web/ ./web/

# 创建 artifacts 目录
RUN mkdir -p artifacts

# 暴露端口
EXPOSE 3000

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000/healthz', (res) => { process.exit(res.statusCode === 200 ? 0 : 1) })"

# 启动命令
CMD ["node", "index.js"]
