import type { ReactNode } from 'react';
import { Breadcrumb, Typography } from 'antd';
import { Link } from 'react-router-dom';

interface Props {
  title: ReactNode;
  subtitle?: ReactNode;
  actions?: ReactNode;
  breadcrumb?: { label: string; to?: string }[];
  /** Rendered next to the title (e.g. status tags). */
  meta?: ReactNode;
}

export function PageHeader({ title, subtitle, actions, breadcrumb, meta }: Props) {
  return (
    <header className="page-header">
      {breadcrumb && (
        <Breadcrumb
          style={{ marginBottom: 8 }}
          items={breadcrumb.map((item) => ({
            title: item.to ? <Link to={item.to}>{item.label}</Link> : item.label,
          }))}
        />
      )}
      <div className="page-header__row">
        <div className="page-header__text">
          <div className="page-header__title">
            <Typography.Title level={2} style={{ margin: 0 }}>
              {title}
            </Typography.Title>
            {meta}
          </div>
          {subtitle && (
            <Typography.Paragraph type="secondary" style={{ margin: '4px 0 0', maxWidth: 680 }}>
              {subtitle}
            </Typography.Paragraph>
          )}
        </div>
        {actions && <div className="page-header__actions">{actions}</div>}
      </div>
    </header>
  );
}
