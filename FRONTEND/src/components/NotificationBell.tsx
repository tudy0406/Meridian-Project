import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { notificationsApi } from '../api/endpoints';
import { realtime, RealtimeEvents } from '../api/realtime';

/** Live unread-notification counter. Updates in real time via SignalR. */
export function NotificationBell() {
  const [count, setCount] = useState(0);

  useEffect(() => {
    let active = true;
    notificationsApi.unreadCount().then((c) => active && setCount(c)).catch(() => {});

    const handler = () => setCount((c) => c + 1);
    realtime.on(RealtimeEvents.NotificationReceived, handler);
    return () => {
      active = false;
      realtime.off(RealtimeEvents.NotificationReceived, handler);
    };
  }, []);

  return (
    <Link to="/notifications" className="notification-bell" title="Notifications">
      🔔{count > 0 && <span className="badge">{count}</span>}
    </Link>
  );
}
