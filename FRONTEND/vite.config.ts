import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import basicSsl from '@vitejs/plugin-basic-ssl'

// https://vite.dev/config/
// The dev server runs over HTTPS (self-signed cert) so the whole app is served
// over TLS, matching the production posture.
export default defineConfig({
  plugins: [react(), basicSsl()],
  server: {
    https: {},
    port: 5173,
  },
})
