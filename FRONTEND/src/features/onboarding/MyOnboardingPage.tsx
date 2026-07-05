import { useCallback, useEffect, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { onboardingApi, tasksApi } from '../../api/endpoints';
import { realtime, RealtimeEvents } from '../../api/realtime';
import { useAuth } from '../../auth/AuthContext';
import { ProgressBar } from '../../components/ProgressBar';
import { GroupedTaskList } from '../../components/GroupedTaskList';
import { formatDate } from '../../utils/datetime';
import type { EmployeeTask, OnboardingSummary } from '../../types';

export function MyOnboardingPage() {
  const { user } = useAuth();
  const [summary, setSummary] = useState<OnboardingSummary | null>(null);
  const [tasks, setTasks] = useState<EmployeeTask[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [s, t] = await Promise.all([onboardingApi.mine(), tasksApi.mine()]);
      setSummary(s);
      setTasks(t);
    } catch {
      setError('No onboarding process found for your account.');
    }
  }, []);

  useEffect(() => {
    load();
    const handler = () => load();
    realtime.on(RealtimeEvents.ProgressChanged, handler);
    realtime.on(RealtimeEvents.TasksChanged, handler);
    return () => {
      realtime.off(RealtimeEvents.ProgressChanged, handler);
      realtime.off(RealtimeEvents.TasksChanged, handler);
    };
  }, [load]);

  // Only onboarding accounts have this page; existing employees are redirected.
  if (user && !user.isOnboarding) return <Navigate to="/profile" replace />;

  if (error) return <div className="alert error">{error}</div>;
  if (!summary) return <p>Loading…</p>;

  const remaining = summary.taskCount - summary.completedTaskCount;

  return (
    <section>
      <h2>My Onboarding</h2>

      <div className="card">
        <div className="onboarding-header">
          <div>
            <div className="muted">Status</div>
            <div className="stat">{summary.status}</div>
          </div>
          <div>
            <div className="muted">Started</div>
            <div className="stat">{formatDate(summary.startDate)}</div>
          </div>
          <div>
            <div className="muted">Mentor</div>
            <div className="stat">{summary.mentorName ?? '—'}</div>
          </div>
          <div>
            <div className="muted">Team</div>
            <div className="stat">{summary.teamName ?? '—'}</div>
          </div>
        </div>
        <ProgressBar value={summary.progressPercentage} />
        <div className="stats-row">
          <div className="stat-box">
            <div className="stat">{summary.completedTaskCount}</div>
            <div className="muted small">Completed</div>
          </div>
          <div className="stat-box">
            <div className="stat">{remaining}</div>
            <div className="muted small">Remaining</div>
          </div>
          <div className="stat-box">
            <div className="stat">{summary.taskCount}</div>
            <div className="muted small">Total</div>
          </div>
        </div>
      </div>

      <h3>My Tasks</h3>
      <GroupedTaskList tasks={tasks} canUpdateStatus onChanged={load} />
    </section>
  );
}
