import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { meetingsApi, onboardingApi } from '../../api/endpoints';
import type { ApiError } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { Roles } from '../../auth/roles';
import { Modal } from '../../components/Modal';
import { formatDateTime } from '../../utils/datetime';
import type { Meeting, OnboardingSummary } from '../../types';

export function MeetingsPage() {
  const { hasRole } = useAuth();
  const canSchedule = hasRole(
    Roles.HrEmployee,
    Roles.Manager,
    Roles.TeamLead,
    Roles.Mentor,
    Roles.Administrator,
  );

  const [meetings, setMeetings] = useState<Meeting[]>([]);
  const [loading, setLoading] = useState(true);
  const [scheduling, setScheduling] = useState(false);

  const load = useCallback(() => {
    meetingsApi.mine().then(setMeetings).finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  if (loading) return <p>Loading…</p>;

  return (
    <section>
      <div className="section-header">
        <h2>My Meetings</h2>
        {canSchedule && (
          <button className="primary" onClick={() => setScheduling(true)}>
            Schedule meeting
          </button>
        )}
      </div>

      {meetings.length === 0 && <p className="muted">No meetings scheduled.</p>}
      <ul className="task-list">
        {meetings.map((m) => (
          <MeetingCard key={m.id} meeting={m} />
        ))}
      </ul>

      {scheduling && (
        <ScheduleMeetingModal
          onClose={() => setScheduling(false)}
          onCreated={() => {
            setScheduling(false);
            load();
          }}
        />
      )}
    </section>
  );
}

/** Collapsible meeting card: title + date, click to expand full details. */
function MeetingCard({ meeting }: { meeting: Meeting }) {
  const [open, setOpen] = useState(false);
  const when = new Date(meeting.dateTime);

  return (
    <li className="task-card accordion">
      <div className="task-card-row" onClick={() => setOpen((o) => !o)}>
        <div className="task-main">
          <div className="task-title">
            <span className="chevron">{open ? '▾' : '▸'}</span> {meeting.title}
          </div>
        </div>
        <span className="chip">{formatDateTime(when)}</span>
      </div>

      {open && (
        <div className="task-detail" onClick={(e) => e.stopPropagation()}>
          {meeting.description && <p>{meeting.description}</p>}

          <div className="detail-meta">
            <span>
              <strong>When:</strong> {formatDateTime(when)}
            </span>
            <span>
              <strong>Organizer:</strong> {meeting.organizerName}
            </span>
            {meeting.location && (
              <span>
                <strong>Location:</strong> {meeting.location}
              </span>
            )}
          </div>

          {meeting.onlineLink && (
            <div className="detail-section">
              <span className="field-label">Online link</span>
              <a href={meeting.onlineLink} target="_blank" rel="noreferrer">
                {meeting.onlineLink}
              </a>
            </div>
          )}

          <div className="detail-section">
            <span className="field-label">Participants</span>
            <div>
              {meeting.participants.length === 0 ? (
                <span className="muted">none</span>
              ) : (
                meeting.participants.map((p, i) => (
                  <span key={p.userId}>
                    {i > 0 && ', '}
                    <Link to={`/profile/${p.userId}`}>{p.fullName}</Link>
                  </span>
                ))
              )}
            </div>
          </div>
        </div>
      )}
    </li>
  );
}

function ScheduleMeetingModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [candidates, setCandidates] = useState<OnboardingSummary[]>([]);
  const [form, setForm] = useState({
    title: '',
    description: '',
    dateTime: '',
    location: '',
    onlineLink: '',
  });
  const [participantIds, setParticipantIds] = useState<number[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    // The people this supervisor may invite are exactly the onboarding
    // employees in their scope (same list as the Onboarding Monitor).
    onboardingApi.list().then(setCandidates).catch(() => {});
  }, []);

  const toggle = (id: number) =>
    setParticipantIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!form.location.trim() && !form.onlineLink.trim()) {
      setError('Provide a location or an online link.');
      return;
    }
    if (participantIds.length === 0) {
      setError('Add at least one onboarding employee.');
      return;
    }
    setBusy(true);
    try {
      await meetingsApi.create({
        title: form.title,
        description: form.description || null,
        dateTime: new Date(form.dateTime).toISOString(),
        location: form.location || null,
        onlineLink: form.onlineLink || null,
        participantIds,
      });
      onCreated();
    } catch (err) {
      setError((err as ApiError).message ?? 'Could not schedule meeting.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal title="Schedule a meeting" onClose={onClose} wide>
      <form onSubmit={submit}>
        {error && <div className="alert error">{error}</div>}
        <label>
          Title
          <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required />
        </label>
        <label>
          Description
          <textarea rows={2} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
        </label>
        <label>
          Date &amp; time
          <input
            type="datetime-local"
            value={form.dateTime}
            onChange={(e) => setForm({ ...form, dateTime: e.target.value })}
            required
          />
        </label>
        <div className="form-row">
          <label>
            Location (in-person)
            <input value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} />
          </label>
          <label>
            Online link (remote)
            <input value={form.onlineLink} onChange={(e) => setForm({ ...form, onlineLink: e.target.value })} />
          </label>
        </div>

        <fieldset className="roles-fieldset">
          <legend>Onboarding employees</legend>
          {candidates.length === 0 && <span className="muted small">No onboarding employees in your scope.</span>}
          {candidates.map((c) => (
            <label key={c.employeeId} className="checkbox">
              <input
                type="checkbox"
                checked={participantIds.includes(c.employeeId)}
                onChange={() => toggle(c.employeeId)}
              />
              {c.employeeName}
              {c.teamName ? ` · ${c.teamName}` : ''}
            </label>
          ))}
        </fieldset>

        <button className="primary" type="submit" disabled={busy}>
          {busy ? 'Scheduling…' : 'Schedule & notify'}
        </button>
      </form>
    </Modal>
  );
}
