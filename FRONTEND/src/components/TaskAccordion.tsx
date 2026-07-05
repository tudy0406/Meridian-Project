import { useState } from 'react';
import { Link } from 'react-router-dom';
import { tasksApi } from '../api/endpoints';
import type { EmployeeTask, EmployeeTaskDetail, EmployeeTaskStatus } from '../types';
import { formatDate, formatDateTime } from '../utils/datetime';

const statusLabel: Record<string, string> = {
  NotStarted: 'Not started',
  InProgress: 'In progress',
  Completed: 'Completed',
};

/** A task card that expands into a full accordion of task details. */
export function TaskAccordion({
  task,
  canUpdateStatus,
  onChanged,
}: {
  task: EmployeeTask;
  canUpdateStatus: boolean;
  onChanged: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [detail, setDetail] = useState<EmployeeTaskDetail | null>(null);
  const [loading, setLoading] = useState(false);

  const toggle = async () => {
    const next = !open;
    setOpen(next);
    if (next && !detail) {
      setLoading(true);
      try {
        setDetail(await tasksApi.detail(task.id));
      } finally {
        setLoading(false);
      }
    }
  };

  const setStatus = async (status: EmployeeTaskStatus) => {
    await tasksApi.updateStatus(task.id, status);
    setDetail(null); // force refresh of history on next expand
    onChanged();
  };

  return (
    <li className={`task-card status-${task.status.toLowerCase()} accordion`}>
      <div className="task-card-row" onClick={toggle}>
        <div className="task-main">
          <div className="task-title">
            <span className="chevron">{open ? '▾' : '▸'}</span> {task.title}
          </div>
          <div className="task-meta">
            <span className="status-pill">{statusLabel[task.status]}</span>
            <span className={`chip priority-${task.priority.toLowerCase()}`}>{task.priority}</span>
            {task.deadline && <span className="chip">Due {formatDate(task.deadline)}</span>}
            <span className="chip">By {task.assignedByName ?? '—'}</span>
          </div>
        </div>
      </div>

      {open && (
        <div className="task-detail" onClick={(e) => e.stopPropagation()}>
          {loading || !detail ? (
            <p className="muted">Loading…</p>
          ) : (
            <>
              <DetailBlock label="Description" value={detail.description} />
              <DetailBlock label="Requirements" value={detail.requirements} />

              <div className="detail-meta">
                <span>
                  <strong>Assigned by:</strong>{' '}
                  {detail.assignedByName ? (
                    <Link to={`/profile/${detail.assignedById}`}>{detail.assignedByName}</Link>
                  ) : (
                    '—'
                  )}
                </span>
                <span>
                  <strong>Assigned:</strong> {formatDate(detail.assignedAt)}
                </span>
                {detail.completedAt && (
                  <span>
                    <strong>Completed:</strong> {formatDate(detail.completedAt)}
                  </span>
                )}
              </div>

              {detail.attachments.length > 0 && (
                <div className="detail-section">
                  <span className="field-label">Attachments</span>
                  <ul className="attachment-list">
                    {detail.attachments.map((a) => (
                      <li key={a.id}>
                        <a href={a.url} target="_blank" rel="noreferrer">
                          📎 {a.fileName}
                        </a>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              <div className="detail-section">
                <span className="field-label">Completion history</span>
                <ul className="history-list">
                  {detail.history.map((h, i) => (
                    <li key={i}>
                      <span className="chip">{statusLabel[h.status] ?? h.status}</span> by {h.changedByName} ·{' '}
                      {formatDateTime(h.changedAt)}
                    </li>
                  ))}
                </ul>
              </div>

              <CommentSection taskId={task.id} detail={detail} onPosted={setDetail} />

              {canUpdateStatus && task.status !== 'Completed' && (
                <div className="task-actions">
                  {task.status === 'NotStarted' && <button onClick={() => setStatus('InProgress')}>Start</button>}
                  <button className="primary" onClick={() => setStatus('Completed')}>
                    Mark done
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      )}
    </li>
  );
}

function DetailBlock({ label, value }: { label: string; value?: string | null }) {
  if (!value) return null;
  return (
    <div className="detail-section">
      <span className="field-label">{label}</span>
      <p className="doc-content">{value}</p>
    </div>
  );
}

function CommentSection({
  taskId,
  detail,
  onPosted,
}: {
  taskId: number;
  detail: EmployeeTaskDetail;
  onPosted: (d: EmployeeTaskDetail) => void;
}) {
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);

  const post = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!text.trim()) return;
    setBusy(true);
    try {
      const comment = await tasksApi.addComment(taskId, text.trim());
      onPosted({ ...detail, comments: [...detail.comments, comment] });
      setText('');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="detail-section">
      <span className="field-label">Comments</span>
      <ul className="comment-list">
        {detail.comments.length === 0 && <li className="muted small">No comments yet.</li>}
        {detail.comments.map((c) => (
          <li key={c.id}>
            <strong>{c.authorName}</strong> <span className="muted small">{formatDateTime(c.createdAt)}</span>
            <div>{c.text}</div>
          </li>
        ))}
      </ul>
      <form className="inline-form" onSubmit={post}>
        <input placeholder="Add a comment…" value={text} onChange={(e) => setText(e.target.value)} />
        <button type="submit" disabled={busy || !text.trim()}>
          Post
        </button>
      </form>
    </div>
  );
}
