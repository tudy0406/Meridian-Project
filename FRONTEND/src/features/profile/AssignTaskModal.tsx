import { useEffect, useState } from 'react';
import { tasksApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import { Modal } from '../../components/Modal';
import type { TaskCategory, TaskPriority, TaskTemplate } from '../../types';

type Mode = 'template' | 'custom';

interface Props {
  employeeId: number;
  categories: TaskCategory[];
  onClose: () => void;
  onAssigned: () => void;
}

export function AssignTaskModal({ employeeId, categories, onClose, onAssigned }: Props) {
  const [mode, setMode] = useState<Mode>('template');

  return (
    <Modal title="Assign a task" onClose={onClose} wide>
      <div className="mode-switch">
        <button className={mode === 'template' ? 'primary' : ''} onClick={() => setMode('template')}>
          Use predefined template
        </button>
        <button className={mode === 'custom' ? 'primary' : ''} onClick={() => setMode('custom')}>
          Create custom task
        </button>
      </div>

      {mode === 'template' ? (
        <TemplateForm employeeId={employeeId} categories={categories} onAssigned={onAssigned} />
      ) : (
        <CustomForm employeeId={employeeId} categories={categories} onAssigned={onAssigned} />
      )}
    </Modal>
  );
}

function TemplateForm({
  employeeId,
  categories,
  onAssigned,
}: {
  employeeId: number;
  categories: TaskCategory[];
  onAssigned: () => void;
}) {
  const [templates, setTemplates] = useState<TaskTemplate[]>([]);
  const [templateId, setTemplateId] = useState(0);
  const [deadline, setDeadline] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    tasksApi.templates
      .list()
      .then((all) => setTemplates(all.filter((t) => categories.includes(t.category))))
      .catch(() => {});
  }, [categories]);

  const selected = templates.find((t) => t.id === templateId);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!templateId) return setError('Select a template.');
    setBusy(true);
    try {
      await tasksApi.assignFromTemplate({
        onboardingEmployeeId: employeeId,
        taskTemplateId: templateId,
        deadline: deadline ? new Date(deadline).toISOString() : null,
      });
      onAssigned();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not assign task.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <form onSubmit={submit}>
      {error && <div className="alert error">{error}</div>}
      {templates.length === 0 && <p className="muted">No predefined templates available for your role.</p>}
      <label>
        Template
        <select value={templateId} onChange={(e) => setTemplateId(Number(e.target.value))}>
          <option value={0}>Select…</option>
          {templates.map((t) => (
            <option key={t.id} value={t.id}>
              [{t.category}] {t.title}
            </option>
          ))}
        </select>
      </label>
      {selected && (
        <div className="template-preview">
          {selected.description && <p>{selected.description}</p>}
          {selected.requirements && (
            <p className="muted small">
              <strong>Requirements:</strong> {selected.requirements}
            </p>
          )}
          <p className="muted small">
            Priority: {selected.priority} · Suggested {selected.estimatedCompletionDays} days
          </p>
        </div>
      )}
      <label>
        Deadline (optional — defaults from template)
        <input type="date" value={deadline} onChange={(e) => setDeadline(e.target.value)} />
      </label>
      <button className="primary" type="submit" disabled={busy || !templateId}>
        {busy ? 'Assigning…' : 'Assign task'}
      </button>
    </form>
  );
}

function CustomForm({
  employeeId,
  categories,
  onAssigned,
}: {
  employeeId: number;
  categories: TaskCategory[];
  onAssigned: () => void;
}) {
  const [form, setForm] = useState({
    title: '',
    description: '',
    requirements: '',
    category: categories[0],
    priority: 'Medium' as TaskPriority,
    deadline: '',
  });
  const [attachments, setAttachments] = useState<{ fileName: string; url: string }[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const addAttachment = () => setAttachments((a) => [...a, { fileName: '', url: '' }]);
  const updateAttachment = (i: number, patch: Partial<{ fileName: string; url: string }>) =>
    setAttachments((a) => a.map((x, idx) => (idx === i ? { ...x, ...patch } : x)));
  const removeAttachment = (i: number) => setAttachments((a) => a.filter((_, idx) => idx !== i));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await tasksApi.assign({
        onboardingEmployeeId: employeeId,
        title: form.title,
        description: form.description || null,
        requirements: form.requirements || null,
        category: form.category,
        priority: form.priority,
        deadline: form.deadline ? new Date(form.deadline).toISOString() : null,
        attachments: attachments.filter((a) => a.fileName && a.url),
      });
      onAssigned();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not assign task.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <form onSubmit={submit}>
      {error && <div className="alert error">{error}</div>}
      <label>
        Title
        <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required />
      </label>
      <label>
        Description
        <textarea rows={3} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
      </label>
      <label>
        Requirements
        <textarea
          rows={2}
          value={form.requirements}
          onChange={(e) => setForm({ ...form, requirements: e.target.value })}
        />
      </label>
      <div className="form-row">
        <label>
          Category
          <select
            value={form.category}
            onChange={(e) => setForm({ ...form, category: e.target.value as TaskCategory })}
          >
            {categories.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </label>
        <label>
          Priority
          <select value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value as TaskPriority })}>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
          </select>
        </label>
        <label>
          Due date
          <input type="date" value={form.deadline} onChange={(e) => setForm({ ...form, deadline: e.target.value })} />
        </label>
      </div>

      <div className="attachments-editor">
        <div className="section-header">
          <span className="field-label">Attachments (optional)</span>
          <button type="button" onClick={addAttachment}>
            + Add
          </button>
        </div>
        {attachments.map((a, i) => (
          <div key={i} className="form-row">
            <input
              placeholder="File name"
              value={a.fileName}
              onChange={(e) => updateAttachment(i, { fileName: e.target.value })}
            />
            <input
              placeholder="URL"
              value={a.url}
              onChange={(e) => updateAttachment(i, { url: e.target.value })}
            />
            <button type="button" onClick={() => removeAttachment(i)}>
              ✕
            </button>
          </div>
        ))}
      </div>

      <button className="primary" type="submit" disabled={busy}>
        {busy ? 'Assigning…' : 'Assign task'}
      </button>
    </form>
  );
}
