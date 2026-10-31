import { Fragment, useState } from 'react';
import { Avatar, Button, Input, Popover, Space, Tooltip, Typography } from 'antd';
import { DislikeFilled, DislikeOutlined, LikeFilled, LikeOutlined, RobotOutlined, UserOutlined } from '@ant-design/icons';
import { useQueryClient } from '@tanstack/react-query';
import { chatApi } from '@/api/endpoints';
import { qk, useAppMutation } from '@/api/hooks';
import type { ChatMessage, ChatSource, ConversationDetail } from '@/api/types';
import { TimeText } from '@/components/TimeText';

/* ---------- "…see the reset page [1]." -> text with clickable citation chips ---------- */

function AnswerText({ text, sources }: { text: string; sources: ChatSource[] }) {
  const parts = text.split(/(\[\d+\])/g);
  return (
    <div className="pre-wrap">
      {parts.map((part, index) => {
        const match = /^\[(\d+)\]$/.exec(part);
        if (!match) return <Fragment key={index}>{part}</Fragment>;
        const number = Number(match[1]);
        const source = sources[number - 1];
        if (!source) return <Fragment key={index}>{part}</Fragment>;
        return (
          <Tooltip key={index} title={source.section ? `${source.title} › ${source.section}` : source.title}>
            <a
              href={`#source-${number}`}
              aria-label={`Source ${number}: ${source.title}`}
              style={{
                display: 'inline-block',
                minWidth: 18,
                padding: '0 5px',
                margin: '0 2px',
                borderRadius: 9,
                textAlign: 'center',
                fontSize: 11,
                fontWeight: 700,
                lineHeight: '17px',
                textDecoration: 'none',
                verticalAlign: 'super',
                color: 'var(--ant-color-primary)',
                background: 'color-mix(in srgb, var(--ant-color-primary) 14%, transparent)',
              }}
            >
              {number}
            </a>
          </Tooltip>
        );
      })}
    </div>
  );
}

/* ------------------------------- Feedback ------------------------------- */

function Feedback({ message, conversationId }: { message: ChatMessage; conversationId: number }) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [comment, setComment] = useState('');

  const rate = useAppMutation({
    mutationFn: ({ isHelpful, text }: { isHelpful: boolean; text?: string }) =>
      chatApi.feedback(message.id, isHelpful, text),
    onSuccess: (_data, { isHelpful }) => {
      queryClient.setQueryData<ConversationDetail>(qk.conversation(conversationId), (old) =>
        old ? { ...old, messages: old.messages.map((m) => (m.id === message.id ? { ...m, helpful: isHelpful } : m)) } : old,
      );
      setOpen(false);
      setComment('');
    },
    successMessage: 'Thanks, that helps us improve the answers.',
  });

  return (
    <Space size={2} role="group" aria-label="Rate this answer">
      <Tooltip title="Helpful">
        <Button
          size="small"
          type="text"
          aria-label="This answer was helpful"
          aria-pressed={message.helpful === true}
          icon={message.helpful === true ? <LikeFilled style={{ color: 'var(--ant-color-success)' }} /> : <LikeOutlined />}
          loading={rate.isPending && rate.variables?.isHelpful === true}
          onClick={() => rate.mutate({ isHelpful: true })}
        />
      </Tooltip>

      <Popover
        trigger="click"
        open={open}
        onOpenChange={setOpen}
        title="What went wrong?"
        content={
          <div style={{ width: 280 }}>
            <Input.TextArea
              rows={3}
              maxLength={500}
              showCount
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="Optional: tell us what was missing or wrong"
              aria-label="Feedback comment"
            />
            <div style={{ marginTop: 10, display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
              <Button size="small" onClick={() => setOpen(false)}>
                Cancel
              </Button>
              <Button
                size="small"
                type="primary"
                loading={rate.isPending}
                onClick={() => rate.mutate({ isHelpful: false, text: comment.trim() || undefined })}
              >
                Send feedback
              </Button>
            </div>
          </div>
        }
      >
        <Tooltip title="Not helpful">
          <Button
            size="small"
            type="text"
            aria-label="This answer was not helpful"
            aria-pressed={message.helpful === false}
            icon={message.helpful === false ? <DislikeFilled style={{ color: 'var(--ant-color-error)' }} /> : <DislikeOutlined />}
          />
        </Tooltip>
      </Popover>
    </Space>
  );
}

/* -------------------------------- Bubbles -------------------------------- */

const botAvatar = (
  <Avatar size={32} icon={<RobotOutlined />} style={{ background: 'var(--ant-color-primary)', flex: 'none' }} />
);

export function MessageBubble({
  message,
  conversationId,
  canEscalate,
  onEscalate,
}: {
  message: ChatMessage;
  conversationId: number;
  canEscalate: boolean;
  onEscalate: () => void;
}) {
  if (message.role === 'User') {
    return (
      <div className="msg msg--user">
        <Avatar size={32} icon={<UserOutlined />} style={{ flex: 'none' }} />
        <div className="msg__bubble pre-wrap">{message.content}</div>
      </div>
    );
  }

  return (
    <div className={`msg msg--bot${message.isFallback ? ' msg--fallback' : ''}`}>
      {botAvatar}
      <div style={{ minWidth: 0, maxWidth: 'min(100%, 620px)' }}>
        <div className="msg__bubble">
          <AnswerText text={message.content} sources={message.sources} />

          {message.sources.length > 0 && (
            <div className="msg__sources">
              <strong>Sources</strong>
              <ol style={{ margin: '4px 0 0', paddingLeft: 18 }}>
                {message.sources.map((source, index) => (
                  <li key={`${source.documentId}-${index}`} id={`source-${index + 1}`}>
                    {source.title}
                    {source.section ? ` › ${source.section}` : ''}
                  </li>
                ))}
              </ol>
            </div>
          )}

          {message.isFallback && canEscalate && (
            <div style={{ marginTop: 10 }}>
              <Button type="primary" size="small" onClick={onEscalate}>
                Talk to a person
              </Button>
            </div>
          )}
        </div>

        <div className="msg__tools">
          {message.id > 0 && !message.isFallback && <Feedback message={message} conversationId={conversationId} />}
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            <TimeText value={message.createdAt} />
          </Typography.Text>
        </div>
      </div>
    </div>
  );
}

export function TypingBubble() {
  return (
    <div className="msg msg--bot" role="status" aria-label="The assistant is writing an answer">
      {botAvatar}
      <div className="msg__bubble">
        <span className="typing" aria-hidden="true">
          <span />
          <span />
          <span />
        </span>
      </div>
    </div>
  );
}
