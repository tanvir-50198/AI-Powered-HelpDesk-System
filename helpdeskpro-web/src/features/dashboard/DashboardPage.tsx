import { Link } from 'react-router-dom';
import { Button, Card, Col, Row, Select, Space, Typography } from 'antd';
import {
  CheckCircleOutlined,
  ClockCircleOutlined,
  InboxOutlined,
  LockOutlined,
  MessageOutlined,
  PlusOutlined,
  TeamOutlined,
  UserDeleteOutlined,
} from '@ant-design/icons';
import { useAgents, useAssignAgent, useDashboard, useTickets } from '@/api/hooks';
import type { Ticket, TicketStatus } from '@/api/types';
import { useCurrentUser } from '@/auth/AuthContext';
import { PageHeader } from '@/components/PageHeader';
import { StatCard } from '@/components/StatCard';
import { EmptyState, ErrorState } from '@/components/States';
import { STATUS_META, TICKET_STATUSES } from '@/domain';
import { displayName } from '@/utils/format';
import { TicketTable } from '@/features/tickets/TicketTable';

const STATUS_ICON: Record<TicketStatus, React.ReactNode> = {
  Open: <InboxOutlined />,
  InProgress: <ClockCircleOutlined />,
  Resolved: <CheckCircleOutlined />,
  Closed: <LockOutlined />,
};
const STATUS_TONE: Record<TicketStatus, string> = {
  Open: '#2a6fc4',
  InProgress: '#b7791f',
  Resolved: '#1f8f5f',
  Closed: '#6b7c85',
};

function greeting() {
  const hour = new Date().getHours();
  return hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
}

function StatusCards() {
  const { data, isLoading } = useDashboard();
  return (
    <Row gutter={[16, 16]}>
      {TICKET_STATUSES.map((status) => (
        <Col xs={12} lg={6} key={status}>
          <StatCard
            label={STATUS_META[status].label}
            value={data?.statusCounts[status] ?? 0}
            icon={STATUS_ICON[status]}
            tone={STATUS_TONE[status]}
            loading={isLoading}
            to={`/tickets?status=${status}`}
          />
        </Col>
      ))}
    </Row>
  );
}

/* ------------------------------ Customer ------------------------------ */

function CustomerDashboard() {
  const { data, isLoading, error, refetch } = useTickets({ pageSize: 5 });

  return (
    <>
      <StatusCards />

      <Row gutter={[16, 16]} style={{ marginTop: 20 }}>
        <Col xs={24} xl={17}>
          <Card
            title="Recent tickets"
            extra={<Link to="/tickets">View all</Link>}
            styles={{ body: { padding: 0 } }}
          >
            {error ? (
              <ErrorState error={error} onRetry={() => void refetch()} />
            ) : (
              <TicketTable
                tickets={data?.items ?? []}
                role="Customer"
                loading={isLoading}
                empty={
                  <EmptyState
                    title="No tickets yet"
                    description="When you open a ticket, you can follow it here."
                    action={
                      <Link to="/tickets/new">
                        <Button type="primary">Create a ticket</Button>
                      </Link>
                    }
                  />
                }
              />
            )}
          </Card>
        </Col>

        <Col xs={24} xl={7}>
          <Card title="Need help?">
            <Space direction="vertical" size={12} style={{ width: '100%' }}>
              <Typography.Text type="secondary">
                The assistant answers from approved help articles, and shows its sources. If it can’t help, hand the chat to a person
                in one click.
              </Typography.Text>
              <Link to="/assistant">
                <Button block type="primary" icon={<MessageOutlined />}>
                  Ask the AI assistant
                </Button>
              </Link>
              <Link to="/tickets/new">
                <Button block icon={<PlusOutlined />}>
                  Create a ticket
                </Button>
              </Link>
            </Space>
          </Card>
        </Col>
      </Row>
    </>
  );
}

/* -------------------------------- Agent -------------------------------- */

function AgentDashboard() {
  const { data, isLoading, error, refetch } = useTickets({ sortBy: 'priority', pageSize: 20 });
  const active = (data?.items ?? []).filter((t) => t.status === 'Open' || t.status === 'InProgress').slice(0, 8);

  return (
    <>
      <StatusCards />

      <Card
        style={{ marginTop: 20 }}
        title="Needs your attention"
        extra={<Link to="/tickets?sort=priority">View all</Link>}
        styles={{ body: { padding: 0 } }}
      >
        {error ? (
          <ErrorState error={error} onRetry={() => void refetch()} />
        ) : (
          <TicketTable
            tickets={active}
            role="Agent"
            loading={isLoading}
            empty={<EmptyState title="You’re all caught up" description="No open or in-progress tickets are assigned to you." />}
          />
        )}
      </Card>
    </>
  );
}

/* -------------------------------- Admin -------------------------------- */

function InlineAssign({ ticket }: { ticket: Ticket }) {
  const { data: agents = [], isLoading } = useAgents();
  const assign = useAssignAgent();
  return (
    <Select
      size="small"
      style={{ width: 200 }}
      placeholder="Assign to…"
      aria-label={`Assign ticket ${ticket.id}`}
      loading={isLoading || assign.isPending}
      disabled={assign.isPending}
      showSearch
      optionFilterProp="label"
      options={agents.map((a) => ({ value: a.id, label: a.fullName ?? a.email }))}
      onChange={(agentUserId: string) => assign.mutate({ ticketId: ticket.id, agentUserId })}
    />
  );
}

function AdminDashboard() {
  const { data: summary, isLoading: summaryLoading } = useDashboard();
  const { data, isLoading, error, refetch } = useTickets({ onlyUnassigned: true, pageSize: 5 });
  const unassigned = summary?.unassignedTickets ?? 0;

  return (
    <>
      <StatusCards />

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} sm={8}>
          <StatCard
            label="Waiting for an agent"
            value={unassigned}
            icon={<UserDeleteOutlined />}
            tone={unassigned > 0 ? '#c2610c' : '#1f8f5f'}
            loading={summaryLoading}
            to="/tickets?unassigned=1"
            footnote={unassigned > 0 ? 'Assign these first' : 'Everything is assigned'}
          />
        </Col>
        <Col xs={12} sm={8}>
          <StatCard label="Users" value={summary?.totalUsers ?? 0} icon={<TeamOutlined />} loading={summaryLoading} />
        </Col>
        <Col xs={12} sm={8}>
          <StatCard
            label="Assistant conversations"
            value={summary?.totalConversations ?? 0}
            icon={<MessageOutlined />}
            loading={summaryLoading}
          />
        </Col>
      </Row>

      <Card
        style={{ marginTop: 20 }}
        title="Unassigned tickets"
        extra={<Link to="/tickets?unassigned=1">View all</Link>}
        styles={{ body: { padding: 0 } }}
      >
        {error ? (
          <ErrorState error={error} onRetry={() => void refetch()} />
        ) : (
          <TicketTable
            tickets={data?.items ?? []}
            role="Admin"
            loading={isLoading}
            renderAgent={(ticket) => <InlineAssign ticket={ticket} />}
            empty={<EmptyState title="Nothing waiting" description="Every ticket has an agent." />}
          />
        )}
      </Card>
    </>
  );
}

/* -------------------------------- Page -------------------------------- */

export default function DashboardPage() {
  const user = useCurrentUser();
  const first = displayName(user).split(' ')[0];

  const subtitle = {
    Customer: 'Here’s where your requests stand.',
    Agent: 'Your assigned work, most urgent first.',
    Admin: 'A snapshot of the help desk right now.',
  }[user.role];

  return (
    <div className="page">
      <PageHeader title={`${greeting()}, ${first}`} subtitle={subtitle} />
      {user.role === 'Customer' && <CustomerDashboard />}
      {user.role === 'Agent' && <AgentDashboard />}
      {user.role === 'Admin' && <AdminDashboard />}
    </div>
  );
}
