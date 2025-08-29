const express = require('express');
const path = require('path');
const cors = require('cors');
const Database = require('better-sqlite3');
const { v4: uuidv4 } = require('uuid');
const bodyParser = require('body-parser');

const app = express();
const db = new Database('./data.db');
const fs = require('fs');

app.use(cors());
app.use(bodyParser.json({ limit: '1mb' }));

// 静态网站：托管 web 目录
const webDir = path.join(__dirname, '../web');
app.use(express.static(webDir));
app.get('/', (req, res) => {
  res.sendFile(path.join(webDir, 'index.html'));
});

// 下载基础 exe 并按 session 改名（用于通过文件名识别用户）
// 请将已构建的 Detector.exe 放到 artifacts/Detector.exe
app.get('/download/exe', (req, res) => {
  const session = (req.query.session || '').toString();
  const endpoint = (req.query.endpoint || '').toString();
  if (!session) return res.status(400).json({ error: 'missing session' });
  const basePath = path.join(__dirname, '../artifacts/Detector.exe');
  if (!fs.existsSync(basePath)) {
    return res.status(500).json({ error: 'server missing base exe at artifacts/Detector.exe' });
  }
  let filename = `Detector_${session}.exe`;
  if (endpoint) {
    // base64url 编码 endpoint 以便放入文件名
    const buf = Buffer.from(endpoint, 'utf8');
    const b64 = buf.toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
    filename = `Detector_${session}__e_${b64}.exe`;
  }
  res.setHeader('Content-Disposition', `attachment; filename="${filename}"`);
  res.setHeader('Content-Type', 'application/octet-stream');
  fs.createReadStream(basePath).pipe(res);
});

// 创建 session
app.post('/api/session', (req, res) => {
  const session = uuidv4();
  const now = Date.now();
  try {
    db.prepare('INSERT INTO sessions(session_id, created_at, result_json) VALUES(?,?,?)').run(session, now, null);
    res.json({ session });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// 上报结果（客户端调用）
app.post('/api/report', (req, res) => {
  const body = req.body;
  const session = body.session || body.session_id;
  if (!session) return res.status(400).json({ error: 'missing session' });

  const json = JSON.stringify(body);
  try {
    const result = db.prepare('UPDATE sessions SET result_json = ? WHERE session_id = ?').run(json, session);
    if (result.changes === 0) {
      db.prepare('INSERT INTO sessions(session_id, created_at, result_json) VALUES(?,?,?)').run(session, Date.now(), json);
      res.json({ status: 'ok', inserted: true });
    } else {
      res.json({ status: 'ok' });
    }
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// 查询结果
app.get('/api/result', (req, res) => {
  const session = req.query.session;
  if (!session) return res.status(400).json({ error: 'missing session query' });

  try {
    const row = db.prepare('SELECT result_json FROM sessions WHERE session_id = ?').get(session);
    if (!row || !row.result_json) return res.json({ found: false });
    try {
      const parsed = JSON.parse(row.result_json);
      res.json({ found: true, result: parsed });
    } catch (e) {
      res.json({ found: true, result_raw: row.result_json });
    }
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Server listening on ${PORT}`));



