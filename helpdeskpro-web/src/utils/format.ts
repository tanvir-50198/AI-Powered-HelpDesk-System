import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import updateLocale from 'dayjs/plugin/updateLocale';

dayjs.extend(relativeTime);
dayjs.extend(updateLocale);

/**
 * The API stores UTC. If a timestamp comes without a time-zone designator
 * (DateTimeKind.Unspecified) we treat it as UTC so it is shown in the viewer's local time.
 */
export function toDate(value: string) {
  const hasZone = /([zZ]|[+-]\d{2}:?\d{2})$/.test(value);
  return dayjs(hasZone ? value : value + 'Z');
}

export const fromNow = (value: string) => toDate(value).fromNow();
export const fullDate = (value: string) => toDate(value).format('D MMM YYYY, HH:mm');
export const shortDate = (value: string) => toDate(value).format('D MMM YYYY');

export function displayName(user: { fullName?: string | null; email: string }) {
  return user.fullName?.trim() || user.email;
}

export function initials(name: string) {
  const parts = name.replace(/@.*/, '').split(/[\s._-]+/).filter(Boolean);
  const letters = (parts[0]?.[0] ?? '?') + (parts.length > 1 ? parts[parts.length - 1][0] : '');
  return letters.toUpperCase();
}

export function plural(count: number, one: string, many = one + 's') {
  return `${count} ${count === 1 ? one : many}`;
}

export function percent(value: number) {
  return `${Math.round(value)}%`;
}
