import { useMemo, useRef, useState } from 'react';
import { Alert, App, Button, Card, Input, Select, Skeleton, Space, Tag, Timeline, Typography } from 'antd';
import type { TextAreaRef } from 'antd/es/input/TextArea';
import {
  useAddComment,
  useAgents,
  useAssignAgent,
  useSaveResolution,
  useTicketHistory,
  useUpdatePriority,
  useUpdateStatus,
} from '@/api/hooks';
import type { HistoryEntry, Ticket, TicketPriority, TicketStatus } from '@/api/types';
import { EmptyState } from '@/components/States';
import { TimeText } from '@/components/TimeText';
import { UserAvatar } from '@/components/UserAvatar';
import {
  COMMENT_MAX,
  PRIORITY_META,
  RESOLUTION_MAX,
  STATUS_META,
  TICKET_PRIORITIES,
  TICKET_STATUSES,
} from '@/domain';

/* ------------------------------ Comments ------------------------------ */

export function CommentsCard({ ticket }: { ticket: Ticket }) {
  const [body, setBody] = useState('');
  const add = useAddComment(ticket.id);
  const closed = ticket.status === 'Closed';

  const comments = useMemo(
    () => [...ticket.comments].sort((a, b) => a.createdAt.localeCompare(b.createdAt)),
    [ticket.comments],
  );

  const submit = () => {
    const text = body.trim();
    if (!text || add.isPending) return;
    add.mutate(text, { onSuccess: () => setBody('') });
  };

  return (
    <Card title={`Comments (${comments.length})`}>
      {comments.length === 0 ? (
        <EmptyState title="No comments yet" description={closed ? undefined : 'Start the conversation below.'} />
      ) : (
        <div role="list">
          {comments.map((comment) => (
            <div className="comment" role="listitem" key={comment.id}>
              <UserAvatar name={comment.author} />
              <div className="comment__body">
                <div className="comment__meta">
                  <Typography.Text strong>{comment.author}</Typography.Text>
                  {comment.author === ticket.createdBy && (
                    <Tag bordered={false} style={{ marginInlineEnd: 0 }}>
                      Requester
                    </Tag>
                  )}
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    <TimeText value={comment.createdAt} />
                  </Typography.Text>
                </div>
                <div className="pre-wrap">{comment.body}</div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div style={{ marginTop: 16 }}>
        {closed ? (
          <Alert type="info" showIcon message="This ticket is closed, so new comments can’t be added." />
        ) : (
          <>
            <Input.TextArea
              aria-label="Write a comment"
              rows={3}
              value={body}
              maxLength={COMMENT_MAX}
              showCount
              placeholder="Write a comment…"
              onChange={(e) => setBody(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                  e.preventDefault();
                  submit();
                }
              }}
            />
            <div style={{ marginTop: 26, display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12 }}>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Press Ctrl + Enter to send
              </Typography.Text>
              <Button type="primary" onClick={submit} loading={add.isPending} disabled={!body.trim()}>
                Add comment
              </Button>
            </div>
          </>
        )}
      </div>
    </Card>
  );
}

/* ------------------------------ Manage ------------------------------ */

export function ManageCard({ ticket, isAdmin }: { ticket: Ticket; isAdmin: boolean }) {
  const { message } = App.useApp();
  const updateStatus = useUpdateStatus(ticket.id);
  const updatePriority = useUpdatePriority(ticket.id);
  const saveResolution = useSaveResolution(ticket.id);

  const saved = ticket.resolutionDetails ?? '';
  // null = untouched, so the box always follows the saved value until the user types
  const [draft, setDraft] = useState<string | null>(null);
  const resolution = draft ?? saved;
  const resolutionRef = useRef<TextAreaRef>(null);

  const locked = ticket.status === 'Closed' && !isAdmin;
  const dirty = resolution.trim() !== saved.trim();

  const changeStatus = (status: TicketStatus) => {
    // Mirrors the server rule, so people get the hint before a round-trip.
    if ((status === 'Resolved' || status === 'Closed') && !saved.trim()) {
      message.warning('Write and save the resolution details first, then resolve or close the ticket.');
      resolutionRef.current?.focus();
      return;
    }
    updateStatus.mutate(status);
  };

  return (
    <Card title="Manage ticket">
      <Space direction="vertical" size={16} style={{ width: '100%' }}>
        <div>
          <label htmlFor="manage-status" style={{ display: 'block', fontWeight: 600, marginBottom: 6 }}>
            Status
          </label>
          <Select<TicketStatus>
            id="manage-status"
            style={{ width: '100%' }}
            value={ticket.status}
            disabled={locked}
            loading={updateStatus.isPending}
            onChange={changeStatus}
            options={TICKET_STATUSES.map((s) => ({ value: s, label: STATUS_META[s].label }))}
          />
          {locked && (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Only an admin can re-open a closed ticket.
            </Typography.Text>
          )}
        </div>

        <div>
          <label htmlFor="manage-priority" style={{ display: 'block', fontWeight: 600, marginBottom: 6 }}>
            Priority
          </label>
          <Select<TicketPriority>
            id="manage-priority"
            style={{ width: '100%' }}
            value={ticket.priority}
            disabled={locked}
            loading={updatePriority.isPending}
            onChange={(priority) => updatePriority.mutate(priority)}
            options={TICKET_PRIORITIES.map((p) => ({ value: p, label: PRIORITY_META[p].label }))}
          />
        </div>

        <div>
          <label htmlFor="manage-resolution" style={{ display: 'block', fontWeight: 600, marginBottom: 6 }}>
            Resolution details
          </label>
          <Input.TextArea
            id="manage-resolution"
            ref={resolutionRef}
            rows={5}
            value={resolution}
            maxLength={RESOLUTION_MAX}
            showCount
            disabled={locked}
            placeholder="What fixed it? The customer sees this."
            onChange={(e) => setDraft(e.target.value)}
          />
          <Button
            style={{ marginTop: 26 }}
            onClick={() => saveResolution.mutate(resolution.trim(), { onSuccess: () => setDraft(null) })}
            loading={saveResolution.isPending}
            disabled={locked || !dirty || !resolution.trim()}
          >
            Save resolution
          </Button>
        </div>
      </Space>
    </Card>
  );
}

/* ------------------------------ Assign ------------------------------ */

export function AssignCard({ ticket }: { ticket: Ticket }) {
  const { data: agents = [], isLoading } = useAgents();
  const assign = useAssignAgent();

  const currentId = agents.find((a) => a.email === ticket.assignedAgent)?.id;
  const [picked, setPicked] = useState<string>();
  const selected = picked ?? currentId;

  return (
    <Card title="Assignment">
      <label htmlFor="assign-agent" style={{ display: 'block', fontWeight: 600, marginBottom: 6 }}>
        Agent
      </label>
      <Space direction="vertical" size={10} style={{ width: '100%' }}>
        <Select
          id="assign-agent"
          style={{ width: '100%' }}
          placeholder={isLoading ? 'Loading agents…' : 'Choose an agent'}
          loading={isLoading}
          value={selected}
          onChange={setPicked}
          showSearch
          optionFilterProp="label"
          options={agents.map((a) => ({ value: a.id, label: a.fullName ? `${a.fullName} (${a.email})` : a.email }))}
        />
        <Button
          type="primary"
          block
          loading={assign.isPending}
          disabled={!selected || selected === currentId}
          onClick={() => selected && assign.mutate({ ticketId: ticket.id, agentUserId: selected }, { onSuccess: () => setPicked(undefined) })}
        >
          {currentId ? 'Reassign' : 'Assign'}
        </Button>
      </Space>
      {ticket.status === 'Open' && (
        <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginTop: 8 }}>
          Assigning an open ticket moves it to “In progress”.
        </Typography.Text>
      )}
      {!isLoading && agents.length === 0 && (
        <Alert
          style={{ marginTop: 10 }}
          type="warning"
          showIcon
          message="There are no agents yet. Promote a user to Agent under Users."
        />
      )}
    </Card>
  );
}

/* ------------------------------ History ------------------------------ */

function pretty(field: string, value: string | null) {
  if (!value) return 'none';
  if (field === 'Status' && value in STATUS_META) return STATUS_META[value as TicketStatus].label;
  return value;
}

export function describeHistory(entry: HistoryEntry): string {
  switch (entry.fieldName) {
    case 'Ticket':
      return 'created the ticket';
    case 'Comment':
      return 'added a comment';
    case 'Resolution':
      return 'saved the resolution details';
    case 'Assigned agent':
      return `assigned the ticket to ${entry.newValue ?? 'nobody'}`;
    case 'Status':
    case 'Priority':
      return `changed ${entry.fieldName.toLowerCase()} from ${pretty(entry.fieldName, entry.oldValue)} to ${pretty(entry.fieldName, entry.newValue)}`;
    default:
      return `updated ${entry.fieldName.toLowerCase()}`;
  }
}

export function HistoryCard({ ticketId }: { ticketId: number }) {
  const { data, isLoading, isError } = useTicketHistory(ticketId);

  const items = useMemo(
    () =>
      [...(data ?? [])]
        .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
        .map((entry) => ({
          key: `${entry.createdAt}-${entry.fieldName}-${entry.newValue}`,
          color: entry.fieldName === 'Status' ? 'blue' : 'gray',
          children: (
            <div>
              <div>
                <Typography.Text strong>{entry.actor ?? 'System'}</Typography.Text> {describeHistory(entry)}
              </div>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                <TimeText value={entry.createdAt} />
              </Typography.Text>
            </div>
          ),
        })),
    [data],
  );

  return (
    <Card title="History">
      {isLoading ? (
        <Skeleton active paragraph={{ rows: 4 }} />
      ) : isError ? (
        <Typography.Text type="secondary">The history couldn’t be loaded.</Typography.Text>
      ) : items.length === 0 ? (
        <Typography.Text type="secondary">No activity yet.</Typography.Text>
      ) : (
        <Timeline items={items} style={{ marginTop: 6, marginBottom: -20 }} />
      )}
    </Card>
  );
}
