import { useCallback, useEffect, useState } from 'react';
import { notificationsApi } from '../../api/endpoints';
import { realtime, RealtimeEvents } from '../../api/realtime';
import { formatDateTime } from '../../utils/datetime';
import type { AppNotification } from '../../types';

export function NotificationsPage() {
  const [items, setItems] = useState<AppNotification[]>([]);

  const load = useCallback(() => {
    notificationsApi.list().then(setItems).catch(() => {});
  }, []);

  useEffect(() => {
    load();
    const handler = () => load();
    realtime.on(RealtimeEvents.NotificationReceived, handler);
    return () => realtime.off(RealtimeEvents.NotificationReceived, handler);
  }, [load]);

  const markRead = async (id: number) => {
    await notificationsApi.markRead(id);
    setItems((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
  };

  const markAllRead = async () => {
    await notificationsApi.markAllRead();
    setItems((prev) => prev.map((n) => ({ ...n, isRead: true })));
  };

  return (
    <section>
      <div className="section-header">
        <h2>Notifications</h2>
        <button onClick={markAllRead}>Mark all as read</button>
      </div>
      {items.length === 0 && <p className="muted">No notifications.</p>}
      <ul className="notification-list">
        {items.map((n) => (
          <li key={n.id} className={n.isRead ? 'read' : 'unread'}>
            <div>
              <div className="task-title">{n.title}</div>
              <div className="muted">{n.message}</div>
              <div className="muted small">{formatDateTime(n.createdAt)}</div>
            </div>
            {!n.isRead && <button onClick={() => markRead(n.id)}>Mark read</button>}
          </li>
        ))}
      </ul>
    </section>
  );
}
