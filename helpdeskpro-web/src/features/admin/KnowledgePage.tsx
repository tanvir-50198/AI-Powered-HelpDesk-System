import { useMemo, useState } from 'react';
import { Button, Card, Col, Input, Popconfirm, Row, Select, Space, Table, Tag, Tooltip, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { CheckOutlined, CloseOutlined, DeleteOutlined, ImportOutlined, ReloadOutlined, SearchOutlined, UploadOutlined } from '@ant-design/icons';
import { useDocumentActions, useDocuments } from '@/api/hooks';
import type { ApprovalStatus, KnowledgeDocument } from '@/api/types';
import { PageHeader } from '@/components/PageHeader';
import { StatCard } from '@/components/StatCard';
import { ErrorState } from '@/components/States';
import { ApprovalTag, ProcessingTag } from '@/components/Tags';
import { TimeText } from '@/components/TimeText';
import { APPROVAL_META, AUDIENCE_META } from '@/domain';
import { UploadDocumentModal } from './UploadDocumentModal';

export default function KnowledgePage() {
  const { data: docs = [], isLoading, error, refetch } = useDocuments();
  const actions = useDocumentActions();
  const [uploadOpen, setUploadOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [approval, setApproval] = useState<ApprovalStatus>();
  const [category, setCategory] = useState<string>();

  const categories = useMemo(() => [...new Set(docs.map((d) => d.category).filter((c): c is string => !!c))].sort(), [docs]);

  const rows = useMemo(() => {
    const term = search.trim().toLowerCase();
    return docs.filter(
      (d) =>
        (!approval || d.approvalStatus === approval) &&
        (!category || d.category === category) &&
        (!term || d.title.toLowerCase().includes(term)),
    );
  }, [docs, search, approval, category]);

  const count = (fn: (d: KnowledgeDocument) => boolean) => docs.filter(fn).length;
  const busyId = [actions.approve, actions.reject, actions.reindex, actions.remove].find((m) => m.isPending)?.variables as number | undefined;

  const columns: ColumnsType<KnowledgeDocument> = [
    {
      title: 'Document',
      key: 'title',
      render: (_, d) => (
        <div>
          <div style={{ fontWeight: 600 }}>{d.title}</div>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {d.category ?? 'No category'}
            {d.sourceType && <> · .{d.sourceType}</>} · v{d.version}
          </Typography.Text>
        </div>
      ),
    },
    {
      title: 'Audience',
      key: 'audience',
      width: 120,
      responsive: ['lg'],
      render: (_, d) => (
        <Tooltip title={AUDIENCE_META[d.audience].hint}>
          <Tag bordered={false}>{AUDIENCE_META[d.audience].label}</Tag>
        </Tooltip>
      ),
    },
    { title: 'Approval', key: 'approval', width: 110, render: (_, d) => <ApprovalTag status={d.approvalStatus} /> },
    { title: 'Indexing', key: 'processing', width: 110, render: (_, d) => <ProcessingTag status={d.processingStatus} error={d.processingError} /> },
    { title: 'Updated', key: 'updated', width: 130, responsive: ['md'], render: (_, d) => <TimeText value={d.updatedAt} /> },
    {
      title: 'Actions',
      key: 'actions',
      width: 168,
      render: (_, d) => {
        const busy = busyId === d.id;
        const canApprove = d.processingStatus === 'Indexed' && d.approvalStatus !== 'Approved';
        return (
          <Space size={2}>
            <Tooltip title={d.processingStatus !== 'Indexed' ? 'Index the document before approving' : 'Approve'}>
              <Button type="text" aria-label={`Approve ${d.title}`} icon={<CheckOutlined />} disabled={!canApprove || busy} onClick={() => actions.approve.mutate(d.id)} style={canApprove ? { color: 'var(--ant-color-success)' } : undefined} />
            </Tooltip>
            <Tooltip title="Reject">
              <Button type="text" aria-label={`Reject ${d.title}`} icon={<CloseOutlined />} disabled={d.approvalStatus === 'Rejected' || busy} onClick={() => actions.reject.mutate(d.id)} />
            </Tooltip>
            <Tooltip title="Re-index">
              <Button type="text" aria-label={`Re-index ${d.title}`} icon={<ReloadOutlined />} disabled={busy} onClick={() => actions.reindex.mutate(d.id)} />
            </Tooltip>
            <Popconfirm title="Delete this document?" description="Its indexed text is removed too. This can’t be undone." okText="Delete" okButtonProps={{ danger: true }} onConfirm={() => actions.remove.mutate(d.id)}>
              <Button type="text" danger aria-label={`Delete ${d.title}`} icon={<DeleteOutlined />} disabled={busy} />
            </Popconfirm>
          </Space>
        );
      },
    },
  ];

  return (
    <div className="page">
      <PageHeader
        title="Knowledge base"
        subtitle="The assistant answers only from approved, indexed documents."
        actions={
          <>
            <Button icon={<ImportOutlined />} loading={actions.importFolder.isPending} onClick={() => actions.importFolder.mutate()}>
              Import from folder
            </Button>
            <Button type="primary" icon={<UploadOutlined />} onClick={() => setUploadOpen(true)}>
              Upload document
            </Button>
          </>
        }
      />

      <Row gutter={[16, 16]} style={{ marginBottom: 20 }}>
        <Col xs={12} lg={6}><StatCard label="Documents" value={docs.length} loading={isLoading} /></Col>
        <Col xs={12} lg={6}><StatCard label="Approved" value={count((d) => d.approvalStatus === 'Approved')} tone="#1f8f5f" loading={isLoading} /></Col>
        <Col xs={12} lg={6}><StatCard label="Waiting for approval" value={count((d) => d.approvalStatus === 'Draft')} tone="#b7791f" loading={isLoading} /></Col>
        <Col xs={12} lg={6}><StatCard label="Failed to index" value={count((d) => d.processingStatus === 'Failed')} tone="#c0262d" loading={isLoading} /></Col>
      </Row>

      <Card styles={{ body: { padding: 0 } }}>
        <div style={{ padding: 16 }}>
          <Space wrap size={[10, 10]}>
            <Input allowClear prefix={<SearchOutlined />} placeholder="Search by title" aria-label="Search documents" value={search} onChange={(e) => setSearch(e.target.value)} style={{ width: 260 }} />
            <Select allowClear placeholder="Approval" aria-label="Filter by approval" value={approval} onChange={setApproval} style={{ width: 150 }} options={(Object.keys(APPROVAL_META) as ApprovalStatus[]).map((a) => ({ value: a, label: APPROVAL_META[a].label }))} />
            <Select allowClear placeholder="Category" aria-label="Filter by category" value={category} onChange={setCategory} style={{ width: 180 }} options={categories.map((c) => ({ value: c, label: c }))} />
          </Space>
        </div>
        {error ? (
          <ErrorState error={error} onRetry={() => void refetch()} />
        ) : (
          <Table<KnowledgeDocument>
            rowKey="id"
            columns={columns}
            dataSource={rows}
            loading={isLoading}
            pagination={{ pageSize: 15, hideOnSinglePage: true, style: { padding: '0 16px' } }}
            locale={{ emptyText: docs.length ? 'No documents match these filters.' : 'No documents yet. Upload one or import the KnowledgeBase folder.' }}
          />
        )}
      </Card>

      <UploadDocumentModal open={uploadOpen} onClose={() => setUploadOpen(false)} />
    </div>
  );
}
