import { BrowserRouter } from 'react-router';
import { AuthProvider } from '@/features/auth/AuthProvider';
import { AppRoutes } from './routes';
export default function Application() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
