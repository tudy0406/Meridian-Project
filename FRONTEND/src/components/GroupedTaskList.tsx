import type { EmployeeTask, TaskCategory } from '../types';
import { TaskAccordion } from './TaskAccordion';

const CATEGORY_ORDER: TaskCategory[] = ['Hr', 'Department', 'Team', 'Personal'];
const CATEGORY_LABEL: Record<string, string> = {
  Hr: 'HR',
  Department: 'Department',
  Team: 'Team',
  Personal: 'Personal',
};

/** Renders onboarding tasks grouped by category (HR, Department, Team, Personal). */
export function GroupedTaskList({
  tasks,
  canUpdateStatus,
  onChanged,
}: {
  tasks: EmployeeTask[];
  canUpdateStatus: boolean;
  onChanged: () => void;
}) {
  if (tasks.length === 0) return <p className="muted">No tasks assigned yet.</p>;

  // Known categories in a sensible order, then any others that appear.
  const present = CATEGORY_ORDER.filter((c) => tasks.some((t) => t.category === c));
  const extras = [...new Set(tasks.map((t) => t.category))].filter((c) => !CATEGORY_ORDER.includes(c));
  const categories = [...present, ...extras];

  return (
    <div className="task-groups">
      {categories.map((category) => {
        const group = tasks.filter((t) => t.category === category);
        const done = group.filter((t) => t.status === 'Completed').length;
        return (
          <div key={category} className="task-group">
            <div className="task-group-header">
              <h4>{CATEGORY_LABEL[category] ?? category} tasks</h4>
              <span className="muted small">
                {done}/{group.length} done
              </span>
            </div>
            <ul className="task-list">
              {group.map((task) => (
                <TaskAccordion
                  key={task.id}
                  task={task}
                  canUpdateStatus={canUpdateStatus}
                  onChanged={onChanged}
                />
              ))}
            </ul>
          </div>
        );
      })}
    </div>
  );
}
