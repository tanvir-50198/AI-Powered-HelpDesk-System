import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Alert, Button, Checkbox, Divider, Form, Input, Typography } from 'antd';
import { LockOutlined, MailOutlined } from '@ant-design/icons';
import { useAuth } from '@/auth/AuthContext';
import { AuthLayout } from '@/layouts/AuthLayout';
import { DEMO_ACCOUNTS } from '@/domain';
import type { LoginRequest } from '@/api/types';

const SHOW_DEMO = import.meta.env.VITE_SHOW_DEMO_ACCOUNTS === 'true';

/** Only follow in-app paths (blocks open-redirects like //evil.com). */
function safeReturnUrl(value: string | null) {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/dashboard';
}

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [form] = Form.useForm<LoginRequest>();
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const submit = async (values: LoginRequest) => {
    setError(null);
    setSubmitting(true);
    try {
      await login({ ...values, rememberMe: !!values.rememberMe });
      navigate(safeReturnUrl(params.get('returnUrl')), { replace: true });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <Typography.Title level={2} style={{ marginBottom: 4 }}>
        Sign in
      </Typography.Title>
      <Typography.Paragraph type="secondary">Welcome back. Enter your details to continue.</Typography.Paragraph>

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 16 }} role="alert" />}

      <Form form={form} layout="vertical" requiredMark={false} onFinish={submit} initialValues={{ rememberMe: false }}>
        <Form.Item
          label="Email"
          name="email"
          rules={[
            { required: true, message: 'Enter your email address.' },
            { type: 'email', message: 'Enter a valid email address.' },
          ]}
        >
          <Input prefix={<MailOutlined />} size="large" autoComplete="username" placeholder="you@company.com" autoFocus />
        </Form.Item>

        <Form.Item label="Password" name="password" rules={[{ required: true, message: 'Enter your password.' }]}>
          <Input.Password prefix={<LockOutlined />} size="large" autoComplete="current-password" />
        </Form.Item>

        <Form.Item name="rememberMe" valuePropName="checked">
          <Checkbox>Keep me signed in on this device</Checkbox>
        </Form.Item>

        <Button type="primary" htmlType="submit" size="large" block loading={submitting}>
          Sign in
        </Button>
      </Form>

      <Typography.Paragraph style={{ marginTop: 20, textAlign: 'center' }}>
        New here? <Link to="/register">Create a customer account</Link>
      </Typography.Paragraph>

      {SHOW_DEMO && (
        <>
          <Divider plain style={{ fontSize: 12 }}>
            Demo accounts
          </Divider>
          <div className="demo-accounts">
            {DEMO_ACCOUNTS.map((account) => (
              <Button
                key={account.role}
                size="small"
                disabled={submitting}
                onClick={() => {
                  form.setFieldsValue({ email: account.email, password: account.password });
                  void submit({ email: account.email, password: account.password, rememberMe: false });
                }}
              >
                Sign in as {account.role.toLowerCase()}
              </Button>
            ))}
          </div>
        </>
      )}
    </AuthLayout>
  );
}
