import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  // Where the ASP.NET API runs during development (see Properties/launchSettings.json).
  const apiTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5080';

  return {
    // Set VITE_BASE=/app/ if the SPA is hosted under a sub-path of the ASP.NET site.
    base: env.VITE_BASE || '/',
    plugins: [react()],
    resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
    server: {
      port: 5173,
      // Same-origin in dev => the Identity cookie "just works", no CORS needed.
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
      },
    },
    build: {
      sourcemap: false,
      chunkSizeWarningLimit: 1200,
    },
  };
});
