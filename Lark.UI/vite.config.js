// vite.config.js
import autoprefixer from 'autoprefixer';
import cssnano from 'cssnano';
import postcssImport from 'postcss-import';
import postcssNested from 'postcss-nested';
import postcssPresetEnv from 'postcss-preset-env';

import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [
    {
      name: 'remove-type-module',
      apply: 'build',
      transformIndexHtml(html) {
        return html.replace(/type="module"/g, '');
      },
    },
  ],
  optimizeDeps: {
    // include: ['urlpattern-polyfill'],
  },
  build: {
    minify: 'esbuild', // use esbuild for minification
    target: 'es2018',
    rollupOptions: {
      output: {
        // format: 'commonjs',
        // manualChunks: (id) => {
        //   if (id.includes('node_modules')) {
        //     // if the file is in node_modules, it goes to the vendors chunk
        //     return 'vendors';
        //   }
        // },
      },
    },
  },
  esbuild: {
    minify: true,
  },
  server: {
    watch: {
      usePolling: true,
    },
  },
  css: {
    postcss: {
      plugins: [
        autoprefixer(),
        cssnano(),
        postcssPresetEnv(),
        postcssImport(),
        postcssNested(),
      ],
    },
  },
});
