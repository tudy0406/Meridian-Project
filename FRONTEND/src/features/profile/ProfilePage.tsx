import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { usersApi } from '../../api/endpoints';
import { useAuth } from '../../auth/AuthContext';
import { Roles } from '../../auth/roles';
import type { TaskCategory, UserProfile } from '../../types';
import { ProfileHeader } from './ProfileHeader';
import { InfoTab } from './InfoTab';
import { OnboardingTab } from './OnboardingTab';

type Tab = 'info' | 'onboarding';

/**
 * Profile page — shows either the signed-in user's own profile or that of an
 * employee they supervise (via /profile/:userId). Content and available actions
 * adapt to the viewer's role and to the own-vs-supervised relationship.
 */
export function ProfilePage() {
  const { userId } = useParams();
  const { user, hasRole } = useAuth();

  const targetId = userId ? Number(userId) : user!.userId;
  const isOwn = targetId === user!.userId;

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [tab, setTab] = useState<Tab>('info');
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setProfile(await usersApi.byId(targetId));
    } catch {
      setError('Could not load this profile.');
    }
  }, [targetId]);

  useEffect(() => {
    load();
  }, [load]);

  // Task categories this viewer may assign to the profiled employee.
  const assignableCategories = useMemo<TaskCategory[]>(() => {
    if (hasRole(Roles.Administrator)) return ['Hr', 'Department', 'Team', 'Personal'];
    const set = new Set<TaskCategory>();
    if (hasRole(Roles.HrEmployee)) set.add('Hr');
    if (hasRole(Roles.Manager)) set.add('Department');
    if (hasRole(Roles.TeamLead)) set.add('Team');
    if (hasRole(Roles.Mentor)) set.add('Personal');
    return [...set];
  }, [hasRole]);

  if (error) return <div className="alert error">{error}</div>;
  if (!profile) return <p>Loading…</p>;

  // The onboarding tab is available only when the viewer may actually access this
  // employee's onboarding (own, HR/Admin, or in their supervisory audience). This
  // ensures a mentor only sees onboarding data for the employees they mentor.
  const showOnboarding = profile.canAccessOnboarding;

  const canAssign = !isOwn && profile.canAccessOnboarding && assignableCategories.length > 0;
  const canEditTeam = !!profile.teamId && hasRole(Roles.TeamLead, Roles.Manager, Roles.Administrator);
  const canEditDepartment = !!profile.departmentId && hasRole(Roles.Manager, Roles.Administrator);

  return (
    <section>
      <ProfileHeader profile={profile} />

      <nav className="tab-nav">
        <button className={tab === 'info' ? 'active' : ''} onClick={() => setTab('info')}>
          Info
        </button>
        {showOnboarding && (
          <button className={tab === 'onboarding' ? 'active' : ''} onClick={() => setTab('onboarding')}>
            Onboarding Process
          </button>
        )}
      </nav>

      {tab === 'info' && (
        <InfoTab
          profile={profile}
          isOwn={isOwn}
          canEditTeam={canEditTeam}
          canEditDepartment={canEditDepartment}
          onReload={load}
        />
      )}
      {tab === 'onboarding' && showOnboarding && (
        <OnboardingTab
          employeeId={targetId}
          isOwn={isOwn}
          canAssign={canAssign}
          assignableCategories={assignableCategories}
        />
      )}
    </section>
  );
}
