import { useParams } from 'react-router-dom';
import { Card, Col, Descriptions, Row, Skeleton, Space, Typography } from 'antd';
import { CheckCircleFilled } from '@ant-design/icons';
import { useTicket } from '@/api/hooks';
import { useCurrentUser } from '@/auth/AuthContext';
import { PageHeader } from '@/components/PageHeader';
import { ErrorState } from '@/components/States';
import { PriorityBadge, StatusTag } from '@/components/Tags';
import { TimeText } from '@/components/TimeText';
import { fullDate } from '@/utils/format';
import { AssignCard, CommentsCard, HistoryCard, ManageCard } from './TicketPanels';

export default function TicketDetailPage() {
  const { id } = useParams();
  const ticketId = Number(id);
  const user = useCurrentUser();
  const { data: ticket, isLoading, error, refetch } = useTicket(ticketId);

  const listLabel = user.role === 'Customer' ? 'My tickets' : user.role === 'Agent' ? 'Assigned tickets' : 'All tickets';

  if (isLoading) {
    return (
      <div className="page">
        <Skeleton active paragraph={{ rows: 10 }} />
      </div>
    );
  }
  if (error || !ticket) {
    return (
      <div className="page">
        <ErrorState error={error ?? new Error('Ticket not found.')} onRetry={() => void refetch()} />
      </div>
    );
  }

  // Agents only ever receive their own assigned tickets, admins manage everything.
  const canManage = user.role === 'Admin' || user.role === 'Agent';

  return (
    <div className="page">
      <PageHeader
        breadcrumb={[{ label: listLabel, to: '/tickets' }, { label: `#${ticket.id}` }]}
        title={ticket.title}
        meta={
          <Space size={10} wrap>
            <StatusTag status={ticket.status} />
            <PriorityBadge priority={ticket.priority} />
          </Space>
        }
        subtitle={
          <>
            Ticket #{ticket.id} · opened <TimeText value={ticket.createdAt} /> by {ticket.createdBy}
          </>
        }
      />

      <Row gutter={[20, 20]}>
        <Col xs={24} lg={16}>
          <Space direction="vertical" size={20} style={{ width: '100%' }}>
            <Card title="Description">
              <div className="pre-wrap">{ticket.description}</div>
            </Card>

            {ticket.resolutionDetails && (
              <Card
                className="resolution-card"
                title={
                  <Space>
                    <CheckCircleFilled style={{ color: 'var(--ant-color-success)' }} />
                    Resolution
                  </Space>
                }
              >
                <div className="pre-wrap">{ticket.resolutionDetails}</div>
              </Card>
            )}

            <CommentsCard ticket={ticket} />
          </Space>
        </Col>

        <Col xs={24} lg={8}>
          <Space direction="vertical" size={20} style={{ width: '100%' }}>
            <Card title="Details">
              <Descriptions column={1} size="small" colon={false} styles={{ label: { width: 96, color: 'var(--ant-color-text-secondary)' } }}>
                <Descriptions.Item label="Category">{ticket.category}</Descriptions.Item>
                <Descriptions.Item label="Requester">{ticket.createdBy}</Descriptions.Item>
                <Descriptions.Item label="Agent">
                  {ticket.assignedAgent ?? <Typography.Text type="secondary">Not assigned yet</Typography.Text>}
                </Descriptions.Item>
                <Descriptions.Item label="Created">{fullDate(ticket.createdAt)}</Descriptions.Item>
                <Descriptions.Item label="Updated">{fullDate(ticket.updatedAt)}</Descriptions.Item>
              </Descriptions>
            </Card>

            {user.role === 'Admin' && <AssignCard ticket={ticket} />}
            {canManage && <ManageCard ticket={ticket} isAdmin={user.role === 'Admin'} />}
            <HistoryCard ticketId={ticket.id} />
          </Space>
        </Col>
      </Row>
    </div>
  );
}
