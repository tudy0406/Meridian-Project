import { useEffect, useRef, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { Roles } from '../auth/roles';
import type { Role } from '../types';
import { NotificationBell } from './NotificationBell';

interface NavItem {
  to: string;
  label: string;
  roles?: Role[]; // visible to these roles; undefined = everyone
  requiresOnboarding?: boolean; // only for accounts currently onboarding
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

const PRIMARY: NavItem[] = [
  { to: '/', label: 'Dashboard' },
  { to: '/profile', label: 'My Profile' },
  { to: '/onboarding', label: 'My Onboarding', requiresOnboarding: true },
  { to: '/team', label: 'My Team' },
  { to: '/meetings', label: 'Meetings' },
];

const GROUPS: NavGroup[] = [
  {
    label: 'Onboarding',
    items: [
      {
        to: '/monitor',
        label: 'Onboarding Monitor',
        roles: [Roles.HrEmployee, Roles.Manager, Roles.TeamLead, Roles.Mentor, Roles.Administrator],
      },
      {
        to: '/templates',
        label: 'Task Templates',
        roles: [Roles.HrEmployee, Roles.Manager, Roles.TeamLead, Roles.Mentor, Roles.Administrator],
      },
      { to: '/hr/employees', label: 'Create Employee', roles: [Roles.HrEmployee, Roles.Administrator] },
      { to: '/documentation/manage', label: 'Manage Docs', roles: [Roles.TeamLead, Roles.HrEmployee, Roles.Administrator] },
    ],
  },
  {
    label: 'Administration',
    items: [
      { to: '/organization', label: 'Organization', roles: [Roles.Administrator] },
      { to: '/admin/staff', label: 'Staff & Roles', roles: [Roles.Administrator] },
    ],
  },
];

export function Layout() {
  const { user, logout, hasRole } = useAuth();
  const navigate = useNavigate();

  const canSee = (item: NavItem) => {
    if (item.requiresOnboarding && !user?.isOnboarding) return false;
    return !item.roles || hasRole(...item.roles);
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand">Meridian</div>
        <nav className="main-nav">
          {PRIMARY.filter(canSee).map((item) => (
            <NavLink key={item.to} to={item.to} end={item.to === '/'}>
              {item.label}
            </NavLink>
          ))}
          {GROUPS.map((group) => {
            const visible = group.items.filter(canSee);
            if (visible.length === 0) return null;
            return <NavDropdown key={group.label} label={group.label} items={visible} />;
          })}
        </nav>
        <div className="header-right">
          <NotificationBell />
          <span className="user-name">{user?.fullName}</span>
          <button className="link-button" onClick={handleLogout}>
            Sign out
          </button>
        </div>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  );
}

function NavDropdown({ label, items }: { label: string; items: NavItem[] }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const location = useLocation();

  // Close on outside click.
  useEffect(() => {
    if (!open) return;
    const onClick = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, [open]);

  // Close when the route changes.
  useEffect(() => setOpen(false), [location.pathname]);

  const active = items.some((i) => location.pathname.startsWith(i.to));

  return (
    <div className={`nav-dropdown ${open ? 'open' : ''}`} ref={ref}>
      <button className={`nav-dropdown-trigger ${active ? 'active' : ''}`} onClick={() => setOpen((o) => !o)}>
        {label} <span className="caret">▾</span>
      </button>
      {open && (
        <div className="nav-dropdown-menu">
          {items.map((item) => (
            <NavLink key={item.to} to={item.to} className="nav-dropdown-item">
              {item.label}
            </NavLink>
          ))}
        </div>
      )}
    </div>
  );
}
