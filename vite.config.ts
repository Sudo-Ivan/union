import path from 'path';
import { spawn, ChildProcess } from 'child_process';
import { cpSync, mkdirSync, readdirSync } from 'fs';
import react from '@vitejs/plugin-react';
import {
  defineConfig,
  Plugin,
  ResolvedConfig,
  transformWithOxc,
} from 'vite';
import { patchCssModules } from 'vite-css-modules';

const src = path.resolve(__dirname, 'frontend/src');
const outDir = path.resolve(__dirname, '_output/UI');

const contentDir = path.join(src, 'Content');
const themeCss = path.join(src, 'Styles/Themes/themes.css');

function htmlFiles() {
  return readdirSync(src).filter((name) => name.endsWith('.html'));
}

function copyStaticContent(): Plugin {
  function copyAll() {
    mkdirSync(outDir, { recursive: true });

    cpSync(contentDir, path.join(outDir, 'Content'), { recursive: true });
    cpSync(themeCss, path.join(outDir, 'Content', 'theme.css'));

    for (const file of htmlFiles()) {
      cpSync(path.join(src, file), path.join(outDir, file));
    }
  }

  function owns(file: string) {
    return (
      file.startsWith(contentDir) ||
      file === themeCss ||
      (path.dirname(file) === src && file.endsWith('.html'))
    );
  }

  return {
    name: 'copy-static-content',

    buildStart() {
      for (const file of htmlFiles()) {
        this.addWatchFile(path.join(src, file));
      }

      this.addWatchFile(contentDir);
      this.addWatchFile(themeCss);
    },

    closeBundle() {
      copyAll();
    },

    configureServer(server) {
      copyAll();

      server.watcher.add([contentDir, themeCss, path.join(src, '*.html')]);

      server.watcher.on('add', (file) => owns(file) && copyAll());
      server.watcher.on('change', (file) => owns(file) && copyAll());
    },
  };
}

function cssModuleTypes(): Plugin {
  let child: ChildProcess | undefined;

  function stop() {
    child?.kill();
    child = undefined;
  }

  return {
    name: 'css-module-types',
    apply: 'serve',

    configureServer(server) {
      const bin = process.platform === 'win32' ? 'tcm.cmd' : 'tcm';

      child = spawn(
        path.join(__dirname, 'node_modules', '.bin', bin),
        ['frontend/src', '--camelCase', '--pattern', '**/*.module.css', '--watch'],
        { cwd: __dirname, stdio: 'inherit' }
      );

      server.httpServer?.on('close', stop);
      process.on('exit', stop);
    },
  };
}

// The merged Radarr codebase treats every plain .css file as a CSS module
// (webpack css-loader style). Vite only treats *.module.css as a module, so
// route plain .css imports through vite-css-modules by appending its
// '?.module.css' query - the same mechanism the plugin uses internally for
// composes dependencies.
const GLOBAL_CSS_FILES = new Set([
  path.join(src, 'index.css'),
  path.join(src, 'Styles', 'globals.css'),
]);

function cssAsModules(): Plugin {
  return {
    name: 'css-as-modules',
    enforce: 'pre',

    async resolveId(source, importer) {
      // Webpack-style '~' prefix used by composes declarations in merged
      // Radarr .css files (e.g. composes: x from '~Components/Label.css' or
      // '~./Local.css'). Strip the tilde and let the src aliases resolve it.
      if (source.startsWith('~')) {
        const cleanSource = source.slice(1);
        const resolved = await this.resolve(cleanSource, importer, {
          skipSelf: true,
        });

        return resolved ?? null;
      }

      if (
        !importer ||
        !source.endsWith('.css') ||
        source.endsWith('.module.css') ||
        !/\.[jt]sx?(?:\?.*)?$/.test(importer)
      ) {
        return null;
      }

      const resolved = await this.resolve(source, importer, {
        skipSelf: true,
      });

      if (!resolved || resolved.external) {
        return null;
      }

      const [filePath] = resolved.id.split('?', 2);

      if (!filePath.startsWith(src) || GLOBAL_CSS_FILES.has(filePath)) {
        return null;
      }

      return `${resolved.id}?.module.css`;
    },
  };
}

// The merged Radarr codebase has .js files that contain JSX. Vite's builtin
// transform only enables JSX for .jsx/.tsx, so transform those .js files
// here with the jsx lang before the builtin plugin sees them.
function jsxInJsFiles(): Plugin {
  let config: ResolvedConfig;

  return {
    name: 'jsx-in-js-files',
    enforce: 'pre',

    configResolved(resolvedConfig) {
      config = resolvedConfig;
    },

    transform: {
      filter: {
        id: {
          include: /frontend\/src\/.*\.js$/,
          exclude: /node_modules/,
        },
      },
      async handler(code, id) {
        const result = await transformWithOxc(
          code,
          id,
          {
            lang: 'jsx',
            jsx: { runtime: 'automatic', importSource: 'react' },
          } as Parameters<typeof transformWithOxc>[2],
          undefined,
          config
        );

        return {
          code: result.code,
          map: result.map,
        };
      },
    },
  };
}

const srcAliases = Object.fromEntries(
  readdirSync(src, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => [entry.name, path.join(src, entry.name)])
);

export default defineConfig({
  plugins: [
    jsxInJsFiles(),
    cssAsModules(),
    patchCssModules({ exportMode: 'default' }),
    react(),
    copyStaticContent(),
    cssModuleTypes(),
  ],

  base: '/',

  server: {
    port: Number(process.env.SONARR_VITE_PORT ?? 8959),
    strictPort: true,
    hmr: {
      clientPort: Number(process.env.SONARR_VITE_PORT ?? 8959),
    },
  },

  define: {
    __DEV__: JSON.stringify(process.env.NODE_ENV !== 'production'),
  },

  oxc: {
    // The merged Radarr codebase has .js files that contain JSX. Include them
    // in the oxc transform; the build.moduleTypes override below parses them
    // as jsx.
    include: /\.(m?ts|[jt]sx?)$/,
    exclude: /node_modules/,
  },

  build: {
    outDir,
    emptyOutDir: true,
    target: 'esnext',
    sourcemap: true,
    rolldownOptions: {
      moduleTypes: {
        // The merged Radarr codebase has .js files that contain JSX.
        '.js': 'jsx',
      },
    },
  },

  experimental: {
    renderBuiltUrl(filename, { hostType }) {
      if (hostType === 'html') {
        return `/${filename}`;
      }

      return { relative: true };
    },
  },

  resolve: {
    // Prefer the migrated TypeScript sources over the stale Radarr .js
    // files that share the same module name.
    extensions: ['.ts', '.tsx', '.mjs', '.mts', '.jsx', '.js', '.json'],
    alias: [
      ...Object.entries(srcAliases).map(([find, replacement]) => ({
        find,
        replacement,
      })),
      { find: 'jquery', replacement: 'jquery/dist/jquery.min.js' },
    ],
  },

  css: {
    postcss: path.resolve(__dirname, 'frontend'),
  },
});
