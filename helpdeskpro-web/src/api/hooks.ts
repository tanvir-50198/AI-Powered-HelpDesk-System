import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationOptions,
} from '@tanstack/react-query';
import { App } from 'antd';
import { adminApi, chatApi, lookupApi, ticketApi } from './endpoints';
import type { Role, TicketPriority, TicketQuery, TicketStatus, UploadDocumentInput } from './types';

/* ------------------------------ Query keys ------------------------------ */
export const qk = {
  categories: ['categories'] as const,
  agents: ['agents'] as const,
  dashboard: ['dashboard'] as const,
  tickets: ['tickets'] as const,
  ticketList: (query: TicketQuery) => ['tickets', 'list', query] as const,
  ticket: (id: number) => ['tickets', 'detail', id] as const,
  ticketHistory: (id: number) => ['tickets', 'history', id] as const,
  conversations: ['chat', 'conversations'] as const,
  conversation: (id: number) => ['chat', 'conversation', id] as const,
  users: ['admin', 'users'] as const,
  documents: ['admin', 'documents'] as const,
  summary: ['admin', 'summary'] as const,
};

/* --------------------------- Mutation helper ---------------------------- */
type AppMutationOptions<TData, TVars> = UseMutationOptions<TData, Error, TVars> & {
  /** Toast shown on success. */
  successMessage?: string | ((data: TData, vars: TVars) => string);
  /** Skip the default error toast (the caller renders the error itself). */
  silentError?: boolean;
};

/** useMutation + consistent success / error toasts. */
export function useAppMutation<TData = unknown, TVars = void>(options: AppMutationOptions<TData, TVars>) {
  const { message } = App.useApp();
  const { successMessage, silentError, onSuccess, onError, ...rest } = options;

  return useMutation<TData, Error, TVars>({
    ...rest,
    onSuccess: (data, vars, ctx, meta) => {
      if (successMessage) {
        message.success(typeof successMessage === 'function' ? successMessage(data, vars) : successMessage);
      }
      return onSuccess?.(data, vars, ctx, meta);
    },
    onError: (error, vars, ctx, meta) => {
      if (!silentError) message.error(error.message);
      return onError?.(error, vars, ctx, meta);
    },
  });
}

/* -------------------------------- Lookups ------------------------------- */
export const useCategories = () =>
  useQuery({ queryKey: qk.categories, queryFn: lookupApi.categories, staleTime: 10 * 60_000 });

export const useAgents = (enabled = true) =>
  useQuery({ queryKey: qk.agents, queryFn: lookupApi.agents, staleTime: 5 * 60_000, enabled });

export const useDashboard = () => useQuery({ queryKey: qk.dashboard, queryFn: lookupApi.dashboard });

/* -------------------------------- Tickets ------------------------------- */
export const useTickets = (query: TicketQuery) =>
  useQuery({
    queryKey: qk.ticketList(query),
    queryFn: ({ signal }) => ticketApi.list(query, signal),
    placeholderData: keepPreviousData,
  });

export const useTicket = (id: number) =>
  useQuery({ queryKey: qk.ticket(id), queryFn: () => ticketApi.get(id), enabled: Number.isFinite(id) });

export const useTicketHistory = (id: number) =>
  useQuery({ queryKey: qk.ticketHistory(id), queryFn: () => ticketApi.history(id), enabled: Number.isFinite(id) });

/** After any change to a ticket, everything that shows tickets is stale. */
function useRefreshTickets() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: qk.tickets });
    void queryClient.invalidateQueries({ queryKey: qk.dashboard });
  };
}

export function useCreateTicket() {
  const refresh = useRefreshTickets();
  return useAppMutation({
    mutationFn: ticketApi.create,
    onSuccess: refresh,
    successMessage: (ticket) => `Ticket #${ticket.id} created.`,
  });
}

export function useAddComment(ticketId: number) {
  const refresh = useRefreshTickets();
  return useAppMutation({
    mutationFn: (body: string) => ticketApi.addComment(ticketId, body),
    onSuccess: refresh,
  });
}

export function useUpdateStatus(ticketId: number) {
  const refresh = useRefreshTickets();
  return useAppMutation({
    mutationFn: (status: TicketStatus) => ticketApi.updateStatus(ticketId, status),
    onSuccess: refresh,
    successMessage: 'Status updated.',
  });
}

export function useUpdatePriority(ticketId: number) {
  const refresh = useRefreshTickets();
  return useAppMutation({
    mutationFn: (priority: TicketPriority) => ticketApi.updatePriority(ticketId, priority),
    onSuccess: refresh,
    successMessage: 'Priority updated.',
  });
}

export function useSaveResolution(ticketId: number) {
  const refresh = useRefreshTickets();
  return useAppMutation({
    mutationFn: (details: string) => ticketApi.saveResolution(ticketId, details),
    onSuccess: refresh,
    successMessage: 'Resolution saved.',
  });
}

export function useAssignAgent() {
  const refresh = useRefreshTickets();
  return useAppMutation({
    mutationFn: ({ ticketId, agentUserId }: { ticketId: number; agentUserId: string }) =>
      ticketApi.assign(ticketId, agentUserId),
    onSuccess: refresh,
    successMessage: 'Ticket assigned.',
  });
}

/* --------------------------------- Chat --------------------------------- */
export const useConversations = () => useQuery({ queryKey: qk.conversations, queryFn: chatApi.conversations });

export const useConversation = (id: number | undefined) =>
  useQuery({
    queryKey: qk.conversation(id ?? 0),
    queryFn: () => chatApi.conversation(id as number),
    enabled: id !== undefined,
    // Keeps the optimistic message from being overwritten by a refetch that races the POST.
    staleTime: 30_000,
  });

/* --------------------------------- Admin -------------------------------- */
export const useUsers = () => useQuery({ queryKey: qk.users, queryFn: adminApi.users });

export function useChangeRole() {
  const queryClient = useQueryClient();
  return useAppMutation({
    mutationFn: ({ userId, role }: { userId: string; role: Role }) => adminApi.changeRole(userId, role),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: qk.users });
      void queryClient.invalidateQueries({ queryKey: qk.agents });
    },
    successMessage: 'Role updated.',
  });
}

export const useDocuments = () => useQuery({ queryKey: qk.documents, queryFn: adminApi.documents });

export function useDocumentActions() {
  const queryClient = useQueryClient();
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: qk.documents });
    void queryClient.invalidateQueries({ queryKey: qk.summary });
  };

  return {
    upload: useAppMutation({
      mutationFn: (input: UploadDocumentInput) => adminApi.upload(input),
      onSuccess: refresh,
      silentError: true,
    }),
    approve: useAppMutation({
      mutationFn: adminApi.approve,
      onSuccess: refresh,
      successMessage: 'Approved. The assistant can use this document now.',
    }),
    reject: useAppMutation({
      mutationFn: adminApi.reject,
      onSuccess: refresh,
      successMessage: 'Rejected. The assistant will not use this document.',
    }),
    reindex: useAppMutation({
      mutationFn: adminApi.reindex,
      onSuccess: refresh,
      successMessage: 'Document re-indexed.',
    }),
    remove: useAppMutation({
      mutationFn: adminApi.remove,
      onSuccess: refresh,
      successMessage: 'Document deleted.',
    }),
    importFolder: useAppMutation({
      mutationFn: adminApi.importFolder,
      onSuccess: refresh,
      successMessage: (r) =>
        r.imported === 0 ? 'Nothing new to import.' : `${r.imported} new document${r.imported === 1 ? '' : 's'} imported.`,
    }),
  };
}

export const useAdminSummary = () => useQuery({ queryKey: qk.summary, queryFn: adminApi.summary });
