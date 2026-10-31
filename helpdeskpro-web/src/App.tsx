import { lazy, Suspense } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ApiError } from '@/api/client';
import { AuthProvider } from '@/auth/AuthContext';
import { FullPageSpinner, PublicOnly, RequireAuth, RequireRole } from '@/auth/guards';
import { ErrorBoundary } from '@/components/ErrorBoundary';
import AppLayout from '@/layouts/AppLayout';
import { AppThemeProvider } from '@/theme';
import LoginPage from '@/features/auth/LoginPage';
import RegisterPage from '@/features/auth/RegisterPage';
import DashboardPage from '@/features/dashboard/DashboardPage';
import { ForbiddenPage, NotFoundPage } from '@/features/errors/ErrorPages';

// Heavier / role-specific screens are split into their own chunks.
const TicketsPage = lazy(() => import('@/features/tickets/TicketsPage'));
const TicketDetailPage = lazy(() => import('@/features/tickets/TicketDetailPage'));
const CreateTicketPage = lazy(() => import('@/features/tickets/CreateTicketPage'));
const ChatPage = lazy(() => import('@/features/chat/ChatPage'));
const UsersPage = lazy(() => import('@/features/admin/UsersPage'));
const KnowledgePage = lazy(() => import('@/features/admin/KnowledgePage'));
const ReportsPage = lazy(() => import('@/features/admin/ReportsPage'));

// '/' -> '' ; '/app/' -> '/app'
const BASENAME = import.meta.env.BASE_URL.replace(/\/$/, '');

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 15_000,
      refetchOnWindowFocus: true,
      // never retry client errors (401/403/404/429) – they will not fix themselves
      retry: (count, error) => !(error instanceof ApiError && error.status >= 400 && error.status < 500) && count < 2,
    },
  },
});

export default function App() {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <AppThemeProvider>
          <BrowserRouter basename={BASENAME} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
            <AuthProvider>
              <Suspense fallback={<FullPageSpinner />}>
                <Routes>
                  <Route element={<PublicOnly />}>
                    <Route path="/login" element={<LoginPage />} />
                    <Route path="/register" element={<RegisterPage />} />
                  </Route>

                  <Route element={<RequireAuth />}>
                    <Route element={<AppLayout />}>
                      <Route index element={<Navigate to="/dashboard" replace />} />
                      <Route path="/dashboard" element={<DashboardPage />} />
                      <Route path="/tickets" element={<TicketsPage />} />
                      <Route path="/tickets/:id" element={<TicketDetailPage />} />

                      <Route element={<RequireRole roles={['Customer']} />}>
                        <Route path="/tickets/new" element={<CreateTicketPage />} />
                        <Route path="/assistant/:conversationId?" element={<ChatPage />} />
                      </Route>

                      <Route element={<RequireRole roles={['Admin']} />}>
                        <Route path="/admin/users" element={<UsersPage />} />
                        <Route path="/admin/knowledge" element={<KnowledgePage />} />
                        <Route path="/admin/reports" element={<ReportsPage />} />
                      </Route>

                      <Route path="/forbidden" element={<ForbiddenPage />} />
                      <Route path="*" element={<NotFoundPage />} />
                    </Route>
                  </Route>
                </Routes>
              </Suspense>
            </AuthProvider>
          </BrowserRouter>
        </AppThemeProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  );
}
