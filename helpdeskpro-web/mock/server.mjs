// In-memory mock of the HelpDeskPro API (incl. the endpoints in /backend-additions).
// Run: npm run dev:mock   (starts this on :5080 and Vite on :5173)
// Not for production. Data resets on restart.
import http from 'node:http';
import { randomUUID } from 'node:crypto';

const PORT = Number(process.env.PORT || 5080);
const ago = (h) => new Date(Date.now() - h * 3600_000).toISOString();

const users = [
  { id: 'u-c', email: 'customer@helpdesk.com', fullName: 'Chloe Customer', role: 'Customer', password: 'Customer@123', createdAt: ago(900) },
  { id: 'u-a', email: 'agent@helpdesk.com', fullName: 'Aaron Agent', role: 'Agent', password: 'Agent@123', createdAt: ago(800) },
  { id: 'u-a2', email: 'maria@helpdesk.com', fullName: 'Maria Lopez', role: 'Agent', password: 'Agent@123', createdAt: ago(700) },
  { id: 'u-x', email: 'admin@helpdesk.com', fullName: 'Ada Admin', role: 'Admin', password: 'Admin@123', createdAt: ago(1000) },
];
const categories = ['Account & Login', 'Billing', 'Technical Issue', 'Feature Request', 'General Question'].map((name, i) => ({ id: i + 1, name, description: null }));

let ticketSeq = 1040;
const tickets = [];
const history = {};
function addTicket(t) {
  const id = ++ticketSeq;
  const row = { id, description: 'Details of the problem.', status: 'Open', priority: 'Medium', assignedAgent: null, resolutionDetails: null, comments: [], createdAt: ago(5), updatedAt: ago(5), createdBy: 'customer@helpdesk.com', ...t };
  tickets.push(row);
  history[id] = [{ actor: row.createdBy, fieldName: 'Ticket', oldValue: null, newValue: 'Created', createdAt: row.createdAt }];
  return row;
}
addTicket({ title: 'Can’t sign in after changing my password', category: 'Account & Login', priority: 'Urgent', status: 'InProgress', assignedAgent: 'agent@helpdesk.com', createdAt: ago(30), updatedAt: ago(2), description: 'After I changed my password I get “invalid login” on every device.\nI tried the reset link twice.', comments: [{ id: 1, author: 'agent@helpdesk.com', body: 'Thanks for reporting. Could you tell me which browser you use?', createdAt: ago(3) }] });
addTicket({ title: 'Invoice shows the wrong billing address', category: 'Billing', priority: 'Medium', status: 'Open', createdAt: ago(8), updatedAt: ago(8) });
addTicket({ title: 'Export my tickets as CSV', category: 'Feature Request', priority: 'Low', status: 'Resolved', assignedAgent: 'agent@helpdesk.com', resolutionDetails: 'CSV export is on the roadmap. Use the print view in the meantime.', createdAt: ago(100), updatedAt: ago(60) });
addTicket({ title: 'App crashes when uploading a screenshot', category: 'Technical Issue', priority: 'High', status: 'Open', createdBy: 'sam@example.com', createdAt: ago(2), updatedAt: ago(2) });
addTicket({ title: 'Old request about notifications', category: 'General Question', priority: 'Low', status: 'Closed', assignedAgent: 'maria@helpdesk.com', resolutionDetails: 'Answered by phone.', createdAt: ago(300), updatedAt: ago(250) });
for (let i = 0; i < 9; i++) addTicket({ title: `Sample ticket ${i + 1}`, category: categories[i % 5].name, priority: ['Low', 'Medium', 'High', 'Urgent'][i % 4], createdAt: ago(10 + i * 7), updatedAt: ago(10 + i * 7) });

const docs = [
  { id: 1, title: 'Password reset guide', category: 'faq', sourceType: 'md', audience: 'Customer', approvalStatus: 'Approved', processingStatus: 'Indexed', processingError: null, version: 1, updatedAt: ago(40) },
  { id: 2, title: 'Ticket lifecycle', category: 'policies', sourceType: 'md', audience: 'Customer', approvalStatus: 'Draft', processingStatus: 'Indexed', processingError: null, version: 2, updatedAt: ago(10) },
  { id: 3, title: 'Escalation playbook', category: 'internal', sourceType: 'pdf', audience: 'Internal', approvalStatus: 'Draft', processingStatus: 'Failed', processingError: 'No extractable text in PDF.', version: 1, updatedAt: ago(5) },
];

let convSeq = 1;
const conversations = []; // { id, userId, title, linkedTicketId, updatedAt, messages }
let msgSeq = 1;

const sessions = new Map();
const json = (res, status, body, headers = {}) => {
  res.writeHead(status, { 'Content-Type': 'application/json', ...headers });
  res.end(body === undefined ? '' : JSON.stringify(body));
};
const readBody = (req) => new Promise((resolve) => { let d = ''; req.on('data', (c) => (d += c)); req.on('end', () => { try { resolve(d ? JSON.parse(d) : {}); } catch { resolve({}); } }); });
const problem = (res, status, detail) => json(res, status, { title: detail, detail, status });
const publicUser = (u) => ({ userId: u.id, email: u.email, fullName: u.fullName, role: u.role });
const PRI = { Low: 1, Medium: 2, High: 3, Urgent: 4 };

function visible(user) {
  if (user.role === 'Admin') return tickets;
  if (user.role === 'Agent') return tickets.filter((t) => t.assignedAgent === user.email);
  return tickets.filter((t) => t.createdBy === user.email);
}
const log = (id, actor, fieldName, oldValue, newValue) => history[id].push({ actor, fieldName, oldValue, newValue, createdAt: new Date().toISOString() });
const touch = (t) => (t.updatedAt = new Date().toISOString());

function answerFor(q) {
  const l = q.toLowerCase();
  if (l.includes('password')) return { answer: 'To reset your password, open the sign-in page and choose “Forgot password”. You will get an email with a link that works for 60 minutes [1]. If the email does not arrive, check your spam folder [1].', isFallback: false, sources: [{ documentId: 1, title: 'Password reset guide', section: 'Reset by email' }] };
  if (l.includes('status')) return { answer: 'A ticket moves from Open to In progress when an agent starts work, then to Resolved once a solution is written down, and finally Closed [1].', isFallback: false, sources: [{ documentId: 2, title: 'Ticket lifecycle', section: 'Statuses' }] };
  return { answer: 'I could not find this in the approved help articles. I can hand this chat to a support agent.', isFallback: true, sources: [] };
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, 'http://x');
  const path = url.pathname;
  const m = req.method;
  await new Promise((r) => setTimeout(r, 120)); // feel like a network

  const sid = /sid=([^;]+)/.exec(req.headers.cookie || '')?.[1];
  const user = sid ? sessions.get(sid) : undefined;

  if (path === '/api/auth/login' && m === 'POST') {
    const b = await readBody(req);
    const u = users.find((x) => x.email.toLowerCase() === String(b.email).toLowerCase() && x.password === b.password);
    if (!u) return problem(res, 401, 'Invalid email or password.');
    const id = randomUUID();
    sessions.set(id, u);
    return json(res, 200, publicUser(u), { 'Set-Cookie': `sid=${id}; Path=/; HttpOnly; SameSite=Lax` });
  }
  if (path === '/api/auth/register' && m === 'POST') {
    const b = await readBody(req);
    if (users.some((x) => x.email.toLowerCase() === String(b.email).toLowerCase())) return problem(res, 400, `Email '${b.email}' is already taken.`);
    const u = { id: randomUUID(), email: b.email, fullName: b.fullName, role: 'Customer', password: b.password, createdAt: new Date().toISOString() };
    users.push(u);
    const id = randomUUID();
    sessions.set(id, u);
    return json(res, 200, publicUser(u), { 'Set-Cookie': `sid=${id}; Path=/; HttpOnly; SameSite=Lax` });
  }
  if (path === '/api/auth/logout' && m === 'POST') {
    sessions.delete(sid);
    return json(res, 204, undefined, { 'Set-Cookie': 'sid=; Path=/; Max-Age=0' });
  }
  if (path === '/api/auth/me') return user ? json(res, 200, publicUser(user)) : json(res, 401);

  if (!user) return json(res, 401);
  const is = (role) => user.role === role;

  if (path === '/api/lookups/categories') return json(res, 200, categories);
  if (path === '/api/admin/agents') return is('Admin') ? json(res, 200, users.filter((u) => u.role === 'Agent').map((u) => ({ id: u.id, email: u.email, fullName: u.fullName }))) : json(res, 403);

  if (path === '/api/dashboard/summary') {
    const own = visible(user);
    const statusCounts = { Open: 0, InProgress: 0, Resolved: 0, Closed: 0 };
    own.forEach((t) => statusCounts[t.status]++);
    return json(res, 200, { statusCounts, unassignedTickets: is('Admin') ? tickets.filter((t) => !t.assignedAgent).length : null, totalUsers: is('Admin') ? users.length : null, totalConversations: is('Admin') ? conversations.length + 12 : null });
  }

  if (path === '/api/tickets' && m === 'GET') {
    const q = url.searchParams;
    let rows = [...visible(user)];
    const s = q.get('search')?.toLowerCase();
    if (s) rows = rows.filter((t) => (t.title + t.description).toLowerCase().includes(s));
    if (q.get('status')) rows = rows.filter((t) => t.status === q.get('status'));
    if (q.get('priority')) rows = rows.filter((t) => t.priority === q.get('priority'));
    if (q.get('categoryId')) rows = rows.filter((t) => t.category === categories.find((c) => c.id === Number(q.get('categoryId')))?.name);
    if (q.get('onlyUnassigned') === 'true') rows = rows.filter((t) => !t.assignedAgent);
    const sort = q.get('sortBy') || 'newest';
    rows.sort((a, b) => sort === 'oldest' ? a.createdAt.localeCompare(b.createdAt) : sort === 'priority' ? PRI[b.priority] - PRI[a.priority] : sort === 'status' ? a.status.localeCompare(b.status) : b.createdAt.localeCompare(a.createdAt));
    const page = Number(q.get('page') || 1), pageSize = Math.min(Number(q.get('pageSize') || 10), 50);
    return json(res, 200, { items: rows.slice((page - 1) * pageSize, page * pageSize), page, pageSize, totalCount: rows.length, totalPages: Math.ceil(rows.length / pageSize) });
  }
  if (path === '/api/tickets' && m === 'POST') {
    const b = await readBody(req);
    const cat = categories.find((c) => c.id === b.categoryId);
    if (!b.title || !cat) return problem(res, 400, 'Title and a valid category are required.');
    const t = addTicket({ title: b.title, description: b.description, priority: b.priority, category: cat.name, createdBy: user.email, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() });
    return json(res, 201, t);
  }

  const tm = /^\/api\/tickets\/(\d+)(?:\/(\w+))?$/.exec(path);
  if (tm) {
    const t = visible(user).find((x) => x.id === Number(tm[1]));
    if (!t) return problem(res, 404, 'Ticket not found.');
    const sub = tm[2];
    if (!sub && m === 'GET') return json(res, 200, t);
    if (sub === 'history') return json(res, 200, history[t.id]);
    const b = m === 'GET' ? {} : await readBody(req);
    if (sub === 'comments') {
      if (t.status === 'Closed') return problem(res, 409, 'Closed tickets cannot be commented on.');
      t.comments.push({ id: Date.now(), author: user.email, body: b.body, createdAt: new Date().toISOString() });
      log(t.id, user.email, 'Comment', null, 'Added'); touch(t);
      return json(res, 204);
    }
    if (is('Customer')) return json(res, 403);
    if (sub === 'status') {
      if (['Resolved', 'Closed'].includes(b.status) && !t.resolutionDetails) return problem(res, 400, 'Resolution details are required before resolving or closing a ticket.');
      if (t.status === 'Closed' && !is('Admin')) return problem(res, 409, 'Only an admin can re-open a closed ticket.');
      log(t.id, user.email, 'Status', t.status, b.status); t.status = b.status; touch(t);
      return json(res, 204);
    }
    if (sub === 'priority') { log(t.id, user.email, 'Priority', t.priority, b.priority); t.priority = b.priority; touch(t); return json(res, 204); }
    if (sub === 'resolution') { if (!b.resolutionDetails?.trim()) return problem(res, 400, 'Resolution details are required.'); t.resolutionDetails = b.resolutionDetails; log(t.id, user.email, 'Resolution', null, 'Saved'); touch(t); return json(res, 204); }
    if (sub === 'assign' && is('Admin')) {
      const a = users.find((u) => u.id === b.agentUserId && u.role === 'Agent');
      if (!a) return problem(res, 400, 'Pick a valid agent.');
      log(t.id, user.email, 'Assigned agent', t.assignedAgent, a.email); t.assignedAgent = a.email;
      if (t.status === 'Open') { log(t.id, user.email, 'Status', 'Open', 'InProgress'); t.status = 'InProgress'; }
      touch(t); return json(res, 204);
    }
  }

  /* Chat */
  if (path === '/api/chat/conversations' && m === 'GET') return json(res, 200, conversations.filter((c) => c.userId === user.id).map(({ id, title, updatedAt, linkedTicketId }) => ({ id, title, updatedAt, linkedTicketId })).reverse());
  if (path === '/api/chat/conversations' && m === 'POST') {
    const c = { id: convSeq++, userId: user.id, title: 'New conversation', linkedTicketId: null, updatedAt: new Date().toISOString(), messages: [] };
    conversations.push(c);
    return json(res, 200, { id: c.id, title: c.title, updatedAt: c.updatedAt, linkedTicketId: null });
  }
  const cm = /^\/api\/chat\/conversations\/(\d+)(?:\/([\w-]+))?$/.exec(path);
  if (cm) {
    const c = conversations.find((x) => x.id === Number(cm[1]) && x.userId === user.id);
    if (!c) return problem(res, 404, 'Conversation not found.');
    if (!cm[2] && m === 'GET') return json(res, 200, { id: c.id, title: c.title, updatedAt: c.updatedAt, linkedTicketId: c.linkedTicketId, messages: c.messages });
    if (cm[2] === 'messages') {
      const b = await readBody(req);
      if (c.title === 'New conversation') c.title = b.question.slice(0, 60);
      c.messages.push({ id: msgSeq++, role: 'User', content: b.question, isFallback: false, createdAt: new Date().toISOString(), sources: [], helpful: null });
      await new Promise((r) => setTimeout(r, 900));
      const a = answerFor(b.question);
      const msg = { id: msgSeq++, role: 'Assistant', content: a.answer, isFallback: a.isFallback, createdAt: new Date().toISOString(), sources: a.sources, helpful: null };
      c.messages.push(msg); c.updatedAt = msg.createdAt;
      return json(res, 200, { conversationId: c.id, messageId: msg.id, answer: a.answer, isFallback: a.isFallback, canCreateTicket: a.isFallback, sources: a.sources });
    }
    if (cm[2] === 'create-ticket') {
      const b = await readBody(req);
      if (!b.confirmed) return problem(res, 400, 'Please confirm the summary first.');
      const cat = categories.find((x) => x.id === b.categoryId);
      const t = addTicket({ title: b.title, description: b.description, priority: b.priority, category: cat?.name ?? 'General Question', createdBy: user.email, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() });
      c.linkedTicketId = t.id;
      return json(res, 201, t);
    }
  }
  const fm = /^\/api\/chat\/messages\/(\d+)\/feedback$/.exec(path);
  if (fm && m === 'POST') {
    const b = await readBody(req);
    for (const c of conversations) for (const x of c.messages) if (x.id === Number(fm[1])) x.helpful = b.isHelpful;
    return json(res, 204);
  }

  /* Admin */
  if (!is('Admin')) return json(res, 403);
  if (path === '/api/admin/users' && m === 'GET') return json(res, 200, users.map((u) => ({ id: u.id, email: u.email, fullName: u.fullName, role: u.role, createdAt: u.createdAt })));
  const um = /^\/api\/admin\/users\/([\w-]+)\/role$/.exec(path);
  if (um && m === 'PATCH') { const b = await readBody(req); const u = users.find((x) => x.id === um[1]); if (!u) return problem(res, 404, 'User not found.'); u.role = b.role; return json(res, 204); }
  if (path === '/api/admin/knowledge-documents' && m === 'GET') return json(res, 200, docs);
  if (path === '/api/admin/knowledge-documents' && m === 'POST') {
    const d = { id: docs.length + 10, title: 'Uploaded document', category: null, sourceType: 'md', audience: 'Customer', approvalStatus: 'Draft', processingStatus: 'Indexed', processingError: null, version: 1, updatedAt: new Date().toISOString() };
    docs.push(d); return json(res, 201, d);
  }
  if (path === '/api/admin/knowledge-documents/import-folder' && m === 'POST') return json(res, 200, { imported: 0 });
  const dm = /^\/api\/admin\/knowledge-documents\/(\d+)(?:\/(\w+))?$/.exec(path);
  if (dm) {
    const d = docs.find((x) => x.id === Number(dm[1]));
    if (!d) return problem(res, 404, 'Document not found.');
    if (m === 'DELETE') { docs.splice(docs.indexOf(d), 1); return json(res, 204); }
    if (dm[2] === 'approve') { d.approvalStatus = 'Approved'; return json(res, 204); }
    if (dm[2] === 'reject') { d.approvalStatus = 'Rejected'; return json(res, 204); }
    if (dm[2] === 'reindex') { d.processingStatus = 'Indexed'; d.processingError = null; return json(res, 204); }
  }
  if (path === '/api/admin/reports/summary') {
    return json(res, 200, {
      totalTickets: tickets.length,
      statusDistribution: ['Open', 'InProgress', 'Resolved', 'Closed'].map((s) => ({ status: s, count: tickets.filter((t) => t.status === s).length })),
      last7Days: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map((day, i) => ({ day, created: [3, 5, 2, 6, 4, 1, 2][i], resolved: [1, 2, 3, 4, 2, 1, 0][i] })),
      demoSlaHours: 48, overdueTickets: 3, totalConversations: 24, escalatedConversations: 5, escalationRatePercent: 20.8,
      helpfulFeedback: 17, notHelpfulFeedback: 4, helpfulRatePercent: 81, fallbackAnswers: 6,
      unansweredQuestions: [{ conversationId: 3, question: 'Can I change the email on my account?', askedAt: ago(3) }, { conversationId: 5, question: 'Do you support single sign-on with Okta?', askedAt: ago(20) }],
      approvedDocuments: docs.filter((d) => d.approvalStatus === 'Approved').length, failedDocuments: docs.filter((d) => d.processingStatus === 'Failed').length,
    });
  }
  return problem(res, 404, 'Not found');
});

server.listen(PORT, () => console.log(`Mock HelpDeskPro API on http://localhost:${PORT}`));
