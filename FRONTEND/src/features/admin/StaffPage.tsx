import { useCallback, useEffect, useState } from 'react';
import { adminApi, departmentsApi, teamsApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import type { AdminUser, Department, Role, RoleOption, Team } from '../../types';

// Manager & Team Lead follow the Organization page's department/team assignments,
// so they are shown read-only here.
const DERIVED_ROLES: Role[] = ['Manager', 'Team Lead'];
const isDerived = (r: Role) => DERIVED_ROLES.includes(r);

/** Administrator management of staff accounts and their roles. */
export function StaffPage() {
  const [roles, setRoles] = useState<RoleOption[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [teams, setTeams] = useState<Team[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [r, u, d] = await Promise.all([adminApi.roles(), adminApi.users(), departmentsApi.list()]);
      setRoles(r);
      setUsers(u);
      setDepartments(d);
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not load staff data.');
    }
  }, []);

  useEffect(() => {
    load();
    teamsApi.list().then(setTeams).catch(() => {});
  }, [load]);

  return (
    <section>
      <h2>Staff &amp; Roles</h2>
      <p className="muted">
        Create accounts for existing employees (managers, team leads, mentors) and manage everyone's roles. New hires
        are onboarded from the <strong>Create Employee</strong> page instead. <strong>Manager</strong> and{' '}
        <strong>Team Lead</strong> are assigned from the <strong>Organization</strong> page and shown read-only here.
      </p>
      {error && <div className="alert error">{error}</div>}

      <div className="two-col">
        <CreateStaffForm roles={roles} departments={departments} teams={teams} onCreated={load} />
        <UsersRolesTable users={users} roles={roles} onChanged={load} />
      </div>
    </section>
  );
}

function CreateStaffForm({
  roles,
  departments,
  teams,
  onCreated,
}: {
  roles: RoleOption[];
  departments: Department[];
  teams: Team[];
  onCreated: () => void;
}) {
  const empty = {
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    jobTitle: '',
    inOfficeDays: '',
    departmentId: 0,
    teamId: 0,
  };
  const [form, setForm] = useState(empty);
  const [selectedRoles, setSelectedRoles] = useState<Role[]>([]);
  const [result, setResult] = useState<{ email: string; temporaryPassword: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const teamsForDept = form.departmentId
    ? teams.filter((t) => t.departmentId === form.departmentId)
    : teams;

  const toggleRole = (role: Role) =>
    setSelectedRoles((prev) => (prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setResult(null);
    if (selectedRoles.length === 0) {
      setError('Select at least one role.');
      return;
    }
    setBusy(true);
    try {
      const res = await adminApi.createStaff({
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        phoneNumber: form.phoneNumber || null,
        jobTitle: form.jobTitle || null,
        inOfficeDays: form.inOfficeDays || null,
        departmentId: form.departmentId || null,
        teamId: form.teamId || null,
        roles: selectedRoles,
      });
      setResult(res);
      setForm(empty);
      setSelectedRoles([]);
      onCreated();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not create staff account.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <form className="card" onSubmit={submit}>
      <h3>Create staff account</h3>
      {error && <div className="alert error">{error}</div>}
      {result && (
        <div className="alert success">
          Created <strong>{result.email}</strong>. Temporary password: <code>{result.temporaryPassword}</code>
        </div>
      )}

      <label>
        First name
        <input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} required />
      </label>
      <label>
        Last name
        <input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} required />
      </label>
      <label>
        Email
        <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
      </label>
      <label>
        Job title
        <input value={form.jobTitle} onChange={(e) => setForm({ ...form, jobTitle: e.target.value })} />
      </label>
      <label>
        Department (optional)
        <select
          value={form.departmentId}
          onChange={(e) => setForm({ ...form, departmentId: Number(e.target.value), teamId: 0 })}
        >
          <option value={0}>None</option>
          {departments.map((d) => (
            <option key={d.id} value={d.id}>
              {d.name}
            </option>
          ))}
        </select>
      </label>
      <label>
        Team (optional)
        <select value={form.teamId} onChange={(e) => setForm({ ...form, teamId: Number(e.target.value) })}>
          <option value={0}>None</option>
          {teamsForDept.map((t) => (
            <option key={t.id} value={t.id}>
              {t.name}
            </option>
          ))}
        </select>
      </label>

      <fieldset className="roles-fieldset">
        <legend>Roles</legend>
        {roles.map((r) => (
          <label key={r.id} className="checkbox" title={isDerived(r.name) ? 'Assigned from the Organization page' : ''}>
            <input
              type="checkbox"
              disabled={isDerived(r.name)}
              checked={selectedRoles.includes(r.name)}
              onChange={() => toggleRole(r.name)}
            />
            {r.name}
          </label>
        ))}
      </fieldset>

      <button className="primary" type="submit" disabled={busy}>
        {busy ? 'Creating…' : 'Create account'}
      </button>
    </form>
  );
}

function UsersRolesTable({
  users,
  roles,
  onChanged,
}: {
  users: AdminUser[];
  roles: RoleOption[];
  onChanged: () => void;
}) {
  // Local editable copy of each user's roles, keyed by user id.
  const [draft, setDraft] = useState<Record<number, Role[]>>({});
  const [savingId, setSavingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setDraft(Object.fromEntries(users.map((u) => [u.id, u.roles])));
  }, [users]);

  const toggle = (userId: number, role: Role) =>
    setDraft((prev) => {
      const current = prev[userId] ?? [];
      const next = current.includes(role) ? current.filter((r) => r !== role) : [...current, role];
      return { ...prev, [userId]: next };
    });

  const save = async (userId: number) => {
    setError(null);
    const next = draft[userId] ?? [];
    if (next.length === 0) {
      setError('A user must have at least one role.');
      return;
    }
    setSavingId(userId);
    try {
      await adminApi.setRoles(userId, next);
      onChanged();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not update roles.');
    } finally {
      setSavingId(null);
    }
  };

  return (
    <div>
      <h3>Users &amp; roles</h3>
      {error && <div className="alert error">{error}</div>}
      <table className="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Roles</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => {
            const current = draft[u.id] ?? [];
            const dirty = JSON.stringify([...current].sort()) !== JSON.stringify([...u.roles].sort());
            return (
              <tr key={u.id}>
                <td>
                  {u.fullName}
                  <div className="muted small">{u.email}</div>
                  {u.isOnboarding && <span className="chip">onboarding</span>}
                </td>
                <td>
                  <div className="role-checks">
                    {roles.map((r) => (
                      <label
                        key={r.id}
                        className="checkbox small"
                        title={isDerived(r.name) ? 'Assigned from the Organization page' : ''}
                      >
                        <input
                          type="checkbox"
                          disabled={isDerived(r.name)}
                          checked={current.includes(r.name)}
                          onChange={() => toggle(u.id, r.name)}
                        />
                        {r.name}
                      </label>
                    ))}
                  </div>
                </td>
                <td>
                  <button className="primary" disabled={!dirty || savingId === u.id} onClick={() => save(u.id)}>
                    {savingId === u.id ? 'Saving…' : 'Save'}
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
