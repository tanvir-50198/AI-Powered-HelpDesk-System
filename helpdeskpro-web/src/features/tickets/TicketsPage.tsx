import { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Button, Card, Input, Select, Space, Switch, Typography } from 'antd';
import { PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { useCategories, useTickets } from '@/api/hooks';
import type { TicketPriority, TicketQuery, TicketSort, TicketStatus } from '@/api/types';
import { useCurrentUser } from '@/auth/AuthContext';
import { PageHeader } from '@/components/PageHeader';
import { EmptyState, ErrorState } from '@/components/States';
import { SORT_OPTIONS, PRIORITY_META, STATUS_META, TICKET_PRIORITIES, TICKET_STATUSES } from '@/domain';
import { useDebounce } from '@/hooks/useDebounce';
import { TicketTable } from './TicketTable';

const TITLES = { Customer: 'My tickets', Agent: 'Assigned tickets', Admin: 'All tickets' } as const;
const SUBTITLES = {
  Customer: 'Everything you have asked us for help with.',
  Agent: 'Tickets assigned to you, highest priority first when you sort that way.',
  Admin: 'Every ticket in the system. Filter to unassigned to triage new work.',
} as const;

function readQuery(params: URLSearchParams): TicketQuery {
  const number = (key: string) => {
    const value = Number(params.get(key));
    return Number.isFinite(value) && value > 0 ? value : undefined;
  };
  const status = params.get('status') as TicketStatus | null;
  const priority = params.get('priority') as TicketPriority | null;
  return {
    search: params.get('search') ?? undefined,
    status: status && TICKET_STATUSES.includes(status) ? status : undefined,
    priority: priority && TICKET_PRIORITIES.includes(priority) ? priority : undefined,
    categoryId: number('categoryId'),
    onlyUnassigned: params.get('unassigned') === '1' || undefined,
    sortBy: (params.get('sort') as TicketSort | null) ?? 'newest',
    page: number('page') ?? 1,
    pageSize: number('pageSize') ?? 10,
  };
}

export default function TicketsPage() {
  const user = useCurrentUser();
  const [params, setParams] = useSearchParams();
  const query = useMemo(() => readQuery(params), [params]);

  // The text box is local state so typing is instant; the URL (and the request) follow after a pause.
  const [searchText, setSearchText] = useState(query.search ?? '');
  const debouncedSearch = useDebounce(searchText);

  const update = (patch: Record<string, string | number | boolean | undefined>, keepPage = false) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(patch)) {
      if (value === undefined || value === '' || value === false) next.delete(key);
      else next.set(key, value === true ? '1' : String(value));
    }
    if (!keepPage) next.delete('page');
    setParams(next, { replace: true });
  };

  useEffect(() => {
    if ((debouncedSearch.trim() || undefined) !== (query.search || undefined)) {
      update({ search: debouncedSearch.trim() || undefined });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch]);

  const { data, isLoading, isFetching, error, refetch } = useTickets(query);
  const { data: categories = [] } = useCategories();

  const hasFilters = !!(query.search || query.status || query.priority || query.categoryId || query.onlyUnassigned);
  const clearFilters = () => {
    setSearchText('');
    setParams(new URLSearchParams(), { replace: true });
  };

  return (
    <div className="page">
      <PageHeader
        title={TITLES[user.role]}
        subtitle={SUBTITLES[user.role]}
        actions={
          user.role === 'Customer' && (
            <Link to="/tickets/new">
              <Button type="primary" icon={<PlusOutlined />}>
                New ticket
              </Button>
            </Link>
          )
        }
      />

      <Card styles={{ body: { padding: 0 } }}>
        <div style={{ padding: 16, borderBottom: '1px solid var(--ant-color-border-secondary)' }}>
          <Space wrap size={[10, 10]} style={{ width: '100%' }}>
            <Input
              allowClear
              prefix={<SearchOutlined />}
              placeholder="Search title or description"
              aria-label="Search tickets"
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              style={{ width: 280, maxWidth: '100%' }}
            />
            <Select
              allowClear
              placeholder="Status"
              aria-label="Filter by status"
              value={query.status}
              onChange={(value) => update({ status: value })}
              style={{ width: 140 }}
              options={TICKET_STATUSES.map((s) => ({ value: s, label: STATUS_META[s].label }))}
            />
            <Select
              allowClear
              placeholder="Priority"
              aria-label="Filter by priority"
              value={query.priority}
              onChange={(value) => update({ priority: value })}
              style={{ width: 140 }}
              options={TICKET_PRIORITIES.map((p) => ({ value: p, label: PRIORITY_META[p].label }))}
            />
            <Select
              allowClear
              placeholder="Category"
              aria-label="Filter by category"
              value={query.categoryId}
              onChange={(value) => update({ categoryId: value })}
              style={{ width: 180 }}
              options={categories.map((c) => ({ value: c.id, label: c.name }))}
            />
            <Select
              aria-label="Sort tickets"
              value={query.sortBy}
              onChange={(value) => update({ sort: value === 'newest' ? undefined : value })}
              style={{ width: 160 }}
              options={SORT_OPTIONS}
            />
            {user.role === 'Admin' && (
              <label style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
                <Switch checked={!!query.onlyUnassigned} onChange={(on) => update({ unassigned: on })} />
                <span>Unassigned only</span>
              </label>
            )}
            {hasFilters && (
              <Button type="link" onClick={clearFilters}>
                Clear filters
              </Button>
            )}
          </Space>
        </div>

        {error ? (
          <ErrorState error={error} onRetry={() => void refetch()} />
        ) : (
          <TicketTable
            tickets={data?.items ?? []}
            role={user.role}
            loading={isLoading || isFetching}
            pagination={{
              current: data?.page ?? query.page,
              pageSize: data?.pageSize ?? query.pageSize,
              total: data?.totalCount ?? 0,
              showSizeChanger: true,
              pageSizeOptions: [10, 20, 50],
              showTotal: (total, [from, to]) => (total ? `${from}–${to} of ${total}` : ''),
              hideOnSinglePage: (data?.totalCount ?? 0) <= 10,
              onChange: (page, pageSize) =>
                update({ page: page === 1 ? undefined : page, pageSize: pageSize === 10 ? undefined : pageSize }, true),
              style: { padding: '0 16px' },
            }}
            empty={
              hasFilters ? (
                <EmptyState
                  title="No tickets match these filters"
                  description="Try a different search or remove a filter."
                  action={<Button onClick={clearFilters}>Clear filters</Button>}
                />
              ) : user.role === 'Customer' ? (
                <EmptyState
                  title="You haven't opened a ticket yet"
                  description="Ask the AI assistant first. It can often answer straight away."
                  action={
                    <Space>
                      <Link to="/assistant">
                        <Button>Ask the assistant</Button>
                      </Link>
                      <Link to="/tickets/new">
                        <Button type="primary">Create a ticket</Button>
                      </Link>
                    </Space>
                  }
                />
              ) : (
                <EmptyState
                  title={user.role === 'Agent' ? 'Nothing is assigned to you right now' : 'There are no tickets yet'}
                  description={
                    <Typography.Text type="secondary">New tickets will show up here.</Typography.Text>
                  }
                />
              )
            }
          />
        )}
      </Card>
    </div>
  );
}
