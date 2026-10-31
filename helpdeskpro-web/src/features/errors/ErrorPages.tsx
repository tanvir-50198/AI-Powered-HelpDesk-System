import { Link } from 'react-router-dom';
import { Button, Result } from 'antd';

export function NotFoundPage() {
  return (
    <div className="page">
      <Result status="404" title="Page not found" subTitle="The page you’re looking for doesn’t exist or was moved." extra={<Link to="/dashboard"><Button type="primary">Back to dashboard</Button></Link>} />
    </div>
  );
}

export function ForbiddenPage() {
  return (
    <div className="page">
      <Result status="403" title="You don’t have access to this page" subTitle="Your role doesn’t include this area. Ask an admin if you need it." extra={<Link to="/dashboard"><Button type="primary">Back to dashboard</Button></Link>} />
    </div>
  );
}
