import type { ReactNode } from 'react';
import { Button, Empty, Result } from 'antd';
import { ApiError } from '@/api/client';

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const status = error instanceof ApiError ? error.status : undefined;
  const message = error instanceof Error ? error.message : 'Something went wrong.';

  if (status === 404) {
    return <Result status="404" title="Not found" subTitle={message} />;
  }
  if (status === 403) {
    return <Result status="403" title="No access" subTitle={message} />;
  }
  return (
    <Result
      status="warning"
      title="We couldn't load this"
      subTitle={message}
      extra={onRetry && <Button type="primary" onClick={onRetry}>Try again</Button>}
    />
  );
}

export function EmptyState({ title, description, action }: { title: string; description?: ReactNode; action?: ReactNode }) {
  return (
    <Empty
      image={Empty.PRESENTED_IMAGE_SIMPLE}
      description={
        <div>
          <div style={{ fontWeight: 600 }}>{title}</div>
          {description && <div style={{ opacity: 0.7, marginTop: 2 }}>{description}</div>}
        </div>
      }
      style={{ padding: '32px 0' }}
    >
      {action}
    </Empty>
  );
}
