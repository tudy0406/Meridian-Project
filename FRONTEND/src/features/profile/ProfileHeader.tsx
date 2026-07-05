import type { UserProfile } from '../../types';

function isActiveOnboarding(status: string) {
  return status === 'Active' || status === 'Pending';
}

function initials(fullName: string) {
  return fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join('');
}

/** Header card: avatar, full name, role badges and onboarding status badge. */
export function ProfileHeader({ profile }: { profile: UserProfile }) {
  return (
    <div className="profile-header card">
      <div className="avatar" aria-hidden>
        {initials(profile.fullName)}
      </div>
      <div className="profile-header-info">
        <h2>{profile.fullName}</h2>
        <div className="badges">
          {profile.roles.map((r) => (
            <span key={r} className="badge role">
              {r}
            </span>
          ))}
          {/* Onboarding badge only for accounts that actually have an onboarding
              process; existing-employee accounts show none. */}
          {profile.onboardingStatus &&
            (isActiveOnboarding(profile.onboardingStatus) ? (
              <span className="badge onboarding">Onboarding</span>
            ) : (
              <span className="badge completed">Completed</span>
            ))}
        </div>
        <div className="muted">{profile.jobTitle ?? '—'}</div>
      </div>
    </div>
  );
}
