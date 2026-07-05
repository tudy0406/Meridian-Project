import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { authApi } from '../api/endpoints';
import { realtime } from '../api/realtime';
import { TOKEN_STORAGE_KEY } from '../config';
import type { LoginResponse, Role } from '../types';

interface AuthUser {
  userId: number;
  fullName: string;
  email: string;
  roles: Role[];
  isOnboarding: boolean;
}

interface AuthState {
  user: AuthUser | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  hasRole: (...roles: Role[]) => boolean;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

const STORAGE_USER_KEY = 'meridian.user';

function loadStoredUser(): AuthUser | null {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  const raw = localStorage.getItem(STORAGE_USER_KEY);
  if (!token || !raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(loadStoredUser);
  const [loading, setLoading] = useState(false);

  const applySession = useCallback((response: LoginResponse) => {
    const authUser: AuthUser = {
      userId: response.userId,
      fullName: response.fullName,
      email: response.email,
      roles: response.roles,
      isOnboarding: response.isOnboarding,
    };
    localStorage.setItem(TOKEN_STORAGE_KEY, response.token);
    localStorage.setItem(STORAGE_USER_KEY, JSON.stringify(authUser));
    setUser(authUser);
  }, []);

  const login = useCallback(
    async (email: string, password: string) => {
      setLoading(true);
      try {
        const response = await authApi.login(email, password);
        applySession(response);
      } finally {
        setLoading(false);
      }
    },
    [applySession],
  );

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    localStorage.removeItem(STORAGE_USER_KEY);
    realtime.stop();
    setUser(null);
  }, []);

  // Establish the realtime connection whenever a user is signed in.
  useEffect(() => {
    if (user) realtime.start();
    return () => {
      if (!user) realtime.stop();
    };
  }, [user]);

  const hasRole = useCallback(
    (...roles: Role[]) => (user ? user.roles.some((r) => roles.includes(r)) : false),
    [user],
  );

  const value = useMemo<AuthState>(
    () => ({ user, loading, login, logout, hasRole }),
    [user, loading, login, logout, hasRole],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
