import type { ReactNode } from 'react';
import { Table, Typography, type TablePaginationConfig } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';
import type { Role, Ticket } from '@/api/types';
import { PriorityBadge, StatusTag } from '@/components/Tags';
import { TimeText } from '@/components/TimeText';

interface Props {
  tickets: Ticket[];
  role: Role;
  loading?: boolean;
  pagination?: TablePaginationConfig | false;
  empty?: ReactNode;
  /** Replaces the "Agent" cell, e.g. with an inline assign control. */
  renderAgent?: (ticket: Ticket) => ReactNode;
  size?: 'small' | 'middle';
}

export function TicketTable({ tickets, role, loading, pagination = false, empty, renderAgent, size = 'middle' }: Props) {
  const navigate = useNavigate();

  const columns: ColumnsType<Ticket> = [
    {
      title: 'Ticket',
      key: 'title',
      className: 'ticket-title-cell',
      render: (_, ticket) => (
        <div>
          <div className="ticket-title">
            <Typography.Text type="secondary" style={{ fontWeight: 500, marginRight: 8 }}>
              #{ticket.id}
            </Typography.Text>
            {ticket.title}
          </div>
          <div className="ticket-sub">
            {ticket.category}
            {role !== 'Customer' && <> · from {ticket.createdBy}</>}
          </div>
        </div>
      ),
    },
    { title: 'Status', dataIndex: 'status', key: 'status', width: 120, render: (_, t) => <StatusTag status={t.status} /> },
    { title: 'Priority', dataIndex: 'priority', key: 'priority', width: 120, render: (_, t) => <PriorityBadge priority={t.priority} /> },
    {
      title: 'Agent',
      key: 'agent',
      width: 220,
      responsive: ['lg'],
      render: (_, t) =>
        renderAgent ? (
          renderAgent(t)
        ) : t.assignedAgent ? (
          <span>{t.assignedAgent}</span>
        ) : (
          <Typography.Text type="secondary">Unassigned</Typography.Text>
        ),
    },
    {
      title: 'Updated',
      key: 'updated',
      width: 130,
      responsive: ['md'],
      render: (_, t) => <TimeText value={t.updatedAt} />,
    },
  ];

  return (
    <Table<Ticket>
      rowKey="id"
      size={size}
      columns={columns}
      dataSource={tickets}
      loading={loading}
      pagination={pagination}
      scroll={{ x: 560 }}
      locale={{ emptyText: empty }}
      rowClassName="clickable-row"
      onRow={(ticket) => ({
        onClick: (event) => {
          // let inline controls (selects, links) work without opening the ticket
          if ((event.target as HTMLElement).closest('.ant-select, a, button, input')) return;
          navigate(`/tickets/${ticket.id}`);
        },
        onKeyDown: (event) => {
          if (event.key === 'Enter') navigate(`/tickets/${ticket.id}`);
        },
        tabIndex: 0,
      })}
    />
  );
}
