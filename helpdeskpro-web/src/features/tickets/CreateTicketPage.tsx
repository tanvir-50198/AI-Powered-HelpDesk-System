import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Alert, Button, Card, Col, Form, Input, Radio, Row, Select, Space, Typography } from 'antd';
import { MessageOutlined } from '@ant-design/icons';
import { useCategories, useCreateTicket } from '@/api/hooks';
import type { CreateTicketRequest } from '@/api/types';
import { PageHeader } from '@/components/PageHeader';
import { PriorityBadge } from '@/components/Tags';
import { PRIORITY_META, TICKET_DESCRIPTION_MAX, TICKET_PRIORITIES, TICKET_TITLE_MAX } from '@/domain';

export default function CreateTicketPage() {
  const navigate = useNavigate();
  const [form] = Form.useForm<CreateTicketRequest>();
  const { data: categories = [], isLoading: loadingCategories } = useCategories();
  const create = useCreateTicket();
  const [error, setError] = useState<string | null>(null);

  const priority = Form.useWatch('priority', form);

  const submit = (values: CreateTicketRequest) => {
    setError(null);
    create.mutate(values, {
      onSuccess: (ticket) => navigate(`/tickets/${ticket.id}`),
      onError: (e) => setError(e.message),
    });
  };

  return (
    <div className="page">
      <PageHeader
        breadcrumb={[{ label: 'My tickets', to: '/tickets' }, { label: 'New ticket' }]}
        title="Create a ticket"
        subtitle="Tell us what’s going on. An agent will reply here, and you’ll see every update on the ticket."
      />

      <Row gutter={[20, 20]}>
        <Col xs={24} lg={16}>
          <Card>
            {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 16 }} role="alert" />}

            <Form<CreateTicketRequest>
              form={form}
              layout="vertical"
              requiredMark={false}
              initialValues={{ priority: 'Medium' }}
              onFinish={submit}
            >
              <Form.Item
                label="Subject"
                name="title"
                rules={[
                  { required: true, whitespace: true, message: 'Give your ticket a short subject.' },
                  { max: TICKET_TITLE_MAX, message: `Use ${TICKET_TITLE_MAX} characters or fewer.` },
                ]}
              >
                <Input size="large" placeholder="e.g. I can’t sign in after changing my password" maxLength={TICKET_TITLE_MAX + 20} showCount />
              </Form.Item>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item label="Category" name="categoryId" rules={[{ required: true, message: 'Choose a category.' }]}>
                    <Select
                      size="large"
                      placeholder="Choose a category"
                      loading={loadingCategories}
                      options={categories.map((c) => ({ value: c.id, label: c.name, title: c.description ?? undefined }))}
                    />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    label="Priority"
                    name="priority"
                    extra={priority ? PRIORITY_META[priority].hint : undefined}
                  >
                    <Radio.Group buttonStyle="solid" optionType="button" size="large" style={{ display: 'flex', flexWrap: 'wrap' }}>
                      {TICKET_PRIORITIES.map((p) => (
                        <Radio.Button key={p} value={p} aria-label={`${PRIORITY_META[p].label} priority`}>
                          {PRIORITY_META[p].label}
                        </Radio.Button>
                      ))}
                    </Radio.Group>
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item
                label="What happened?"
                name="description"
                rules={[
                  { required: true, whitespace: true, message: 'Describe the problem so we can help.' },
                  { max: TICKET_DESCRIPTION_MAX, message: `Use ${TICKET_DESCRIPTION_MAX} characters or fewer.` },
                ]}
              >
                <Input.TextArea
                  rows={9}
                  maxLength={TICKET_DESCRIPTION_MAX + 50}
                  showCount
                  placeholder={'What were you trying to do?\nWhat did you expect, and what happened instead?\nAny error message you saw?'}
                />
              </Form.Item>

              <Space>
                <Button type="primary" htmlType="submit" size="large" loading={create.isPending}>
                  Create ticket
                </Button>
                <Button size="large" onClick={() => navigate(-1)} disabled={create.isPending}>
                  Cancel
                </Button>
              </Space>
            </Form>
          </Card>
        </Col>

        <Col xs={24} lg={8}>
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <Card title="A good ticket includes">
              <ul style={{ margin: 0, paddingLeft: 18, lineHeight: 1.8 }}>
                <li>What you were trying to do</li>
                <li>The exact error message, if any</li>
                <li>When it started, and how often it happens</li>
                <li>What you already tried</li>
              </ul>
            </Card>

            <Card>
              <Typography.Text strong>Want a quicker answer?</Typography.Text>
              <Typography.Paragraph type="secondary" style={{ margin: '4px 0 12px' }}>
                The AI assistant answers from our approved help articles and shows its sources.
              </Typography.Paragraph>
              <Link to="/assistant">
                <Button icon={<MessageOutlined />}>Ask the assistant first</Button>
              </Link>
            </Card>

            <Card title="Priority guide" styles={{ body: { display: 'grid', gap: 10 } }}>
              {TICKET_PRIORITIES.map((p) => (
                <div key={p} style={{ display: 'flex', justifyContent: 'space-between', gap: 12 }}>
                  <PriorityBadge priority={p} />
                  <Typography.Text type="secondary" style={{ textAlign: 'right' }}>
                    {PRIORITY_META[p].hint}
                  </Typography.Text>
                </div>
              ))}
            </Card>
          </Space>
        </Col>
      </Row>
    </div>
  );
}
