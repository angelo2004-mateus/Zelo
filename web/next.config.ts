import type { NextConfig } from 'next';
const config: NextConfig = {
  async rewrites() {
    return { afterFiles: [{ source: '/:path((?!api/|_next/).*)', destination: '/' }] };
  },
};
export default config;
