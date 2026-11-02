import { defineConfig, globalIgnores } from 'eslint/config';
import next from 'eslint-config-next/core-web-vitals';
import ts from 'eslint-config-next/typescript';
import prettier from 'eslint-config-prettier/flat';
export default defineConfig([
  ...next,
  ...ts,
  prettier,
  { rules: { curly: ['error', 'all'] } },
  globalIgnores(['.next/**', 'next-env.d.ts']),
]);
