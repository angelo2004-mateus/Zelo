export function safeReturnPath(value: string | null) {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.includes('\\')) {
    return '/dashboard';
  }
  const pathname = value.split(/[?#]/)[0];
  if (!['/dashboard', '/security'].includes(pathname)) {
    return '/dashboard';
  }
  return value;
}
