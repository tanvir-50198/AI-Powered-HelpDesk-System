import type { ReactNode } from 'react';
import { Card, Skeleton, Typography } from 'antd';
import { Link } from 'react-router-dom';

interface Props {
  label: string;
  value: ReactNode;
  icon?: ReactNode;
  /** Small coloured square behind the icon. */
  tone?: string;
  to?: string;
  loading?: boolean;
  footnote?: ReactNode;
}

export function StatCard({ label, value, icon, tone = 'var(--ant-color-primary)', to, loading, footnote }: Props) {
  const body = (
    <Card className={`stat-card${to ? ' stat-card--link' : ''}`} styles={{ body: { padding: 18 } }}>
      {loading ? (
        <Skeleton active paragraph={{ rows: 1 }} title={{ width: '50%' }} />
      ) : (
        <div className="stat-card__inner">
          <div>
            <Typography.Text type="secondary">{label}</Typography.Text>
            <div className="stat-card__value">{value}</div>
            {footnote && (
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {footnote}
              </Typography.Text>
            )}
          </div>
          {icon && (
            <span className="stat-card__icon" style={{ color: tone, background: `color-mix(in srgb, ${tone} 12%, transparent)` }}>
              {icon}
            </span>
          )}
        </div>
      )}
    </Card>
  );
  return to ? (
    <Link to={to} aria-label={`${label}: view tickets`} style={{ display: 'block' }}>
      {body}
    </Link>
  ) : (
    body
  );
}
