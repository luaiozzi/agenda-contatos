import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// Em desenvolvimento, as chamadas /api/* são encaminhadas para a API .NET.
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true
      }
    }
  }
});
