// 初始化 SQLite 数据库（创建 sessions 表）
const Database = require('better-sqlite3');
const db = new Database('./data.db');
db.prepare(`CREATE TABLE IF NOT EXISTS sessions (
  session_id TEXT PRIMARY KEY,
  created_at INTEGER,
  result_json TEXT
);`).run();
db.close();
console.log('DB initialized: data.db');



