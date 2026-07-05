import { useCallback, useEffect, useState } from 'react';
import { onboardingApi, tasksApi } from '../../api/endpoints';
import { realtime, RealtimeEvents } from '../../api/realtime';
import { ProgressBar } from '../../components/ProgressBar';
import type { EmployeeTask, OnboardingSummary, TaskCategory } from '../../types';
import { AssignTaskModal } from './AssignTaskModal';
import { GroupedTaskList } from '../../components/GroupedTaskList';

interface Props {
  employeeId: number;
  isOwn: boolean;
  canAssign: boolean;
  assignableCategories: TaskCategory[];
}

export function OnboardingTab({ employeeId, isOwn, canAssign, assignableCategories }: Props) {
  const [summary, setSummary] = useState<OnboardingSummary | null>(null);
  const [tasks, setTasks] = useState<EmployeeTask[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [assigning, setAssigning] = useState(false);

  const load = useCallback(async () => {
    try {
      const [s, t] = await Promise.all([
        onboardingApi.forEmployee(employeeId),
        tasksApi.forEmployee(employeeId),
      ]);
      setSummary(s);
      setTasks(t);
    } catch {
      setError('Could not load onboarding data.');
    }
  }, [employeeId]);

  useEffect(() => {
    load();
    const handler = () => load();
    realtime.on(RealtimeEvents.TasksChanged, handler);
    realtime.on(RealtimeEvents.ProgressChanged, handler);
    return () => {
      realtime.off(RealtimeEvents.TasksChanged, handler);
      realtime.off(RealtimeEvents.ProgressChanged, handler);
    };
  }, [load]);

  if (error) return <div className="alert error">{error}</div>;
  if (!summary) return <p>Loading…</p>;

  const remaining = summary.taskCount - summary.completedTaskCount;

  return (
    <div>
      <div className="card">
        <div className="section-header">
          <h3>Onboarding Progress</h3>
          {canAssign && (
            <button className="primary" onClick={() => setAssigning(true)}>
              Assign task
            </button>
          )}
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

      <GroupedTaskList tasks={tasks} canUpdateStatus={isOwn} onChanged={load} />

      {assigning && (
        <AssignTaskModal
          employeeId={employeeId}
          categories={assignableCategories}
          onClose={() => setAssigning(false)}
          onAssigned={() => {
            setAssigning(false);
            load();
          }}
        />
      )}
    </div>
  );
}
