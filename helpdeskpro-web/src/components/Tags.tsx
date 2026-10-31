import { Tag, Tooltip } from 'antd';
import type { ApprovalStatus, ProcessingStatus, Role, TicketPriority, TicketStatus } from '@/api/types';
import { APPROVAL_META, PRIORITY_META, PROCESSING_META, ROLE_META, STATUS_META } from '@/domain';

export function StatusTag({ status }: { status: TicketStatus }) {
  const meta = STATUS_META[status];
  return (
    <Tooltip title={meta.hint}>
      <Tag color={meta.color} bordered={false} style={{ marginInlineEnd: 0 }}>
        {meta.label}
      </Tag>
    </Tooltip>
  );
}

/** Four rising bars (like a signal meter) + the word, so priority never relies on colour alone. */
export function PriorityBadge({ priority }: { priority: TicketPriority }) {
  const meta = PRIORITY_META[priority];
  return (
    <Tooltip title={meta.hint}>
      <span className="priority-badge" style={{ color: meta.color }}>
        <svg width="14" height="14" viewBox="0 0 14 14" aria-hidden="true" focusable="false">
          {[0, 1, 2, 3].map((i) => (
            <rect
              key={i}
              x={i * 3.7}
              y={10 - i * 3}
              width="2.4"
              height={4 + i * 3}
              rx="0.8"
              fill="currentColor"
              opacity={i < meta.level ? 1 : 0.22}
            />
          ))}
        </svg>
        <span>{meta.label}</span>
      </span>
    </Tooltip>
  );
}

export const RoleTag = ({ role }: { role: Role }) => (
  <Tag color={ROLE_META[role].color} bordered={false} style={{ marginInlineEnd: 0 }}>
    {ROLE_META[role].label}
  </Tag>
);

export const ApprovalTag = ({ status }: { status: ApprovalStatus }) => (
  <Tag color={APPROVAL_META[status].color} bordered={false} style={{ marginInlineEnd: 0 }}>
    {APPROVAL_META[status].label}
  </Tag>
);

export function ProcessingTag({ status, error }: { status: ProcessingStatus; error?: string | null }) {
  const tag = (
    <Tag color={PROCESSING_META[status].color} bordered={false} style={{ marginInlineEnd: 0 }}>
      {PROCESSING_META[status].label}
    </Tag>
  );
  return status === 'Failed' && error ? <Tooltip title={error}>{tag}</Tooltip> : tag;
}
