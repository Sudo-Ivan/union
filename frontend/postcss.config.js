const path = require('path');

const cssVarsFiles = [
  './src/Styles/Variables/dimensions',
  './src/Styles/Variables/fonts',
  './src/Styles/Variables/animations',
  './src/Styles/Variables/zIndexes'
].map(require.resolve);

const mixinsFiles = [
  'frontend/src/Styles/Mixins/cover.css',
  'frontend/src/Styles/Mixins/linkOverlay.css',
  'frontend/src/Styles/Mixins/scroller.css',
  'frontend/src/Styles/Mixins/truncate.css'
];

module.exports = {
  plugins: {
    autoprefixer: {},
    'postcss-mixins': {
      mixinsFiles
    },
    'postcss-simple-vars': {
      variables: () => {
        return cssVarsFiles.reduce((acc, vars) => {
          return Object.assign(acc, require(vars));
        }, {});
      }
    },
    '@csstools/postcss-global-data': {
      files: [path.join(__dirname, 'src/Styles/Variables/breakpoints.css')]
    },
    'postcss-custom-media': {},
    'postcss-color-function': {},
    'postcss-nested': {}
  }
};
