import type { Role } from '../types';

export const Roles = {
  Administrator: 'Administrator',
  HrEmployee: 'HR Employee',
  Manager: 'Manager',
  TeamLead: 'Team Lead',
  Mentor: 'Mentor',
  Employee: 'Employee',
} as const;

export const hasAnyRole = (userRoles: Role[], allowed: Role[]) =>
  userRoles.some((r) => allowed.includes(r));
