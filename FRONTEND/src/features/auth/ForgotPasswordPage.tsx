import { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await authApi.forgotPassword(email);
      setSubmitted(true);
    } catch (err) {
      const apiErr = err as ApiError;
      // The endpoint intentionally does not reveal whether the account exists;
      // a 429 just means the rate limit was hit.
      setError(apiErr.status === 429 ? 'Too many attempts. Please wait a minute.' : 'Something went wrong.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="brand">Reset password</h1>

        {submitted ? (
          <>
            <div className="alert success">
              If an account exists for <strong>{email}</strong>, a reset token has been sent by email.
            </div>
            <p className="muted small">
              Then use that token on the <Link to="/reset-password">reset page</Link>.
            </p>
            <Link to="/login">Back to sign in</Link>
          </>
        ) : (
          <form onSubmit={submit}>
            <p className="muted">Enter your email and we'll send you a password reset token.</p>
            {error && <div className="alert error">{error}</div>}
            <label>
              Email
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
            </label>
            <button className="primary" type="submit" disabled={busy}>
              {busy ? 'Sending…' : 'Send reset token'}
            </button>
            <p className="muted small" style={{ marginTop: 12 }}>
              <Link to="/login">Back to sign in</Link> · <Link to="/reset-password">I already have a token</Link>
            </p>
          </form>
        )}
      </div>
    </div>
  );
}
