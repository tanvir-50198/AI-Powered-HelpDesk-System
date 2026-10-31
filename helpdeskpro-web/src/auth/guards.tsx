import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { Spin } from 'antd';
import type { Role } from '@/api/types';
import { useAuth } from './AuthContext';

export function FullPageSpinner() {
  return (
    <div style={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }} role="status" aria-label="Loading">
      <Spin size="large" />
    </div>
  );
}

/** Everything inside needs a signed-in user. */
export function RequireAuth() {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) return <FullPageSpinner />;
  if (!user) {
    const returnTo = location.pathname + location.search;
    const search = returnTo !== '/' && returnTo !== '/dashboard' ? `?returnUrl=${encodeURIComponent(returnTo)}` : '';
    return <Navigate to={`/login${search}`} replace />;
  }
  return <Outlet />;
}

/** Login / register: signed-in users go to the app instead. */
export function PublicOnly() {
  const { user, isLoading } = useAuth();
  if (isLoading) return <FullPageSpinner />;
  if (user) return <Navigate to="/dashboard" replace />;
  return <Outlet />;
}

/** Restrict a branch of routes to some roles. */
export function RequireRole({ roles }: { roles: Role[] }) {
  const { user } = useAuth();
  if (!user) return null;
  if (!roles.includes(user.role)) return <Navigate to="/forbidden" replace />;
  return <Outlet />;
}
