import type {
  ApprovalStatus,
  DocumentAudience,
  ProcessingStatus,
  Role,
  TicketPriority,
  TicketSort,
  TicketStatus,
} from '@/api/types';

export const TICKET_STATUSES: TicketStatus[] = ['Open', 'InProgress', 'Resolved', 'Closed'];
export const TICKET_PRIORITIES: TicketPriority[] = ['Low', 'Medium', 'High', 'Urgent'];

export const STATUS_META: Record<TicketStatus, { label: string; color: string; hint: string }> = {
  Open: { label: 'Open', color: 'blue', hint: 'Waiting for an agent' },
  InProgress: { label: 'In progress', color: 'gold', hint: 'An agent is working on it' },
  Resolved: { label: 'Resolved', color: 'green', hint: 'A solution was provided' },
  Closed: { label: 'Closed', color: 'default', hint: 'No further changes' },
};

/** `level` drives the signal-bar icon (1-4). Colours also pass 3:1 against white. */
export const PRIORITY_META: Record<TicketPriority, { label: string; color: string; level: number; hint: string }> = {
  Low: { label: 'Low', color: '#6b7c85', level: 1, hint: 'Not time-sensitive' },
  Medium: { label: 'Medium', color: '#2a6fc4', level: 2, hint: 'Normal request' },
  High: { label: 'High', color: '#c2610c', level: 3, hint: 'Blocks important work' },
  Urgent: { label: 'Urgent', color: '#c0262d', level: 4, hint: 'Service is down or at risk' },
};

export const SORT_OPTIONS: { value: TicketSort; label: string }[] = [
  { value: 'newest', label: 'Newest first' },
  { value: 'oldest', label: 'Oldest first' },
  { value: 'priority', label: 'Highest priority' },
  { value: 'status', label: 'Status' },
];

export const ROLE_META: Record<Role, { label: string; color: string }> = {
  Customer: { label: 'Customer', color: 'default' },
  Agent: { label: 'Agent', color: 'cyan' },
  Admin: { label: 'Admin', color: 'purple' },
};

export const APPROVAL_META: Record<ApprovalStatus, { label: string; color: string }> = {
  Draft: { label: 'Draft', color: 'default' },
  Approved: { label: 'Approved', color: 'green' },
  Rejected: { label: 'Rejected', color: 'red' },
};

export const PROCESSING_META: Record<ProcessingStatus, { label: string; color: string }> = {
  Pending: { label: 'Pending', color: 'gold' },
  Indexed: { label: 'Indexed', color: 'blue' },
  Failed: { label: 'Failed', color: 'red' },
};

export const AUDIENCE_META: Record<DocumentAudience, { label: string; hint: string }> = {
  Customer: { label: 'Customers', hint: 'Customers, agents and admins' },
  Agent: { label: 'Agents', hint: 'Agents and admins only' },
  Internal: { label: 'Internal', hint: 'Admins only' },
};

export const MAX_UPLOAD_MB = 5; // keep in sync with "Uploads:MaxFileSizeMb" in appsettings.json
export const ACCEPTED_DOC_TYPES = ['.md', '.txt', '.pdf', '.docx'];

export const TICKET_TITLE_MAX = 200;
export const TICKET_DESCRIPTION_MAX = 4000;
export const COMMENT_MAX = 2000;
export const CHAT_QUESTION_MAX = 1000;
export const RESOLUTION_MAX = 4000;

/** Seeded demo accounts (HelpDeskPro/Data/DbSeeder.cs). Only shown when VITE_SHOW_DEMO_ACCOUNTS=true. */
export const DEMO_ACCOUNTS: { role: Role; email: string; password: string }[] = [
  { role: 'Customer', email: 'customer@helpdesk.com', password: 'Customer@123' },
  { role: 'Agent', email: 'agent@helpdesk.com', password: 'Agent@123' },
  { role: 'Admin', email: 'admin@helpdesk.com', password: 'Admin@123' },
];
