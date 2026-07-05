import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { documentationApi, teamsApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { Roles } from '../../auth/roles';
import { formatDate } from '../../utils/datetime';
import type { Team, TeamDocumentation } from '../../types';

const CATEGORIES = [
  'General', 'Technologies', 'Communication', 'Projects',
  'Workflow', 'VersionControl', 'CodingStandards', 'Faq',
];

const emptyDoc = { title: '', category: 'General', content: '' };

/** Team Lead / HR editor for a team's onboarding documentation. */
export function ManageDocsPage() {
  const { user, hasRole } = useAuth();
  const [searchParams] = useSearchParams();
  const preselectTeamId = Number(searchParams.get('teamId')) || 0;
  const [teams, setTeams] = useState<Team[]>([]);
  const [teamId, setTeamId] = useState(0);
  const [docs, setDocs] = useState<TeamDocumentation[]>([]);
  const [form, setForm] = useState({ ...emptyDoc });
  const [editingId, setEditingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    teamsApi.list().then((all) => {
      // Team Leads only manage their own teams; HR/Admin see all.
      const mine = hasRole(Roles.HrEmployee, Roles.Administrator)
        ? all
        : all.filter((t) => t.teamLeadId === user?.userId);
      setTeams(mine);
      // Preselect from the query string (e.g. from the My Team page) if allowed,
      // otherwise auto-select when the user manages exactly one team.
      if (preselectTeamId && mine.some((t) => t.id === preselectTeamId)) setTeamId(preselectTeamId);
      else if (mine.length === 1) setTeamId(mine[0].id);
    });
  }, [hasRole, user?.userId, preselectTeamId]);

  const loadDocs = useCallback(() => {
    if (teamId) documentationApi.forTeam(teamId).then(setDocs).catch(() => {});
    else setDocs([]);
  }, [teamId]);

  useEffect(() => {
    loadDocs();
  }, [loadDocs]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    const payload = { teamId, title: form.title, category: form.category, content: form.content };
    try {
      if (editingId) await documentationApi.update(editingId, payload);
      else await documentationApi.create(payload);
      setForm({ ...emptyDoc });
      setEditingId(null);
      loadDocs();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not save documentation.');
    }
  };

  const edit = (d: TeamDocumentation) => {
    setEditingId(d.id);
    setForm({ title: d.title, category: d.category, content: d.content });
  };

  const remove = async (d: TeamDocumentation) => {
    await documentationApi.remove(d.id);
    loadDocs();
  };

  const leadsNoTeam = !hasRole(Roles.HrEmployee, Roles.Administrator) && teams.length === 0;

  return (
    <section>
      <h2>Manage Team Documentation</h2>

      {leadsNoTeam && (
        <div className="alert error">
          You are not assigned as the Team Lead of any team yet, so there is no documentation for you to manage. Ask a
          Manager or Administrator to assign you as a team's lead.
        </div>
      )}

      {!leadsNoTeam && (
        <label style={{ maxWidth: 320 }}>
          Team
          <select value={teamId} onChange={(e) => setTeamId(Number(e.target.value))}>
            <option value={0}>Select…</option>
            {teams.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
        </label>
      )}

      {error && <div className="alert error">{error}</div>}

      {teamId > 0 && (
        <div className="two-col">
          <div>
            <h3>Sections</h3>
            {docs.length === 0 && <p className="muted">No documentation yet.</p>}
            <ul className="task-list">
              {docs.map((d) => (
                <li key={d.id} className="task-card">
                  <div className="task-main">
                    <div className="task-title">{d.title}</div>
                    <div className="task-meta">
                      <span className="chip">{d.category}</span>
                      <span className="muted small">
                        Updated {formatDate(d.updatedAt)}
                      </span>
                    </div>
                  </div>
                  <div className="task-actions">
                    <button onClick={() => edit(d)}>Edit</button>
                    <button onClick={() => remove(d)}>Delete</button>
                  </div>
                </li>
              ))}
            </ul>
          </div>

          <form className="card" onSubmit={submit}>
            <h3>{editingId ? 'Edit section' : 'New section'}</h3>
            <label>
              Title
              <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required />
            </label>
            <label>
              Category
              <select value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })}>
                {CATEGORIES.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Content
              <textarea
                value={form.content}
                rows={6}
                onChange={(e) => setForm({ ...form, content: e.target.value })}
                required
              />
            </label>
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="primary" type="submit">
                {editingId ? 'Save' : 'Create'}
              </button>
              {editingId && (
                <button
                  type="button"
                  onClick={() => {
                    setEditingId(null);
                    setForm({ ...emptyDoc });
                  }}
                >
                  Cancel
                </button>
              )}
            </div>
          </form>
        </div>
      )}
    </section>
  );
}
