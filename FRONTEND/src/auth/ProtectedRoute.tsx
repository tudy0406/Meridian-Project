import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from './AuthContext';
import type { Role } from '../types';

/** Guards routes: requires authentication, and optionally one of the given roles. */
export function ProtectedRoute({ children, roles }: { children: ReactNode; roles?: Role[] }) {
  const { user, hasRole } = useAuth();

  if (!user) return <Navigate to="/login" replace />;
  if (roles && roles.length > 0 && !hasRole(...roles)) return <Navigate to="/" replace />;

  return <>{children}</>;
}
