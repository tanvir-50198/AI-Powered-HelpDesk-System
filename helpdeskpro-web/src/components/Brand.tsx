import { Link } from 'react-router-dom';

export function BrandMark({ size = 32 }: { size?: number }) {
  return (
    <svg className="brand__mark" width={size} height={size} viewBox="0 0 64 64" aria-hidden="true" focusable="false">
      <rect width="64" height="64" rx="14" fill="#14474c" />
      <path
        d="M16 22a6 6 0 0 1 6-6h20a6 6 0 0 1 6 6v4a6 6 0 0 0 0 12v4a6 6 0 0 1-6 6H22a6 6 0 0 1-6-6v-4a6 6 0 0 0 0-12z"
        fill="#5eead4"
      />
      <path d="M26 28h12M26 36h8" stroke="#0b2f33" strokeWidth="3.5" strokeLinecap="round" />
    </svg>
  );
}

export function Brand({ to = '/dashboard' }: { to?: string }) {
  return (
    <Link to={to} className="brand" aria-label="HelpDesk Pro home">
      <BrandMark />
      <span>
        HelpDesk Pro
        <small>Support, answered</small>
      </span>
    </Link>
  );
}
