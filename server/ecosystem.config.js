module.exports = {
  apps: [
    {
      name: 'mydetector-server',
      script: 'index.js',
      cwd: __dirname,
      instances: 1,
      exec_mode: 'fork',
      env: {
        PORT: process.env.PORT || 3000,
        HOST: process.env.HOST || '0.0.0.0',
        CORS_ORIGIN: process.env.CORS_ORIGIN || '*'
      }
    }
  ]
};


