import { useCallback, useEffect, useMemo, useState } from 'react';
import { departmentsApi, tasksApi, teamsApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { Roles } from '../../auth/roles';
import type { Department, TaskCategory, TaskPriority, TaskTemplate, Team } from '../../types';

function useManageableCategories(): TaskCategory[] {
  const { hasRole } = useAuth();
  return useMemo(() => {
    if (hasRole(Roles.Administrator)) return ['Hr', 'Department', 'Team', 'Personal'];
    const set = new Set<TaskCategory>();
    if (hasRole(Roles.HrEmployee)) set.add('Hr');
    if (hasRole(Roles.Manager)) set.add('Department');
    if (hasRole(Roles.TeamLead)) set.add('Team');
    if (hasRole(Roles.Mentor)) set.add('Personal');
    return [...set];
  }, [hasRole]);
}

const empty = {
  title: '',
  description: '',
  category: 'Hr' as TaskCategory,
  priority: 'Medium' as TaskPriority,
  estimatedCompletionDays: 7,
  departmentId: 0,
  teamId: 0,
};

const scopeHint: Record<TaskCategory, string> = {
  Hr: 'HR templates are company-wide — assigned to every new employee.',
  Department: 'Assigned automatically to new employees in your department.',
  Team: 'Assigned automatically to new employees on your team.',
  Personal: 'Only you can see these; assigned automatically to the employees you mentor.',
};

export function TemplatesPage() {
  const { hasRole } = useAuth();
  const isAdmin = hasRole(Roles.Administrator);
  const categories = useManageableCategories();
  const [templates, setTemplates] = useState<TaskTemplate[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [teams, setTeams] = useState<Team[]>([]);
  const [form, setForm] = useState({ ...empty, category: categories[0] ?? 'Hr' });
  const [editingId, setEditingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    tasksApi.templates.list().then(setTemplates).catch(() => {});
  }, []);

  useEffect(() => {
    load();
    // Admins bind department/team templates explicitly, so they need the lists.
    if (isAdmin) {
      departmentsApi.list().then(setDepartments).catch(() => {});
      teamsApi.list().then(setTeams).catch(() => {});
    }
  }, [load, isAdmin]);

  const reset = () => {
    setForm({ ...empty, category: categories[0] ?? 'Hr' });
    setEditingId(null);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    const payload: Record<string, unknown> = {
      title: form.title,
      description: form.description || null,
      category: form.category,
      priority: form.priority,
      estimatedCompletionDays: Number(form.estimatedCompletionDays),
    };
    // Only an admin sets the scope explicitly; for everyone else the server
    // derives it from what they manage/lead/authored.
    if (isAdmin && form.category === 'Department') payload.departmentId = form.departmentId || null;
    if (isAdmin && form.category === 'Team') payload.teamId = form.teamId || null;

    try {
      if (editingId) await tasksApi.templates.update(editingId, payload);
      else await tasksApi.templates.create(payload);
      reset();
      load();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not save template.');
    }
  };

  const edit = (t: TaskTemplate) => {
    setEditingId(t.id);
    setForm({
      title: t.title,
      description: t.description ?? '',
      category: t.category,
      priority: t.priority,
      estimatedCompletionDays: t.estimatedCompletionDays,
      departmentId: t.departmentId ?? 0,
      teamId: t.teamId ?? 0,
    });
  };

  const deactivate = async (t: TaskTemplate) => {
    await tasksApi.templates.deactivate(t.id);
    load();
  };

  return (
    <section>
      <h2>Task Templates</h2>
      <p className="muted">
        Templates are copied into an employee's tasks when onboarding begins; later edits only affect new employees.
      </p>
      {error && <div className="alert error">{error}</div>}

      <div className="two-col">
        <div>
          <table className="data-table">
            <thead>
              <tr>
                <th>Title</th>
                <th>Category</th>
                <th>Priority</th>
                <th>Days</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {templates.map((t) => (
                <tr key={t.id} style={{ opacity: t.isActive ? 1 : 0.5 }}>
                  <td>{t.title}</td>
                  <td>{t.category}</td>
                  <td>{t.priority}</td>
                  <td>{t.estimatedCompletionDays}</td>
                  <td style={{ display: 'flex', gap: 6 }}>
                    <button onClick={() => edit(t)}>Edit</button>
                    {t.isActive && <button onClick={() => deactivate(t)}>Disable</button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <form className="card" onSubmit={submit}>
          <h3>{editingId ? 'Edit template' : 'New template'}</h3>
          <label>
            Title
            <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required />
          </label>
          <label>
            Description
            <textarea
              value={form.description}
              rows={2}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </label>
          <label>
            Category
            <select
              value={form.category}
              onChange={(e) => setForm({ ...form, category: e.target.value as TaskCategory })}
            >
              {categories.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
          </label>

          {isAdmin && form.category === 'Department' && (
            <label>
              Department
              <select
                value={form.departmentId}
                onChange={(e) => setForm({ ...form, departmentId: Number(e.target.value) })}
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
          )}
          {isAdmin && form.category === 'Team' && (
            <label>
              Team
              <select
                value={form.teamId}
                onChange={(e) => setForm({ ...form, teamId: Number(e.target.value) })}
                required
              >
                <option value={0}>Select…</option>
                {teams.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name} ({t.departmentName})
                  </option>
                ))}
              </select>
            </label>
          )}

          <p className="muted small">{scopeHint[form.category]}</p>

          <label>
            Priority
            <select
              value={form.priority}
              onChange={(e) => setForm({ ...form, priority: e.target.value as TaskPriority })}
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </label>
          <label>
            Estimated completion (days)
            <input
              type="number"
              min={1}
              max={365}
              value={form.estimatedCompletionDays}
              onChange={(e) => setForm({ ...form, estimatedCompletionDays: Number(e.target.value) })}
            />
          </label>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="primary" type="submit">
              {editingId ? 'Save' : 'Create'}
            </button>
            {editingId && (
              <button type="button" onClick={reset}>
                Cancel
              </button>
            )}
          </div>
        </form>
      </div>
    </section>
  );
}
