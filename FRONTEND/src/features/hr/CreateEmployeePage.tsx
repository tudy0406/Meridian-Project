import { useEffect, useState } from 'react';
import { departmentsApi, teamsApi, usersApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import type { Department, Team, UserSummary } from '../../types';

export function CreateEmployeePage() {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [teams, setTeams] = useState<Team[]>([]);
  const [mentors, setMentors] = useState<UserSummary[]>([]);

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    jobTitle: '',
    inOfficeDays: '',
    departmentId: 0,
    teamId: 0,
    mentorId: 0,
  });
  const [result, setResult] = useState<{ email: string; temporaryPassword: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    departmentsApi.list().then(setDepartments).catch(() => {});
  }, []);

  useEffect(() => {
    if (!form.departmentId) {
      setTeams([]);
      return;
    }
    teamsApi.list(form.departmentId).then(setTeams).catch(() => {});
  }, [form.departmentId]);

  useEffect(() => {
    if (!form.teamId) {
      setMentors([]);
      return;
    }
    usersApi.teamMembers(form.teamId).then(setMentors).catch(() => {});
  }, [form.teamId]);

  const update = (patch: Partial<typeof form>) => setForm((f) => ({ ...f, ...patch }));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setResult(null);
    setSubmitting(true);
    try {
      const res = await usersApi.createEmployee({
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        phoneNumber: form.phoneNumber || null,
        jobTitle: form.jobTitle || null,
        inOfficeDays: form.inOfficeDays || null,
        departmentId: Number(form.departmentId),
        teamId: Number(form.teamId),
        mentorId: form.mentorId ? Number(form.mentorId) : null,
      });
      setResult(res);
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not create employee.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section>
      <h2>Create Employee</h2>
      <p className="muted">Onboarding starts automatically once the account is created.</p>

      {error && <div className="alert error">{error}</div>}
      {result && (
        <div className="alert success">
          Created <strong>{result.email}</strong>. Temporary password:{' '}
          <code>{result.temporaryPassword}</code> — share it securely; the user should reset it on first login.
        </div>
      )}

      <form className="form-grid" onSubmit={submit}>
        <label>
          First name
          <input value={form.firstName} onChange={(e) => update({ firstName: e.target.value })} required />
        </label>
        <label>
          Last name
          <input value={form.lastName} onChange={(e) => update({ lastName: e.target.value })} required />
        </label>
        <label>
          Email
          <input type="email" value={form.email} onChange={(e) => update({ email: e.target.value })} required />
        </label>
        <label>
          Phone
          <input value={form.phoneNumber} onChange={(e) => update({ phoneNumber: e.target.value })} />
        </label>
        <label>
          Job title
          <input value={form.jobTitle} onChange={(e) => update({ jobTitle: e.target.value })} />
        </label>
        <label>
          In-office days
          <input
            value={form.inOfficeDays}
            placeholder="Mon,Tue,Wed"
            onChange={(e) => update({ inOfficeDays: e.target.value })}
          />
        </label>
        <label>
          Department
          <select
            value={form.departmentId}
            onChange={(e) => update({ departmentId: Number(e.target.value), teamId: 0, mentorId: 0 })}
            required
          >
            <option value={0}>Select…</option>
            {departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Team
          <select
            value={form.teamId}
            onChange={(e) => update({ teamId: Number(e.target.value), mentorId: 0 })}
            required
            disabled={!form.departmentId}
          >
            <option value={0}>Select…</option>
            {teams.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Mentor (optional)
          <select
            value={form.mentorId}
            onChange={(e) => update({ mentorId: Number(e.target.value) })}
            disabled={!form.teamId}
          >
            <option value={0}>None</option>
            {mentors.map((m) => (
              <option key={m.id} value={m.id}>
                {m.fullName}
              </option>
            ))}
          </select>
        </label>

        <div className="form-actions">
          <button className="primary" type="submit" disabled={submitting}>
            {submitting ? 'Creating…' : 'Create employee'}
          </button>
        </div>
      </form>
    </section>
  );
}
