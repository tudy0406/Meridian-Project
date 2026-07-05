import { Link } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { Roles } from '../../auth/roles';

export function DashboardPage() {
  const { user, hasRole } = useAuth();
  const isNewEmployee = user?.isOnboarding;

  return (
    <section>
      <h2>Welcome, {user?.fullName}</h2>
      <p className="muted">Roles: {user?.roles.join(', ')}</p>

      <div className="card-grid">
        {isNewEmployee && (
          <Link to="/onboarding" className="tile">
            <h3>My Onboarding</h3>
            <p>Track your progress and complete your tasks.</p>
          </Link>
        )}

        <Link to="/team" className="tile">
          <h3>My Team</h3>
          <p>Team information, members and documentation.</p>
        </Link>

        <Link to="/meetings" className="tile">
          <h3>Meetings</h3>
          <p>Your scheduled onboarding meetings.</p>
        </Link>

        {hasRole(Roles.HrEmployee, Roles.Manager, Roles.TeamLead, Roles.Mentor, Roles.Administrator) && (
          <Link to="/monitor" className="tile">
            <h3>Onboarding Monitor</h3>
            <p>Track onboarding progress across people you oversee.</p>
          </Link>
        )}

        {hasRole(Roles.HrEmployee, Roles.Administrator) && (
          <Link to="/hr/employees" className="tile">
            <h3>Create Employee</h3>
            <p>Onboard a new employee and start their journey.</p>
          </Link>
        )}
      </div>
    </section>
  );
}
