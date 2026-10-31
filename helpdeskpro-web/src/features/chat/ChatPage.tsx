import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Alert, Button, Drawer, Grid, Input, Skeleton, Space, Typography } from 'antd';
import { HistoryOutlined, PlusOutlined, RobotOutlined, SendOutlined } from '@ant-design/icons';
import { useConversation, useConversations } from '@/api/hooks';
import type { Conversation } from '@/api/types';
import { ErrorState } from '@/components/States';
import { CHAT_QUESTION_MAX } from '@/domain';
import { fromNow } from '@/utils/format';
import { MessageBubble, TypingBubble } from './ChatMessages';
import { EscalateModal } from './EscalateModal';
import { useChatSession } from './useChatSession';

const SUGGESTIONS = [
  'How do I reset my password?',
  'What do the ticket statuses mean?',
  'How can I create a support ticket?',
  'Why is my account locked?',
];

function ConversationList({
  conversations,
  loading,
  activeId,
  onSelect,
  onNew,
}: {
  conversations: Conversation[];
  loading: boolean;
  activeId?: number;
  onSelect: (id: number) => void;
  onNew: () => void;
}) {
  return (
    <>
      <div style={{ padding: 12, borderBottom: '1px solid var(--ant-color-border-secondary)' }}>
        <Button block type="primary" icon={<PlusOutlined />} onClick={onNew}>
          New chat
        </Button>
      </div>
      <nav className="chat-list__items" aria-label="Your conversations">
        {loading ? (
          <Skeleton active paragraph={{ rows: 5 }} style={{ padding: 12 }} />
        ) : conversations.length === 0 ? (
          <Typography.Paragraph type="secondary" style={{ padding: 12, margin: 0 }}>
            Your conversations will appear here.
          </Typography.Paragraph>
        ) : (
          conversations.map((c) => (
            <button
              key={c.id}
              type="button"
              className="chat-list__item"
              aria-current={c.id === activeId}
              onClick={() => onSelect(c.id)}
            >
              <span className="chat-list__title">{c.title}</span>
              <span className="chat-list__time">
                {fromNow(c.updatedAt)}
                {c.linkedTicketId ? ` · ticket #${c.linkedTicketId}` : ''}
              </span>
            </button>
          ))
        )}
      </nav>
    </>
  );
}

export default function ChatPage() {
  const params = useParams();
  const id = params.conversationId ? Number(params.conversationId) : undefined;
  const navigate = useNavigate();
  const screens = Grid.useBreakpoint();

  const conversations = useConversations();
  const conversation = useConversation(Number.isFinite(id) ? id : undefined);
  const { send, pending } = useChatSession(Number.isFinite(id) ? id : undefined);

  const [text, setText] = useState('');
  const [escalateOpen, setEscalateOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const endRef = useRef<HTMLDivElement>(null);

  const detail = conversation.data;
  const messages = detail?.messages ?? [];

  useEffect(() => {
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    endRef.current?.scrollIntoView({ block: 'end', behavior: reduce ? 'auto' : 'smooth' });
  }, [messages.length, pending, id]);

  const submit = async (question = text) => {
    const trimmed = question.trim();
    if (!trimmed || pending) return;
    setText('');
    const ok = await send(trimmed);
    if (!ok) setText(trimmed); // give the text back so nothing is lost
  };

  const goTo = (conversationId?: number) => {
    setDrawerOpen(false);
    navigate(conversationId ? `/assistant/${conversationId}` : '/assistant');
  };

  const list = (
    <ConversationList
      conversations={conversations.data ?? []}
      loading={conversations.isLoading}
      activeId={id}
      onSelect={goTo}
      onNew={() => goTo()}
    />
  );

  const hasTicket = !!detail?.linkedTicketId;
  const showEmptyState = !id || (!conversation.isLoading && messages.length === 0 && !pending);

  return (
    <div className="page page--chat">
      <div className="chat-shell">
        <aside className="chat-shell__list" aria-label="Conversations">
          {list}
        </aside>

        <section className="chat-shell__main" aria-label="Chat with the assistant">
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 12,
              padding: '12px 16px',
              borderBottom: '1px solid var(--ant-color-border-secondary)',
            }}
          >
            <Space size={10} style={{ minWidth: 0 }}>
              {!screens.lg && (
                <Button icon={<HistoryOutlined />} aria-label="Show conversations" onClick={() => setDrawerOpen(true)} />
              )}
              <div style={{ minWidth: 0 }}>
                <Typography.Text strong ellipsis style={{ display: 'block', maxWidth: 420 }}>
                  {detail && detail.title !== 'New conversation' ? detail.title : 'AI assistant'}
                </Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Answers come from approved help articles
                </Typography.Text>
              </div>
            </Space>

            {detail &&
              (hasTicket ? (
                <Link to={`/tickets/${detail.linkedTicketId}`}>
                  <Button>Ticket #{detail.linkedTicketId}</Button>
                </Link>
              ) : (
                messages.length > 0 && <Button onClick={() => setEscalateOpen(true)}>Talk to a person</Button>
              ))}
          </div>

          <div className="chat-scroll">
            {conversation.error ? (
              <ErrorState error={conversation.error} onRetry={() => void conversation.refetch()} />
            ) : conversation.isLoading ? (
              <Skeleton active paragraph={{ rows: 6 }} />
            ) : showEmptyState ? (
              <div style={{ maxWidth: 560, margin: '8vh auto 0', textAlign: 'center' }}>
                <div
                  style={{
                    width: 56,
                    height: 56,
                    margin: '0 auto 16px',
                    borderRadius: 16,
                    display: 'grid',
                    placeItems: 'center',
                    fontSize: 26,
                    color: 'var(--ant-color-primary)',
                    background: 'color-mix(in srgb, var(--ant-color-primary) 12%, transparent)',
                  }}
                >
                  <RobotOutlined />
                </div>
                <Typography.Title level={3} style={{ marginBottom: 6 }}>
                  How can I help today?
                </Typography.Title>
                <Typography.Paragraph type="secondary">
                  Ask about your account, tickets or how the help desk works. I’ll show which article each answer came from.
                </Typography.Paragraph>
                <Space wrap size={[8, 8]} style={{ justifyContent: 'center' }}>
                  {SUGGESTIONS.map((suggestion) => (
                    <Button key={suggestion} shape="round" onClick={() => void submit(suggestion)} disabled={pending}>
                      {suggestion}
                    </Button>
                  ))}
                </Space>
              </div>
            ) : (
              <div className="chat-column" role="log" aria-live="polite" aria-label="Conversation">
                {messages.map((message) => (
                  <MessageBubble
                    key={message.id}
                    message={message}
                    conversationId={detail!.id}
                    canEscalate={!hasTicket}
                    onEscalate={() => setEscalateOpen(true)}
                  />
                ))}
                {pending && <TypingBubble />}
                {hasTicket && (
                  <Alert
                    type="success"
                    showIcon
                    message={
                      <>
                        This chat was sent to support as <Link to={`/tickets/${detail!.linkedTicketId}`}>ticket #{detail!.linkedTicketId}</Link>.
                      </>
                    }
                  />
                )}
              </div>
            )}
            <div ref={endRef} />
          </div>

          <div className="chat-composer">
            <div className="chat-composer__inner">
              <Space.Compact style={{ width: '100%' }}>
                <Input.TextArea
                  aria-label="Your question"
                  autoSize={{ minRows: 1, maxRows: 5 }}
                  value={text}
                  maxLength={CHAT_QUESTION_MAX}
                  placeholder="Type your question…"
                  disabled={!!conversation.error}
                  onChange={(e) => setText(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) {
                      e.preventDefault();
                      void submit();
                    }
                  }}
                  style={{ resize: 'none' }}
                />
                <Button
                  type="primary"
                  icon={<SendOutlined />}
                  loading={pending}
                  disabled={!text.trim() || !!conversation.error}
                  onClick={() => void submit()}
                  style={{ height: 'auto', minWidth: 48 }}
                  aria-label="Send question"
                />
              </Space.Compact>
              <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginTop: 6 }}>
                Enter to send · Shift + Enter for a new line. The assistant can make mistakes, so check the sources.
              </Typography.Text>
            </div>
          </div>
        </section>
      </div>

      <Drawer title="Conversations" placement="left" open={drawerOpen} onClose={() => setDrawerOpen(false)} width={300} styles={{ body: { padding: 0 } }}>
        {list}
      </Drawer>

      {detail && <EscalateModal conversation={detail} open={escalateOpen} onClose={() => setEscalateOpen(false)} />}
    </div>
  );
}
