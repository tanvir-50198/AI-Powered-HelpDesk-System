import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Alert, Button, Form, Input, Typography } from 'antd';
import { LockOutlined, MailOutlined, UserOutlined } from '@ant-design/icons';
import { useAuth } from '@/auth/AuthContext';
import { AuthLayout } from '@/layouts/AuthLayout';
import type { RegisterRequest } from '@/api/types';

export default function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const submit = async (values: RegisterRequest) => {
    setError(null);
    setSubmitting(true);
    try {
      await register(values);
      navigate('/dashboard', { replace: true });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <AuthLayout>
      <Typography.Title level={2} style={{ marginBottom: 4 }}>
        Create your account
      </Typography.Title>
      <Typography.Paragraph type="secondary">
        Open tickets, follow their progress and chat with the assistant.
      </Typography.Paragraph>

      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 16 }} role="alert" />}

      <Form layout="vertical" requiredMark={false} onFinish={submit}>
        <Form.Item
          label="Full name"
          name="fullName"
          rules={[
            { required: true, message: 'Enter your full name.' },
            { max: 100, message: 'Use 100 characters or fewer.' },
          ]}
        >
          <Input prefix={<UserOutlined />} size="large" autoComplete="name" autoFocus />
        </Form.Item>

        <Form.Item
          label="Email"
          name="email"
          rules={[
            { required: true, message: 'Enter your email address.' },
            { type: 'email', message: 'Enter a valid email address.' },
          ]}
        >
          <Input prefix={<MailOutlined />} size="large" autoComplete="email" />
        </Form.Item>

        {/* Same rules as Program.cs: 8+ characters, one digit, one upper-case, one lower-case letter */}
        <Form.Item
          label="Password"
          name="password"
          extra="At least 8 characters with an upper-case letter, a lower-case letter and a number."
          rules={[
            { required: true, message: 'Choose a password.' },
            { min: 8, message: 'Use at least 8 characters.' },
            { pattern: /[A-Z]/, message: 'Add an upper-case letter.' },
            { pattern: /[a-z]/, message: 'Add a lower-case letter.' },
            { pattern: /\d/, message: 'Add a number.' },
          ]}
        >
          <Input.Password prefix={<LockOutlined />} size="large" autoComplete="new-password" />
        </Form.Item>

        <Form.Item
          label="Confirm password"
          name="confirmPassword"
          dependencies={['password']}
          rules={[
            { required: true, message: 'Repeat your password.' },
            ({ getFieldValue }) => ({
              validator: (_, value) =>
                !value || getFieldValue('password') === value
                  ? Promise.resolve()
                  : Promise.reject(new Error('The passwords do not match.')),
            }),
          ]}
        >
          <Input.Password prefix={<LockOutlined />} size="large" autoComplete="new-password" />
        </Form.Item>

        <Button type="primary" htmlType="submit" size="large" block loading={submitting}>
          Create account
        </Button>
      </Form>

      <Typography.Paragraph style={{ marginTop: 20, textAlign: 'center' }}>
        Already have an account? <Link to="/login">Sign in</Link>
      </Typography.Paragraph>
    </AuthLayout>
  );
}
