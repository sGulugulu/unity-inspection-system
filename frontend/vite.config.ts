import { defineConfig } from 'vite';

// 开发环境同源代理避免放宽后端跨域策略。
export default defineConfig({server: {host: '127.0.0.1', proxy: {'/api': 'http://127.0.0.1:18080'}}});
