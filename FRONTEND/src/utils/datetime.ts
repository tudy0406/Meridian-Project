/**
 * Centralized date/time formatting for the whole app.
 * Times always use a 24-hour HH:MM template (no seconds, no AM/PM).
 */

function toDate(value: string | Date): Date {
  return value instanceof Date ? value : new Date(value);
}

/** Date only: DD/MM/YYYY. */
export function formatDate(value: string | Date): string {
  const d = toDate(value);
  const dd = String(d.getDate()).padStart(2, '0');
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  return `${dd}/${mm}/${d.getFullYear()}`;
}

/** Time only: HH:MM (24-hour). */
export function formatTime(value: string | Date): string {
  const d = toDate(value);
  const hh = String(d.getHours()).padStart(2, '0');
  const min = String(d.getMinutes()).padStart(2, '0');
  return `${hh}:${min}`;
}

/** Date and time: DD/MM/YYYY HH:MM. */
export function formatDateTime(value: string | Date): string {
  return `${formatDate(value)} ${formatTime(value)}`;
}
