import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Button, Result } from 'antd';

/** Last line of defence: a render crash shows a recovery screen instead of a blank page. */
export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled UI error', error, info.componentStack);
  }

  render() {
    if (!this.state.failed) return this.props.children;
    return (
      <Result
        status="error"
        title="Something went wrong"
        subTitle="The page hit an unexpected problem. Reloading usually fixes it."
        extra={<Button type="primary" onClick={() => window.location.assign('/')}>Reload</Button>}
      />
    );
  }
}
