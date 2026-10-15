import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

// The frontend's coding standards. CI runs this on every pull request (npm run lint).
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
    rules: {
      // Naming standards: the frontend version of the backend's .editorconfig naming rules.
      '@typescript-eslint/naming-convention': [
        'error',
        // Everything defaults to camelCase: functions, parameters, properties.
        { selector: 'default', format: ['camelCase'] },
        // Variables: camelCase, PascalCase for components and schemas (TaskSchema),
        // UPPER_CASE for fixed settings (API_URL).
        { selector: 'variable', format: ['camelCase', 'PascalCase', 'UPPER_CASE'] },
        // Functions: camelCase, or PascalCase for React components (TaskList).
        { selector: 'function', format: ['camelCase', 'PascalCase'] },
        // Types: PascalCase (Task, Props, ThemeChoice).
        { selector: 'typeLike', format: ['PascalCase'] },
        // Keys that need quotes, like 'Content-Type', can't follow camelCase.
        { selector: 'objectLiteralProperty', modifiers: ['requiresQuotes'], format: null },
        // Imports keep the name their library gives them (react, tseslint).
        { selector: 'import', format: null },
      ],
    },
  },
])
