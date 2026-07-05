import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { departmentsApi, teamsApi, usersApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import { Modal } from '../../components/Modal';
import type { Department, Team, UserProfile, UserSummary } from '../../types';

interface InfoTabProps {
  profile: UserProfile;
  isOwn: boolean;
  canEditTeam: boolean;
  canEditDepartment: boolean;
  onReload: () => void;
}

export function InfoTab({ profile, isOwn, canEditTeam, canEditDepartment, onReload }: InfoTabProps) {
  const [team, setTeam] = useState<Team | null>(null);
  const [department, setDepartment] = useState<Department | null>(null);
  const [members, setMembers] = useState<UserSummary[]>([]);
  const [editing, setEditing] = useState<'team' | 'department' | null>(null);

  useEffect(() => {
    if (profile.teamId) {
      teamsApi.get(profile.teamId).then(setTeam).catch(() => {});
      usersApi.teamMembers(profile.teamId).then(setMembers).catch(() => {});
    }
    if (profile.departmentId) departmentsApi.get(profile.departmentId).then(setDepartment).catch(() => {});
  }, [profile.teamId, profile.departmentId]);

  return (
    <div className="info-grid">
      <Section title="Personal Information">
        <Field label="Full name" value={profile.fullName} />
        <Field label="Job title" value={profile.jobTitle} />
        <Field label="In-office days" value={profile.inOfficeDays} />
      </Section>

      <Section title="Contact Information">
        <Field label="Email" value={profile.email} />
        <Field label="Phone" value={profile.phoneNumber} />
      </Section>

      <Section
        title="Organization Information"
        action={
          <>
            {canEditDepartment && department && (
              <button onClick={() => setEditing('department')}>Edit department</button>
            )}
            {canEditTeam && team && <button onClick={() => setEditing('team')}>Edit team</button>}
          </>
        }
      >
        <Field label="Department" value={department?.name ?? profile.departmentName} />
        {department?.description && <Field label="Department info" value={department.description} />}
        <Field label="Team" value={team?.name ?? profile.teamName} />
        {team?.description && <Field label="Team info" value={team.description} />}
        <Field label="Manager" value={team?.managerName} />
        <Field label="Team Lead" value={team?.teamLeadName} />
        <div className="field">
          <span className="field-label">Roles</span>
          <span className="badges">
            {profile.roles.map((r) => (
              <span key={r} className="badge role small">
                {r}
              </span>
            ))}
          </span>
        </div>
        {members.length > 0 && (
          <div className="field">
            <span className="field-label">Team members</span>
            <span>
              {members.map((m, i) => (
                <span key={m.id}>
                  {i > 0 && ', '}
                  <Link to={`/profile/${m.id}`}>{m.fullName}</Link>
                </span>
              ))}
            </span>
          </div>
        )}
      </Section>

      <Section title="Security">
        <Field label="Onboarding status" value={profile.isOnboarding ? 'Onboarding' : 'Completed'} />
        {isOwn ? (
          <Link to="/change-password" className="button primary inline-block">
            Change Password
          </Link>
        ) : (
          <p className="muted small">Only the account owner can reset their password.</p>
        )}
      </Section>

      {editing === 'team' && team && (
        <EditTeamModal team={team} onClose={() => setEditing(null)} onSaved={() => { setEditing(null); onReload(); }} />
      )}
      {editing === 'department' && department && (
        <EditDepartmentModal
          department={department}
          onClose={() => setEditing(null)}
          onSaved={() => { setEditing(null); onReload(); }}
        />
      )}
    </div>
  );
}

function Section({
  title,
  action,
  children,
}: {
  title: string;
  action?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <div className="card">
      <div className="section-header">
        <h3>{title}</h3>
        <div style={{ display: 'flex', gap: 8 }}>{action}</div>
      </div>
      {children}
    </div>
  );
}

function Field({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="field">
      <span className="field-label">{label}</span>
      <span>{value || '—'}</span>
    </div>
  );
}

function EditTeamModal({ team, onClose, onSaved }: { team: Team; onClose: () => void; onSaved: () => void }) {
  const [name, setName] = useState(team.name);
  const [description, setDescription] = useState(team.description ?? '');
  const [error, setError] = useState<string | null>(null);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await teamsApi.update(team.id, { name, description });
      onSaved();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not update team.');
    }
  };

  return (
    <Modal title="Edit team information" onClose={onClose}>
      <form onSubmit={save}>
        {error && <div className="alert error">{error}</div>}
        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required />
        </label>
        <label>
          Description
          <textarea rows={4} value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>
        <button className="primary" type="submit">
          Save
        </button>
      </form>
    </Modal>
  );
}

function EditDepartmentModal({
  department,
  onClose,
  onSaved,
}: {
  department: Department;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [name, setName] = useState(department.name);
  const [description, setDescription] = useState(department.description ?? '');
  const [error, setError] = useState<string | null>(null);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      // Preserve the existing manager (ignored for non-privileged callers).
      await departmentsApi.update(department.id, { name, description, managerId: department.managerId });
      onSaved();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not update department.');
    }
  };

  return (
    <Modal title="Edit department information" onClose={onClose}>
      <form onSubmit={save}>
        {error && <div className="alert error">{error}</div>}
        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required />
        </label>
        <label>
          Description
          <textarea rows={4} value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>
        <button className="primary" type="submit">
          Save
        </button>
      </form>
    </Modal>
  );
}
