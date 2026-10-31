import { useMemo, useState } from 'react';
import { App, Alert, Card, Input, Segmented, Select, Space, Table, Tooltip, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { SearchOutlined } from '@ant-design/icons';
import { useChangeRole, useUsers } from '@/api/hooks';
import type { AdminUser, Role } from '@/api/types';
import { useCurrentUser } from '@/auth/AuthContext';
import { PageHeader } from '@/components/PageHeader';
import { ErrorState } from '@/components/States';
import { UserAvatar } from '@/components/UserAvatar';
import { ROLE_META } from '@/domain';
import { displayName, shortDate } from '@/utils/format';

const ROLES: Role[] = ['Customer', 'Agent', 'Admin'];

export default function UsersPage() {
  const me = useCurrentUser();
  const { modal } = App.useApp();
  const { data: users = [], isLoading, error, refetch } = useUsers();
  const changeRole = useChangeRole();

  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<Role | 'All'>('All');

  const counts = useMemo(() => {
    const result: Record<Role | 'All', number> = { All: users.length, Customer: 0, Agent: 0, Admin: 0 };
    users.forEach((u) => (result[u.role] += 1));
    return result;
  }, [users]);

  const rows = useMemo(() => {
    const term = search.trim().toLowerCase();
    return users.filter(
      (u) =>
        (roleFilter === 'All' || u.role === roleFilter) &&
        (!term || u.email.toLowerCase().includes(term) || (u.fullName ?? '').toLowerCase().includes(term)),
    );
  }, [users, search, roleFilter]);

  const confirmChange = (user: AdminUser, role: Role) => {
    modal.confirm({
      title: `Make ${displayName(user)} ${role === 'Admin' ? 'an' : 'a'} ${role.toLowerCase()}?`,
      content:
        role === 'Admin'
          ? 'Admins can manage every ticket, user and knowledge document.'
          : role === 'Agent'
            ? 'Agents see and handle the tickets assigned to them.'
            : 'Customers can open tickets and use the assistant. Agents and admins lose their staff access.',
      okText: `Change to ${role}`,
      onOk: () => changeRole.mutateAsync({ userId: user.id, role }).catch(() => undefined),
    });
  };

  const columns: ColumnsType<AdminUser> = [
    {
      title: 'User',
      key: 'user',
      render: (_, user) => (
        <Space size={12}>
          <UserAvatar name={displayName(user)} />
          <div style={{ lineHeight: 1.3 }}>
            <div style={{ fontWeight: 600 }}>
              {displayName(user)}
              {user.id === me.userId && (
                <Typography.Text type="secondary" style={{ marginLeft: 8, fontWeight: 400 }}>
                  (you)
                </Typography.Text>
              )}
            </div>
            {user.fullName && <Typography.Text type="secondary">{user.email}</Typography.Text>}
          </div>
        </Space>
      ),
    },
    {
      title: 'Role',
      key: 'role',
      width: 200,
      render: (_, user) => {
        const self = user.id === me.userId;
        const select = (
          <Select<Role>
            value={user.role}
            disabled={self}
            style={{ width: 150 }}
            aria-label={`Role of ${displayName(user)}`}
            options={ROLES.map((r) => ({ value: r, label: ROLE_META[r].label }))}
            onChange={(role) => role !== user.role && confirmChange(user, role)}
          />
        );
        return self ? <Tooltip title="You can’t change your own role.">{select}</Tooltip> : select;
      },
    },
    { title: 'Joined', key: 'joined', width: 140, responsive: ['md'], render: (_, user) => shortDate(user.createdAt) },
  ];

  return (
    <div className="page">
      <PageHeader
        title="Users"
        subtitle="Everyone who registered starts as a customer. Promote people to agent or admin here."
      />

      <Card styles={{ body: { padding: 0 } }}>
        <div style={{ padding: 16, display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between' }}>
          <Segmented<Role | 'All'>
            value={roleFilter}
            onChange={setRoleFilter}
            options={(['All', ...ROLES] as const).map((r) => ({
              value: r,
              label: `${r === 'All' ? 'Everyone' : ROLE_META[r].label} · ${counts[r]}`,
            }))}
          />
          <Input
            allowClear
            prefix={<SearchOutlined />}
            placeholder="Search name or email"
            aria-label="Search users"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ width: 280, maxWidth: '100%' }}
          />
        </div>

        {error ? (
          <ErrorState error={error} onRetry={() => void refetch()} />
        ) : (
          <Table<AdminUser>
            rowKey="id"
            columns={columns}
            dataSource={rows}
            loading={isLoading || changeRole.isPending}
            pagination={{ pageSize: 15, hideOnSinglePage: true, style: { padding: '0 16px' } }}
            locale={{ emptyText: 'No users match your search.' }}
          />
        )}
      </Card>

      <Alert
        style={{ marginTop: 16 }}
        type="info"
        showIcon
        message="The person may need to sign out and back in before a role change takes effect."
      />
    </div>
  );
}
