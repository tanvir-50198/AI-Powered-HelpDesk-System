import type { ReactNode } from 'react';
import { Typography } from 'antd';
import { Brand } from '@/components/Brand';

/** Split screen used by Login and Register. */
export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="auth-shell">
      <aside className="auth-aside" aria-hidden={false}>
        <Brand to="/login" />

        <div>
          <Typography.Title level={2}>Ask once. Get answers you can trust.</Typography.Title>
          <p>
            Search our knowledge base with the AI assistant, and hand over to a person the moment it can’t help. Every
            answer shows where it came from.
          </p>
        </div>

        <div className="stub-stack" aria-hidden="true">
          <div className="stub">
            <span className="stub__id">#1042</span>
            <span>Can’t sign in after password change</span>
            <span className="stub__pill stub__pill--warm">In progress</span>
          </div>
          <div className="stub">
            <span className="stub__id">#1041</span>
            <span>How do I export my tickets?</span>
            <span className="stub__pill">Resolved</span>
          </div>
          <div className="stub">
            <span className="stub__id">#1039</span>
            <span>Invoice shows the wrong address</span>
            <span className="stub__pill stub__pill--muted">Closed</span>
          </div>
        </div>
      </aside>

      <main className="auth-main" id="main">
        <div className="auth-card">{children}</div>
      </main>
    </div>
  );
}
