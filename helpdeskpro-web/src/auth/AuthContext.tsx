import { createContext, useCallback, useContext, useEffect, useMemo, type ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi } from '@/api/endpoints';
import { setUnauthorizedHandler } from '@/api/client';
import type { CurrentUser, LoginRequest, RegisterRequest } from '@/api/types';

const ME_KEY = ['auth', 'me'] as const;

interface AuthContextValue {
  user: CurrentUser | null;
  /** true only during the very first "who am I?" request */
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<CurrentUser>;
  register: (data: RegisterRequest) => Promise<CurrentUser>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();

  const { data: user = null, isLoading } = useQuery({
    queryKey: ME_KEY,
    queryFn: authApi.me,
    staleTime: Infinity,
    retry: false,
  });

  // Any 401 from the API means the cookie expired: drop the user, guards redirect to /login.
  useEffect(() => {
    setUnauthorizedHandler(() => {
      queryClient.setQueryData(ME_KEY, null);
    });
    return () => setUnauthorizedHandler(null);
  }, [queryClient]);

  /** Drop every cached query except the session itself, so no account ever sees another's data. */
  const resetUserData = useCallback(() => {
    queryClient.removeQueries({ predicate: (q) => q.queryKey[0] !== ME_KEY[0] });
  }, [queryClient]);

  const login = useCallback(
    async (data: LoginRequest) => {
      const me = await authApi.login(data);
      resetUserData();
      queryClient.setQueryData(ME_KEY, me);
      return me;
    },
    [queryClient, resetUserData],
  );

  const register = useCallback(
    async (data: RegisterRequest) => {
      const me = await authApi.register(data);
      resetUserData();
      queryClient.setQueryData(ME_KEY, me);
      return me;
    },
    [queryClient, resetUserData],
  );

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      queryClient.setQueryData(ME_KEY, null);
      resetUserData();
    }
  }, [queryClient, resetUserData]);

  const value = useMemo(
    () => ({ user, isLoading, login, register, logout }),
    [user, isLoading, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>');
  return context;
}

/** For pages that only render for a signed-in user (guarded by the router). */
// eslint-disable-next-line react-refresh/only-export-components
export function useCurrentUser(): CurrentUser {
  const { user } = useAuth();
  if (!user) throw new Error('useCurrentUser called without a signed-in user');
  return user;
}
