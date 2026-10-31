import { Link } from 'react-router-dom';
import { Alert, Card, Col, Progress, Row, Table, Typography, theme } from 'antd';
import { Bar, BarChart, CartesianGrid, Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip as ChartTooltip, XAxis, YAxis } from 'recharts';
import { CheckCircleOutlined, ClockCircleOutlined, CustomerServiceOutlined, LikeOutlined } from '@ant-design/icons';
import { useAdminSummary } from '@/api/hooks';
import { PageHeader } from '@/components/PageHeader';
import { StatCard } from '@/components/StatCard';
import { ErrorState } from '@/components/States';
import { TimeText } from '@/components/TimeText';
import { STATUS_META } from '@/domain';
import type { TicketStatus } from '@/api/types';
import { percent } from '@/utils/format';

const STATUS_COLORS: Record<string, string> = { Open: '#2a6fc4', InProgress: '#d49a2a', Resolved: '#1f8f5f', Closed: '#8a99a0' };

export default function ReportsPage() {
  const { token } = theme.useToken();
  const { data, isLoading, error, refetch } = useAdminSummary();

  if (error) {
    return (
      <div className="page">
        <ErrorState error={error} onRetry={() => void refetch()} />
      </div>
    );
  }

  const status = (data?.statusDistribution ?? []).map((s) => ({
    ...s,
    label: STATUS_META[s.status as TicketStatus]?.label ?? s.status,
  }));
  const rated = (data?.helpfulFeedback ?? 0) + (data?.notHelpfulFeedback ?? 0);

  return (
    <div className="page">
      <PageHeader title="Reports" subtitle="How tickets and the AI assistant are performing." />

      <Row gutter={[16, 16]}>
        <Col xs={12} lg={6}>
          <StatCard label="Total tickets" value={data?.totalTickets ?? 0} icon={<CustomerServiceOutlined />} loading={isLoading} />
        </Col>
        <Col xs={12} lg={6}>
          <StatCard
            label="Overdue tickets"
            value={data?.overdueTickets ?? 0}
            icon={<ClockCircleOutlined />}
            tone={(data?.overdueTickets ?? 0) > 0 ? '#c0262d' : '#1f8f5f'}
            loading={isLoading}
            footnote={`Open longer than ${data?.demoSlaHours ?? 48} h (demo SLA)`}
          />
        </Col>
        <Col xs={12} lg={6}>
          <StatCard
            label="Chats escalated to a person"
            value={percent(data?.escalationRatePercent ?? 0)}
            icon={<CheckCircleOutlined />}
            loading={isLoading}
            footnote={`${data?.escalatedConversations ?? 0} of ${data?.totalConversations ?? 0} chats`}
          />
        </Col>
        <Col xs={12} lg={6}>
          <StatCard
            label="Answers rated helpful"
            value={rated ? percent(data?.helpfulRatePercent ?? 0) : '–'}
            icon={<LikeOutlined />}
            tone="#1f8f5f"
            loading={isLoading}
            footnote={rated ? `${rated} ratings` : 'No ratings yet'}
          />
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={14}>
          <Card title="Tickets in the last 7 days" loading={isLoading}>
            <div style={{ height: 280 }}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data?.last7Days ?? []} margin={{ top: 8, right: 8, left: -18, bottom: 0 }}>
                  <CartesianGrid vertical={false} stroke={token.colorBorderSecondary} />
                  <XAxis dataKey="day" tickLine={false} axisLine={false} />
                  <YAxis allowDecimals={false} tickLine={false} axisLine={false} />
                  <ChartTooltip cursor={{ fill: token.colorFillTertiary }} contentStyle={{ borderRadius: 8, background: token.colorBgElevated, border: `1px solid ${token.colorBorderSecondary}` }} />
                  <Legend iconType="circle" />
                  <Bar dataKey="created" name="Created" fill={token.colorPrimary} radius={[4, 4, 0, 0]} maxBarSize={28} animationDuration={500} />
                  <Bar dataKey="resolved" name="Resolved" fill="#1f8f5f" radius={[4, 4, 0, 0]} maxBarSize={28} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </Card>
        </Col>
        <Col xs={24} lg={10}>
          <Card title="Tickets by status" loading={isLoading}>
            <div style={{ height: 280 }}>
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie data={status} dataKey="count" nameKey="label" innerRadius={62} outerRadius={96} paddingAngle={2} stroke="none" animationDuration={500}>
                    {status.map((s) => (
                      <Cell key={s.status} fill={STATUS_COLORS[s.status] ?? '#8a99a0'} />
                    ))}
                  </Pie>
                  <ChartTooltip contentStyle={{ borderRadius: 8, background: token.colorBgElevated, border: `1px solid ${token.colorBorderSecondary}` }} />
                  <Legend iconType="circle" />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={8}>
          <Card title="Assistant quality" loading={isLoading}>
            <Typography.Text type="secondary">Helpful vs. not helpful</Typography.Text>
            <Progress
              percent={Math.round(data?.helpfulRatePercent ?? 0)}
              success={{ percent: 0 }}
              strokeColor="#1f8f5f"
              trailColor={rated ? '#f0b9bc' : undefined}
              format={(p) => (rated ? `${p}%` : '–')}
            />
            <Typography.Paragraph type="secondary" style={{ margin: '8px 0 16px', fontSize: 13 }}>
              {data?.helpfulFeedback ?? 0} helpful · {data?.notHelpfulFeedback ?? 0} not helpful
            </Typography.Paragraph>
            <Typography.Text strong style={{ fontSize: 22 }}>{data?.fallbackAnswers ?? 0}</Typography.Text>
            <Typography.Text type="secondary"> answers where the assistant found nothing</Typography.Text>
            <div style={{ marginTop: 16 }}>
              <Typography.Text strong style={{ fontSize: 22 }}>{data?.approvedDocuments ?? 0}</Typography.Text>
              <Typography.Text type="secondary"> approved documents</Typography.Text>
            </div>
            {(data?.failedDocuments ?? 0) > 0 && (
              <Alert
                style={{ marginTop: 16 }}
                type="warning"
                showIcon
                message={<>{data?.failedDocuments} document(s) failed to index. <Link to="/admin/knowledge">Review them</Link></>}
              />
            )}
          </Card>
        </Col>
        <Col xs={24} lg={16}>
          <Card title="Questions the assistant couldn’t answer" loading={isLoading} extra={<Link to="/admin/knowledge">Add a document</Link>}>
            <Typography.Paragraph type="secondary" style={{ fontSize: 13 }}>
              Good candidates for new help articles.
            </Typography.Paragraph>
            <Table
              size="small"
              rowKey={(r) => `${r.conversationId}-${r.askedAt}`}
              pagination={{ pageSize: 6, hideOnSinglePage: true }}
              dataSource={data?.unansweredQuestions ?? []}
              locale={{ emptyText: 'Nothing here. The assistant answered every question.' }}
              columns={[
                { title: 'Question', dataIndex: 'question', ellipsis: true },
                { title: 'Asked', dataIndex: 'askedAt', width: 130, render: (v: string) => <TimeText value={v} /> },
              ]}
            />
          </Card>
        </Col>
      </Row>
    </div>
  );
}
