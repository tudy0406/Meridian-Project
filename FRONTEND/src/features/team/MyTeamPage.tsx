import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { documentationApi, teamsApi, usersApi } from '../../api/endpoints';
import { useAuth } from '../../auth/AuthContext';
import { Roles } from '../../auth/roles';
import type { Team, TeamDocumentation, UserProfile, UserSummary } from '../../types';

export function MyTeamPage() {
  const { user, hasRole } = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [team, setTeam] = useState<Team | null>(null);
  const [members, setMembers] = useState<UserSummary[]>([]);
  const [docs, setDocs] = useState<TeamDocumentation[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    usersApi
      .me()
      .then(async (me) => {
        setProfile(me);
        if (!me.teamId) {
          setError('You are not assigned to a team yet.');
          return;
        }
        const [t, m, d] = await Promise.all([
          teamsApi.get(me.teamId),
          usersApi.teamMembers(me.teamId),
          documentationApi.forTeam(me.teamId),
        ]);
        setTeam(t);
        setMembers(m);
        setDocs(d);
      })
      .catch(() => setError('Could not load your team.'));
  }, []);

  if (error) return <div className="alert error">{error}</div>;
  if (!profile || !team) return <p>Loading…</p>;

  const grouped = docs.reduce<Record<string, TeamDocumentation[]>>((acc, doc) => {
    (acc[doc.category] ??= []).push(doc);
    return acc;
  }, {});

  return (
    <section>
      <h2>{team.name}</h2>
      <p className="muted">
        {team.departmentName} · Manager:{' '}
        {team.managerId ? <Link to={`/profile/${team.managerId}`}>{team.managerName}</Link> : '—'} · Team Lead:{' '}
        {team.teamLeadId ? <Link to={`/profile/${team.teamLeadId}`}>{team.teamLeadName}</Link> : '—'}
      </p>
      {team.description && <p>{team.description}</p>}

      <div className="two-col">
        <div>
          <div className="section-header">
            <h3>Documentation</h3>
            {(hasRole(Roles.Administrator) || (hasRole(Roles.TeamLead) && team.teamLeadId === user?.userId)) && (
              <Link to={`/documentation/manage?teamId=${team.id}`} className="button primary">
                Manage documentation
              </Link>
            )}
          </div>
          {docs.length === 0 && <p className="muted">No documentation yet.</p>}
          {Object.entries(grouped).map(([category, entries]) => (
            <div key={category} className="card">
              <h4>{category}</h4>
              {entries.map((doc) => (
                <details key={doc.id}>
                  <summary>{doc.title}</summary>
                  {/* Content is rendered as text, never as raw HTML (XSS-safe). */}
                  <p className="doc-content">{doc.content}</p>
                </details>
              ))}
            </div>
          ))}
        </div>

        <div>
          <h3>Team Members</h3>
          <ul className="member-list">
            {members.map((m) => (
              <li key={m.id}>
                <Link to={`/profile/${m.id}`} className="task-title">
                  {m.fullName}
                </Link>
                <span className="muted"> {m.jobTitle ?? m.roles.join(', ')}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}
