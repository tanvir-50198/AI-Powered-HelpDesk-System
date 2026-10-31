import { request } from './client';
import type {
  AdminSummary,
  AdminUser,
  AgentOption,
  Category,
  ChatAnswer,
  Conversation,
  ConversationDetail,
  CreateTicketFromChatRequest,
  CreateTicketRequest,
  CurrentUser,
  DashboardSummary,
  HistoryEntry,
  KnowledgeDocument,
  LoginRequest,
  PagedResult,
  RegisterRequest,
  Role,
  Ticket,
  TicketPriority,
  TicketQuery,
  TicketStatus,
  UploadDocumentInput,
} from './types';

/* Auth ------------------------------------------------------------ */
export const authApi = {
  /** Returns null when nobody is signed in. */
  async me(): Promise<CurrentUser | null> {
    try {
      return await request<CurrentUser>('/auth/me', { skipUnauthorizedHandler: true });
    } catch (error) {
      if ((error as { status?: number }).status === 401) return null;
      throw error;
    }
  },
  login: (data: LoginRequest) =>
    request<CurrentUser>('/auth/login', { method: 'POST', json: data, skipUnauthorizedHandler: true }),
  register: (data: RegisterRequest) =>
    request<CurrentUser>('/auth/register', { method: 'POST', json: data, skipUnauthorizedHandler: true }),
  logout: () => request('/auth/logout', { method: 'POST', skipUnauthorizedHandler: true }),
};

/* Lookups / dashboard ---------------------------------------------- */
export const lookupApi = {
  categories: () => request<Category[]>('/lookups/categories'),
  agents: () => request<AgentOption[]>('/admin/agents'),
  dashboard: () => request<DashboardSummary>('/dashboard/summary'),
};

/* Tickets ---------------------------------------------------------- */
export const ticketApi = {
  list: (query: TicketQuery, signal?: AbortSignal) =>
    request<PagedResult<Ticket>>('/tickets', { query: { ...query }, signal }),
  get: (id: number) => request<Ticket>(`/tickets/${id}`),
  history: (id: number) => request<HistoryEntry[]>(`/tickets/${id}/history`),
  create: (data: CreateTicketRequest) => request<Ticket>('/tickets', { method: 'POST', json: data }),
  addComment: (id: number, body: string) =>
    request(`/tickets/${id}/comments`, { method: 'POST', json: { body } }),
  updateStatus: (id: number, status: TicketStatus) =>
    request(`/tickets/${id}/status`, { method: 'PATCH', json: { status } }),
  updatePriority: (id: number, priority: TicketPriority) =>
    request(`/tickets/${id}/priority`, { method: 'PATCH', json: { priority } }),
  saveResolution: (id: number, resolutionDetails: string) =>
    request(`/tickets/${id}/resolution`, { method: 'PUT', json: { resolutionDetails } }),
  assign: (id: number, agentUserId: string) =>
    request(`/tickets/${id}/assign`, { method: 'POST', json: { agentUserId } }),
};

/* Chat ------------------------------------------------------------- */
export const chatApi = {
  conversations: () => request<Conversation[]>('/chat/conversations'),
  conversation: (id: number) => request<ConversationDetail>(`/chat/conversations/${id}`),
  start: () => request<Conversation>('/chat/conversations', { method: 'POST' }),
  send: (id: number, question: string) =>
    request<ChatAnswer>(`/chat/conversations/${id}/messages`, { method: 'POST', json: { question } }),
  feedback: (messageId: number, isHelpful: boolean, comment?: string) =>
    request(`/chat/messages/${messageId}/feedback`, { method: 'POST', json: { isHelpful, comment } }),
  createTicket: (id: number, data: CreateTicketFromChatRequest) =>
    request<Ticket>(`/chat/conversations/${id}/create-ticket`, { method: 'POST', json: data }),
};

/* Admin ------------------------------------------------------------ */
export const adminApi = {
  summary: () => request<AdminSummary>('/admin/reports/summary'),
  users: () => request<AdminUser[]>('/admin/users'),
  changeRole: (userId: string, role: Role) =>
    request(`/admin/users/${userId}/role`, { method: 'PATCH', json: { role } }),
  documents: () => request<KnowledgeDocument[]>('/admin/knowledge-documents'),
  upload: (input: UploadDocumentInput) => {
    const form = new FormData();
    form.append('File', input.file);
    if (input.title) form.append('Title', input.title);
    if (input.category) form.append('Category', input.category);
    form.append('Audience', input.audience);
    return request<KnowledgeDocument>('/admin/knowledge-documents', { method: 'POST', form });
  },
  approve: (id: number) => request(`/admin/knowledge-documents/${id}/approve`, { method: 'POST' }),
  reject: (id: number) => request(`/admin/knowledge-documents/${id}/reject`, { method: 'POST' }),
  reindex: (id: number) => request(`/admin/knowledge-documents/${id}/reindex`, { method: 'POST' }),
  remove: (id: number) => request(`/admin/knowledge-documents/${id}`, { method: 'DELETE' }),
  importFolder: () =>
    request<{ imported: number }>('/admin/knowledge-documents/import-folder', { method: 'POST' }),
};
