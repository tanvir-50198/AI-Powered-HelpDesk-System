import { useState } from 'react';
import { Alert, App, Form, Input, Modal, Select, Upload, Typography } from 'antd';
import type { UploadFile } from 'antd';
import { InboxOutlined } from '@ant-design/icons';
import { useDocumentActions } from '@/api/hooks';
import type { DocumentAudience } from '@/api/types';
import { ACCEPTED_DOC_TYPES, AUDIENCE_META, MAX_UPLOAD_MB } from '@/domain';

interface FormValues {
  files: UploadFile[];
  title?: string;
  category?: string;
  audience: DocumentAudience;
}

export function UploadDocumentModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { message } = App.useApp();
  const [form] = Form.useForm<FormValues>();
  const { upload } = useDocumentActions();
  const [error, setError] = useState<string | null>(null);

  const submit = (values: FormValues) => {
    const file = values.files?.[0]?.originFileObj;
    if (!file) return;
    setError(null);
    upload.mutate(
      { file, title: values.title?.trim(), category: values.category?.trim(), audience: values.audience },
      {
        onSuccess: (doc) => {
          if (doc.processingStatus === 'Failed') {
            message.warning(`Uploaded, but indexing failed: ${doc.processingError ?? 'unknown error'}`, 6);
          } else {
            message.success('Uploaded and indexed. Approve it so the assistant can use it.');
          }
          form.resetFields();
          onClose();
        },
        onError: (e) => setError(e.message),
      },
    );
  };

  return (
    <Modal
      open={open}
      title="Upload a document"
      okText="Upload"
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={upload.isPending}
      destroyOnHidden
      afterOpenChange={(v) => v && setError(null)}
    >
      <Typography.Paragraph type="secondary">
        New documents start as drafts. The assistant only uses them after you approve them.
      </Typography.Paragraph>
      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 16 }} role="alert" />}

      <Form<FormValues> form={form} layout="vertical" requiredMark={false} initialValues={{ audience: 'Customer' }} onFinish={submit}>
        <Form.Item
          name="files"
          valuePropName="fileList"
          getValueFromEvent={(e: { fileList: UploadFile[] }) => e.fileList}
          rules={[{ required: true, message: 'Choose a file to upload.' }]}
        >
          <Upload.Dragger
            maxCount={1}
            accept={ACCEPTED_DOC_TYPES.join(',')}
            beforeUpload={(file) => {
              if (file.size > MAX_UPLOAD_MB * 1024 * 1024) {
                message.error(`${file.name} is larger than ${MAX_UPLOAD_MB} MB.`);
                return Upload.LIST_IGNORE;
              }
              return false; // keep the file in the form, upload on submit
            }}
          >
            <p className="ant-upload-drag-icon">
              <InboxOutlined />
            </p>
            <p className="ant-upload-text">Drop a file here, or click to browse</p>
            <p className="ant-upload-hint">
              {ACCEPTED_DOC_TYPES.join(', ')} · up to {MAX_UPLOAD_MB} MB
            </p>
          </Upload.Dragger>
        </Form.Item>

        <Form.Item label="Title" name="title" extra="Optional. Defaults to the file name." rules={[{ max: 250 }]}>
          <Input />
        </Form.Item>
        <Form.Item label="Category" name="category" rules={[{ max: 100 }]}>
          <Input placeholder="e.g. faq, policies, troubleshooting" />
        </Form.Item>
        <Form.Item label="Who can read it through the assistant?" name="audience" style={{ marginBottom: 0 }}>
          <Select
            options={(Object.keys(AUDIENCE_META) as DocumentAudience[]).map((a) => ({
              value: a,
              label: `${AUDIENCE_META[a].label} – ${AUDIENCE_META[a].hint}`,
            }))}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}
