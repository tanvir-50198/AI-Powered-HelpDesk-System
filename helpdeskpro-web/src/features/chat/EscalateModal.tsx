import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Alert, Checkbox, Col, Form, Input, Modal, Radio, Row, Select, Typography } from 'antd';
import { useQueryClient } from '@tanstack/react-query';
import { chatApi } from '@/api/endpoints';
import { qk, useAppMutation, useCategories } from '@/api/hooks';
import type { ConversationDetail, TicketPriority } from '@/api/types';
import { PRIORITY_META, TICKET_DESCRIPTION_MAX, TICKET_PRIORITIES, TICKET_TITLE_MAX } from '@/domain';

interface FormValues {
  title: string;
  categoryId: number;
  priority: TicketPriority;
  description: string;
  confirmed: boolean;
}

/** Same wording the server-rendered page used (ChatController.BuildTitle / BuildSummary). */
function draftFrom(conversation: ConversationDetail) {
  const questions = conversation.messages.filter((m) => m.role === 'User').map((m) => m.content);
  const first = questions[0] ?? 'Help needed';
  const title = first.length > 100 ? first.slice(0, 100) + '…' : first;
  const description = [
    'I asked the AI assistant and could not solve my problem.',
    '',
    'My questions:',
    ...questions.slice(-3).map((q) => `- ${q}`),
    '',
    'Please help me.',
  ].join('\n');
  return { title, description };
}

export function EscalateModal({
  conversation,
  open,
  onClose,
}: {
  conversation: ConversationDetail;
  open: boolean;
  onClose: () => void;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form] = Form.useForm<FormValues>();
  const { data: categories = [], isLoading: loadingCategories } = useCategories();
  const [error, setError] = useState<string | null>(null);

  const create = useAppMutation({
    mutationFn: (values: FormValues) => chatApi.createTicket(conversation.id, values),
    silentError: true,
    onSuccess: (ticket) => {
      void queryClient.invalidateQueries({ queryKey: qk.tickets });
      void queryClient.invalidateQueries({ queryKey: qk.dashboard });
      void queryClient.invalidateQueries({ queryKey: qk.conversations });
      void queryClient.invalidateQueries({ queryKey: qk.conversation(conversation.id) });
      onClose();
      navigate(`/tickets/${ticket.id}`);
    },
    successMessage: (ticket) => `Ticket #${ticket.id} created. An agent will help you soon.`,
  });

  const draft = draftFrom(conversation);

  return (
    <Modal
      open={open}
      title="Talk to a person"
      okText="Create ticket"
      cancelText="Back to chat"
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={create.isPending}
      destroyOnHidden
      width={640}
      afterOpenChange={(visible) => visible && setError(null)}
    >
      <Typography.Paragraph type="secondary">
        We wrote a summary of your chat. Check it, change anything you like, and confirm. Nothing is sent until you do.
      </Typography.Paragraph>

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 16 }} role="alert" />}

      <Form<FormValues>
        form={form}
        layout="vertical"
        requiredMark={false}
        initialValues={{ ...draft, priority: 'Medium', confirmed: false }}
        onFinish={(values) => {
          setError(null);
          create.mutate({ ...values, confirmed: true }, { onError: (e) => setError(e.message) });
        }}
      >
        <Form.Item
          label="Subject"
          name="title"
          rules={[
            { required: true, whitespace: true, message: 'Give the ticket a subject.' },
            { max: TICKET_TITLE_MAX, message: `Use ${TICKET_TITLE_MAX} characters or fewer.` },
          ]}
        >
          <Input />
        </Form.Item>

        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item label="Category" name="categoryId" rules={[{ required: true, message: 'Choose a category.' }]}>
              <Select
                placeholder="Choose a category"
                loading={loadingCategories}
                options={categories.map((c) => ({ value: c.id, label: c.name }))}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item label="Priority" name="priority">
              <Radio.Group optionType="button" buttonStyle="solid">
                {TICKET_PRIORITIES.map((p) => (
                  <Radio.Button key={p} value={p}>
                    {PRIORITY_META[p].label}
                  </Radio.Button>
                ))}
              </Radio.Group>
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          label="Summary"
          name="description"
          rules={[
            { required: true, whitespace: true, message: 'Describe the problem.' },
            { max: TICKET_DESCRIPTION_MAX, message: `Use ${TICKET_DESCRIPTION_MAX} characters or fewer.` },
          ]}
        >
          <Input.TextArea rows={8} showCount maxLength={TICKET_DESCRIPTION_MAX + 50} />
        </Form.Item>

        <Form.Item
          name="confirmed"
          valuePropName="checked"
          rules={[
            {
              validator: (_, value) =>
                value ? Promise.resolve() : Promise.reject(new Error('Please confirm the summary to continue.')),
            },
          ]}
          style={{ marginBottom: 0 }}
        >
          <Checkbox>I’ve read this summary and want to send it to the support team.</Checkbox>
        </Form.Item>
      </Form>
    </Modal>
  );
}
