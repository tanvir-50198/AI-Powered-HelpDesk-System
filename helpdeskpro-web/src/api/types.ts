/**
 * Types mirror the C# DTOs in HelpDeskPro/DTOs.
 * The API serialises enums as strings (JsonStringEnumConverter) and uses camelCase.
 */

export type Role = 'Customer' | 'Agent' | 'Admin';

export type TicketStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed';
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export type ApprovalStatus = 'Draft' | 'Approved' | 'Rejected';
export type ProcessingStatus = 'Pending' | 'Indexed' | 'Failed';
export type DocumentAudience = 'Customer' | 'Agent' | 'Internal';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/* ----------------------------- Auth ----------------------------- */

export interface CurrentUser {
  userId: string;
  email: string;
  fullName: string | null;
  role: Role;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

/* ---------------------------- Tickets ---------------------------- */

export interface Comment {
  id: number;
  author: string;
  body: string;
  createdAt: string;
}

export interface Ticket {
  id: number;
  title: string;
  description: string;
  category: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdBy: string;
  assignedAgent: string | null;
  resolutionDetails: string | null;
  createdAt: string;
  updatedAt: string;
  comments: Comment[];
}

export interface HistoryEntry {
  actor: string | null;
  fieldName: string;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
}

export type TicketSort = 'newest' | 'oldest' | 'priority' | 'status';

export interface TicketQuery {
  search?: string;
  status?: TicketStatus;
  priority?: TicketPriority;
  categoryId?: number;
  onlyUnassigned?: boolean;
  sortBy?: TicketSort;
  page?: number;
  pageSize?: number;
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  categoryId: number;
  priority: TicketPriority;
}

/* ---------------------------- Lookups ---------------------------- */

export interface Category {
  id: number;
  name: string;
  description: string | null;
}

export interface AgentOption {
  id: string;
  email: string;
  fullName: string | null;
}

export interface DashboardSummary {
  statusCounts: Record<TicketStatus, number>;
  /** Admin only */
  unassignedTickets: number | null;
  totalUsers: number | null;
  totalConversations: number | null;
}

/* ------------------------------ Chat ------------------------------ */

export interface ChatSource {
  documentId: number;
  title: string;
  section: string | null;
}

export interface Conversation {
  id: number;
  title: string;
  updatedAt: string;
  linkedTicketId: number | null;
}

export interface ChatMessage {
  id: number;
  role: 'User' | 'Assistant';
  content: string;
  isFallback: boolean;
  createdAt: string;
  sources: ChatSource[];
  /** true / false once the customer rated the answer, otherwise null */
  helpful: boolean | null;
}

export interface ConversationDetail extends Conversation {
  messages: ChatMessage[];
}

export interface ChatAnswer {
  conversationId: number;
  messageId: number;
  answer: string;
  isFallback: boolean;
  canCreateTicket: boolean;
  sources: ChatSource[];
}

export interface CreateTicketFromChatRequest extends CreateTicketRequest {
  confirmed: boolean;
}

/* ------------------------------ Admin ------------------------------ */

export interface AdminUser {
  id: string;
  email: string;
  fullName: string | null;
  role: Role;
  createdAt: string;
}

export interface KnowledgeDocument {
  id: number;
  title: string;
  category: string | null;
  sourceType: string | null;
  audience: DocumentAudience;
  approvalStatus: ApprovalStatus;
  processingStatus: ProcessingStatus;
  processingError: string | null;
  version: number;
  updatedAt: string;
}

export interface UploadDocumentInput {
  file: File;
  title?: string;
  category?: string;
  audience: DocumentAudience;
}

export interface AdminSummary {
  totalTickets: number;
  statusDistribution: { status: string; count: number }[];
  last7Days: { day: string; created: number; resolved: number }[];
  demoSlaHours: number;
  overdueTickets: number;
  totalConversations: number;
  escalatedConversations: number;
  escalationRatePercent: number;
  helpfulFeedback: number;
  notHelpfulFeedback: number;
  helpfulRatePercent: number;
  fallbackAnswers: number;
  unansweredQuestions: { conversationId: number; question: string; askedAt: string }[];
  approvedDocuments: number;
  failedDocuments: number;
}
