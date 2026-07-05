import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import { PASSWORD_HINT, validatePassword } from './passwordPolicy';

/** Authenticated self-service password change (any signed-in user). */
export function ChangePasswordPage() {
  const navigate = useNavigate();
  const [current, setCurrent] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setDone(false);

    const policyError = validatePassword(password);
    if (policyError) return setError(policyError);
    if (password !== confirm) return setError('Passwords do not match.');

    setBusy(true);
    try {
      await authApi.changePassword(current, password);
      setDone(true);
      setCurrent('');
      setPassword('');
      setConfirm('');
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not change password.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <section>
      <h2>Change password</h2>
      <form className="card" style={{ maxWidth: 420 }} onSubmit={submit}>
        {error && <div className="alert error">{error}</div>}
        {done && <div className="alert success">Your password has been changed.</div>}
        <label>
          Current password
          <input type="password" value={current} onChange={(e) => setCurrent(e.target.value)} required />
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
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="primary" type="submit" disabled={busy}>
            {busy ? 'Saving…' : 'Change password'}
          </button>
          <button type="button" onClick={() => navigate(-1)}>
            Cancel
          </button>
        </div>
      </form>
    </section>
  );
}
