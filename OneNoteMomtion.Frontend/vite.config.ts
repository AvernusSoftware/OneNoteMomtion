import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],

  // Relative paths, because the page is served from a virtual host mapped onto the wwwroot folder
  // next to the engine executable, not from the server root.
  base: './',

  build: {
    outDir: 'dist',
    emptyOutDir: true,
    // The palette is a single small window - splitting it into chunks would only multiply network requests.
    chunkSizeWarningLimit: 1500,
  },

  server: {
    port: 5273,
    // The interface texts live in OneNoteMomtion.Core/Localization, shared with the engine and the ribbon.
    fs: {
      allow: ['.', '../OneNoteMomtion.Core/Localization'],
    },
  },
})
