import { Avatar } from 'antd';
import { initials } from '@/utils/format';

const TONES = ['#0e6f73', '#2a6fc4', '#7a4fb3', '#b5560f', '#1f8f5f', '#a63d6b'];

function toneFor(text: string) {
  let hash = 0;
  for (const char of text) hash = (hash * 31 + char.charCodeAt(0)) >>> 0;
  return TONES[hash % TONES.length];
}

export function UserAvatar({ name, size = 32 }: { name: string; size?: number }) {
  return (
    <Avatar size={size} style={{ background: toneFor(name), color: '#fff', fontWeight: 600, flex: 'none' }}>
      {initials(name)}
    </Avatar>
  );
}
