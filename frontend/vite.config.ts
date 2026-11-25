import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Dev works like live: the website calls /api on its own address, and Vite passes
    // those calls to the API on localhost:5149. One address means no CORS and the same
    // login-cookie behaviour as on Render. The API's UsePathBase("/api") removes the prefix.
    proxy: {
      '/api': 'http://localhost:5149',
    },
  },
})
