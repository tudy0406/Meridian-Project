import { useCallback, useEffect, useState } from 'react';
import { departmentsApi, teamsApi, usersApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import type { Department, Team, UserSummary } from '../../types';

/** HR/Admin management of departments and teams (create + key assignments). */
export function OrganizationPage() {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [teams, setTeams] = useState<Team[]>([]);
  const [people, setPeople] = useState<UserSummary[]>([]);
  const [error, setError] = useState<string | null>(null);

  const [newDept, setNewDept] = useState('');
  const [team, setTeam] = useState({ name: '', description: '', departmentId: 0 });

  const load = useCallback(async () => {
    try {
      const [d, t, p] = await Promise.all([departmentsApi.list(), teamsApi.list(), usersApi.list()]);
      setDepartments(d);
      setTeams(t);
      setPeople(p);
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not load organization data.');
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const createDepartment = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await departmentsApi.create(newDept.trim());
      setNewDept('');
      load();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not create department.');
    }
  };

  const setManager = async (dept: Department, managerId: number) => {
    // Assigning the manager here grants them the Manager role and propagates to
    // the department's teams (single source of truth for "who manages this").
    await departmentsApi.update(dept.id, {
      name: dept.name,
      description: dept.description ?? null,
      managerId: managerId || null,
    });
    load();
  };

  const createTeam = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await teamsApi.create({
        name: team.name.trim(),
        description: team.description || null,
        departmentId: Number(team.departmentId),
      });
      setTeam({ name: '', description: '', departmentId: 0 });
      load();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not create team.');
    }
  };

  const setTeamLead = async (t: Team, userId: number) => {
    if (userId) await teamsApi.assignTeamLead(t.id, userId);
    load();
  };

  return (
    <section>
      <h2>Organization</h2>
      {error && <div className="alert error">{error}</div>}

      <div className="two-col">
        <div>
          <h3>Departments</h3>
          <form className="inline-form" onSubmit={createDepartment}>
            <input
              placeholder="New department name"
              value={newDept}
              onChange={(e) => setNewDept(e.target.value)}
              required
            />
            <button className="primary" type="submit">
              Add
            </button>
          </form>
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Teams</th>
                <th>Manager</th>
              </tr>
            </thead>
            <tbody>
              {departments.map((d) => (
                <tr key={d.id}>
                  <td>{d.name}</td>
                  <td>{d.teamCount}</td>
                  <td>
                    {/* Only members of this department can be its manager. */}
                    <select value={d.managerId ?? 0} onChange={(e) => setManager(d, Number(e.target.value))}>
                      <option value={0}>—</option>
                      {people
                        .filter((p) => p.departmentId === d.id)
                        .map((p) => (
                          <option key={p.id} value={p.id}>
                            {p.fullName}
                          </option>
                        ))}
                    </select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div>
          <h3>Teams</h3>
          <form className="card" onSubmit={createTeam}>
            <label>
              Name
              <input value={team.name} onChange={(e) => setTeam({ ...team, name: e.target.value })} required />
            </label>
            <label>
              Department
              <select
                value={team.departmentId}
                onChange={(e) => setTeam({ ...team, departmentId: Number(e.target.value) })}
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
              Description
              <textarea
                value={team.description}
                rows={2}
                onChange={(e) => setTeam({ ...team, description: e.target.value })}
              />
            </label>
            <button className="primary" type="submit">
              Create team
            </button>
          </form>

          <table className="data-table">
            <thead>
              <tr>
                <th>Team</th>
                <th>Department</th>
                <th>Team Lead</th>
              </tr>
            </thead>
            <tbody>
              {teams.map((t) => (
                <tr key={t.id}>
                  <td>{t.name}</td>
                  <td>{t.departmentName}</td>
                  <td>
                    {/* Only members of this team can be its lead. */}
                    <select value={t.teamLeadId ?? 0} onChange={(e) => setTeamLead(t, Number(e.target.value))}>
                      <option value={0}>—</option>
                      {people
                        .filter((p) => p.teamId === t.id)
                        .map((p) => (
                          <option key={p.id} value={p.id}>
                            {p.fullName}
                          </option>
                        ))}
                    </select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
