import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { onboardingApi } from '../../api/endpoints';
import { ProgressBar } from '../../components/ProgressBar';
import type { OnboardingSummary } from '../../types';

export function MonitorPage() {
  const [items, setItems] = useState<OnboardingSummary[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    onboardingApi.list().then(setItems).finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Loading…</p>;

  return (
    <section>
      <h2>Onboarding Monitor</h2>
      {items.length === 0 && <p className="muted">No onboarding processes are visible to you.</p>}
      <table className="data-table">
        <thead>
          <tr>
            <th>Employee</th>
            <th>Team</th>
            <th>Mentor</th>
            <th>Status</th>
            <th>Progress</th>
          </tr>
        </thead>
        <tbody>
          {items.map((o) => (
            <tr key={o.onboardingId}>
              <td>
                <Link to={`/profile/${o.employeeId}`}>{o.employeeName}</Link>
              </td>
              <td>{o.teamName ?? '—'}</td>
              <td>{o.mentorName ?? '—'}</td>
              <td>{o.status}</td>
              <td style={{ minWidth: 160 }}>
                <ProgressBar value={o.progressPercentage} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
