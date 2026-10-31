import { useMemo, useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Button, Drawer, Dropdown, Grid, Layout, Menu, Space, Tooltip, Typography, type MenuProps } from 'antd';
import {
  BarChartOutlined,
  BookOutlined,
  DashboardOutlined,
  InboxOutlined,
  LogoutOutlined,
  MenuOutlined,
  MessageOutlined,
  MoonOutlined,
  PlusCircleOutlined,
  SunOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import type { Role } from '@/api/types';
import { useAuth, useCurrentUser } from '@/auth/AuthContext';
import { Brand } from '@/components/Brand';
import { RoleTag } from '@/components/Tags';
import { UserAvatar } from '@/components/UserAvatar';
import { useThemeMode } from '@/theme';
import { displayName } from '@/utils/format';

interface NavItem {
  key: string;
  label: string;
  icon: React.ReactNode;
}

const NAV: Record<Role, NavItem[]> = {
  Customer: [
    { key: '/dashboard', label: 'Dashboard', icon: <DashboardOutlined /> },
    { key: '/tickets', label: 'My tickets', icon: <InboxOutlined /> },
    { key: '/tickets/new', label: 'New ticket', icon: <PlusCircleOutlined /> },
    { key: '/assistant', label: 'AI assistant', icon: <MessageOutlined /> },
  ],
  Agent: [
    { key: '/dashboard', label: 'Dashboard', icon: <DashboardOutlined /> },
    { key: '/tickets', label: 'Assigned tickets', icon: <InboxOutlined /> },
  ],
  Admin: [
    { key: '/dashboard', label: 'Dashboard', icon: <DashboardOutlined /> },
    { key: '/tickets', label: 'All tickets', icon: <InboxOutlined /> },
    { key: '/admin/users', label: 'Users', icon: <TeamOutlined /> },
    { key: '/admin/knowledge', label: 'Knowledge base', icon: <BookOutlined /> },
    { key: '/admin/reports', label: 'Reports', icon: <BarChartOutlined /> },
  ],
};

/** The nav item whose key is the longest prefix of the current path. */
function activeKey(items: NavItem[], pathname: string) {
  const match = items
    .filter((item) => pathname === item.key || pathname.startsWith(item.key + '/'))
    .sort((a, b) => b.key.length - a.key.length)[0];
  return match?.key;
}

function SideNav({ onNavigate }: { onNavigate?: () => void }) {
  const user = useCurrentUser();
  const location = useLocation();
  const navigate = useNavigate();
  const items = NAV[user.role];

  const menuItems: MenuProps['items'] = items.map(({ key, label, icon }) => ({ key, label, icon }));

  return (
    <>
      <Brand />
      <Menu
        theme="dark"
        mode="inline"
        aria-label="Main navigation"
        selectedKeys={[activeKey(items, location.pathname) ?? '']}
        items={menuItems}
        onClick={({ key }) => {
          navigate(key);
          onNavigate?.();
        }}
        style={{ border: 0, background: 'transparent', marginTop: 6 }}
      />
      <div className="sider-foot">Times are shown in your local time zone.</div>
    </>
  );
}

export default function AppLayout() {
  const screens = Grid.useBreakpoint();
  const isDesktop = !!screens.lg;
  const [drawerOpen, setDrawerOpen] = useState(false);
  const { mode, toggle } = useThemeMode();
  const { logout } = useAuth();
  const user = useCurrentUser();
  const navigate = useNavigate();

  const name = displayName(user);

  const userMenu = useMemo<MenuProps>(
    () => ({
      items: [
        {
          key: 'who',
          disabled: true,
          label: (
            <div style={{ lineHeight: 1.35 }}>
              <div style={{ fontWeight: 600 }}>{name}</div>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {user.email}
              </Typography.Text>
            </div>
          ),
        },
        { type: 'divider' },
        { key: 'logout', icon: <LogoutOutlined />, label: 'Sign out', danger: true },
      ],
      onClick: async ({ key }) => {
        if (key === 'logout') {
          await logout();
          navigate('/login', { replace: true });
        }
      },
    }),
    [name, user.email, logout, navigate],
  );

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <a className="skip-link" href="#main">
        Skip to content
      </a>

      {isDesktop ? (
        <Layout.Sider className="app-sider" width={248} theme="dark">
          <SideNav />
        </Layout.Sider>
      ) : (
        <Drawer
          placement="left"
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          width={264}
          closable={false}
          styles={{ body: { padding: 0, background: '#0b2f33', display: 'flex', flexDirection: 'column' } }}
        >
          <SideNav onNavigate={() => setDrawerOpen(false)} />
        </Drawer>
      )}

      <Layout>
        <Layout.Header className="app-header">
          <div>
            {!isDesktop && (
              <Button
                type="text"
                aria-label="Open navigation"
                icon={<MenuOutlined />}
                onClick={() => setDrawerOpen(true)}
              />
            )}
          </div>

          <Space size={4}>
            <Tooltip title={mode === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}>
              <Button
                type="text"
                shape="circle"
                aria-label={mode === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
                icon={mode === 'dark' ? <SunOutlined /> : <MoonOutlined />}
                onClick={toggle}
              />
            </Tooltip>

            <Dropdown menu={userMenu} trigger={['click']} placement="bottomRight">
              <Button type="text" style={{ height: 44, paddingInline: 8 }} aria-label="Account menu">
                <Space size={10}>
                  <UserAvatar name={name} size={30} />
                  {screens.sm && (
                    <span style={{ textAlign: 'left', lineHeight: 1.2 }}>
                      <span style={{ display: 'block', fontWeight: 600 }}>{name}</span>
                      <span style={{ display: 'block', marginTop: 2 }}>
                        <RoleTag role={user.role} />
                      </span>
                    </span>
                  )}
                </Space>
              </Button>
            </Dropdown>
          </Space>
        </Layout.Header>

        <Layout.Content id="main" tabIndex={-1}>
          <Outlet />
        </Layout.Content>
      </Layout>
    </Layout>
  );
}
