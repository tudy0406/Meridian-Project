/** Client-side mirror of the server password policy (server remains authoritative). */
export const PASSWORD_HINT = 'At least 8 characters, with an uppercase letter, a lowercase letter, and a digit.';

export function validatePassword(password: string): string | null {
  if (password.length < 8) return 'Password must be at least 8 characters long.';
  if (!/[A-Z]/.test(password)) return 'Password must contain an uppercase letter.';
  if (!/[a-z]/.test(password)) return 'Password must contain a lowercase letter.';
  if (!/[0-9]/.test(password)) return 'Password must contain a digit.';
  return null;
}
