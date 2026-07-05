import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import { PASSWORD_HINT, validatePassword } from './passwordPolicy';

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [token, setToken] = useState(params.get('token') ?? '');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const policyError = validatePassword(password);
    if (policyError) return setError(policyError);
    if (password !== confirm) return setError('Passwords do not match.');

    setBusy(true);
    try {
      await authApi.resetPassword(token.trim(), password);
      setDone(true);
      setTimeout(() => navigate('/login'), 2000);
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not reset password.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="brand">Set a new password</h1>

        {done ? (
          <div className="alert success">Password updated. Redirecting to sign in…</div>
        ) : (
          <form onSubmit={submit}>
            {error && <div className="alert error">{error}</div>}
            <label>
              Reset token
              <input value={token} onChange={(e) => setToken(e.target.value)} required autoFocus />
            </label>
            <label>
              New password
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
            </label>
            <label>
              Confirm new password
              <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} required />
            </label>
            <p className="muted small">{PASSWORD_HINT}</p>
            <button className="primary" type="submit" disabled={busy}>
              {busy ? 'Updating…' : 'Reset password'}
            </button>
            <p className="muted small" style={{ marginTop: 12 }}>
              <Link to="/login">Back to sign in</Link>
            </p>
          </form>
        )}
      </div>
    </div>
  );
}
