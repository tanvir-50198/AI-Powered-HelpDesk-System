import { Tooltip } from 'antd';
import { fromNow, fullDate } from '@/utils/format';

/** "3 hours ago" with the exact local time on hover. */
export function TimeText({ value }: { value: string }) {
  return (
    <Tooltip title={fullDate(value)}>
      <time dateTime={value} style={{ whiteSpace: 'nowrap' }}>
        {fromNow(value)}
      </time>
    </Tooltip>
  );
}
