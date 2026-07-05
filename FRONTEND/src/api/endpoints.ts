import { api } from './client';
import type {
  AdminUser,
  AppNotification,
  Department,
  EmployeeTaskDetail,
  RoleOption,
  TaskComment,
  EmployeeTask,
  EmployeeTaskStatus,
  LoginResponse,
  Meeting,
  OnboardingSummary,
  TaskTemplate,
  Team,
  TeamDocumentation,
  UserProfile,
  UserSummary,
} from '../types';

/** Grouped, typed wrappers over the REST API — one namespace per feature. */

export const authApi = {
  login: (email: string, password: string) =>
    api.post<LoginResponse>('/auth/login', { email, password }).then((r) => r.data),
  forgotPassword: (email: string) => api.post('/auth/forgot-password', { email }),
  resetPassword: (token: string, newPassword: string) =>
    api.post('/auth/reset-password', { token, newPassword }),
  changePassword: (currentPassword: string, newPassword: string) =>
    api.post('/auth/change-password', { currentPassword, newPassword }),
};

export const usersApi = {
  me: () => api.get<UserProfile>('/users/me').then((r) => r.data),
  updateMe: (data: Partial<UserProfile>) => api.put<UserProfile>('/users/me', data).then((r) => r.data),
  byId: (id: number) => api.get<UserProfile>(`/users/${id}`).then((r) => r.data),
  list: (params?: { teamId?: number; departmentId?: number }) =>
    api.get<UserSummary[]>('/users', { params }).then((r) => r.data),
  teamMembers: (teamId: number) =>
    api.get<UserSummary[]>(`/users/team/${teamId}`).then((r) => r.data),
  createEmployee: (data: unknown) =>
    api.post<{ userId: number; email: string; temporaryPassword: string }>('/users', data).then((r) => r.data),
};

export const departmentsApi = {
  list: () => api.get<Department[]>('/departments').then((r) => r.data),
  get: (id: number) => api.get<Department>(`/departments/${id}`).then((r) => r.data),
  create: (name: string) => api.post<Department>('/departments', { name }).then((r) => r.data),
  update: (id: number, data: { name: string; description?: string | null; managerId?: number | null }) =>
    api.put<Department>(`/departments/${id}`, data).then((r) => r.data),
};

export const teamsApi = {
  list: (departmentId?: number) =>
    api.get<Team[]>('/teams', { params: { departmentId } }).then((r) => r.data),
  get: (id: number) => api.get<Team>(`/teams/${id}`).then((r) => r.data),
  create: (data: { name: string; description?: string | null; departmentId: number; teamLeadId?: number | null }) =>
    api.post<Team>('/teams', data).then((r) => r.data),
  update: (id: number, data: { name: string; description?: string | null }) =>
    api.put<Team>(`/teams/${id}`, data).then((r) => r.data),
  assignEmployee: (teamId: number, userId: number) => api.post(`/teams/${teamId}/members/${userId}`),
  assignTeamLead: (teamId: number, userId: number) => api.put(`/teams/${teamId}/team-lead/${userId}`),
};

export const onboardingApi = {
  mine: () => api.get<OnboardingSummary>('/onboarding/me').then((r) => r.data),
  forEmployee: (employeeId: number) =>
    api.get<OnboardingSummary>(`/onboarding/employee/${employeeId}`).then((r) => r.data),
  list: () => api.get<OnboardingSummary[]>('/onboarding').then((r) => r.data),
  assignMentor: (employeeId: number, mentorId: number) =>
    api.put(`/onboarding/employee/${employeeId}/mentor/${mentorId}`),
};

export const tasksApi = {
  mine: () => api.get<EmployeeTask[]>('/tasks/me').then((r) => r.data),
  forEmployee: (employeeId: number) =>
    api.get<EmployeeTask[]>(`/tasks/employee/${employeeId}`).then((r) => r.data),
  detail: (taskId: number) => api.get<EmployeeTaskDetail>(`/tasks/${taskId}`).then((r) => r.data),
  updateStatus: (taskId: number, status: EmployeeTaskStatus) =>
    api.patch<EmployeeTask>(`/tasks/${taskId}/status`, { status }).then((r) => r.data),
  assign: (data: unknown) => api.post<EmployeeTask>('/tasks', data).then((r) => r.data),
  assignFromTemplate: (data: unknown) =>
    api.post<EmployeeTask>('/tasks/from-template', data).then((r) => r.data),
  addComment: (taskId: number, text: string) =>
    api.post<TaskComment>(`/tasks/${taskId}/comments`, { text }).then((r) => r.data),
  templates: {
    list: (category?: string) =>
      api.get<TaskTemplate[]>('/tasks/templates', { params: { category } }).then((r) => r.data),
    create: (data: unknown) => api.post<TaskTemplate>('/tasks/templates', data).then((r) => r.data),
    update: (id: number, data: unknown) =>
      api.put<TaskTemplate>(`/tasks/templates/${id}`, data).then((r) => r.data),
    deactivate: (id: number) => api.delete(`/tasks/templates/${id}`),
  },
};

export const documentationApi = {
  forTeam: (teamId: number) =>
    api.get<TeamDocumentation[]>(`/documentation/team/${teamId}`).then((r) => r.data),
  create: (data: unknown) => api.post<TeamDocumentation>('/documentation', data).then((r) => r.data),
  update: (id: number, data: unknown) =>
    api.put<TeamDocumentation>(`/documentation/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/documentation/${id}`),
};

export const meetingsApi = {
  mine: () => api.get<Meeting[]>('/meetings/me').then((r) => r.data),
  get: (id: number) => api.get<Meeting>(`/meetings/${id}`).then((r) => r.data),
  create: (data: unknown) => api.post<Meeting>('/meetings', data).then((r) => r.data),
};

export const adminApi = {
  roles: () => api.get<RoleOption[]>('/admin/roles').then((r) => r.data),
  users: () => api.get<AdminUser[]>('/admin/users').then((r) => r.data),
  createStaff: (data: unknown) =>
    api.post<{ userId: number; email: string; temporaryPassword: string }>('/admin/staff', data).then((r) => r.data),
  setRoles: (userId: number, roles: string[]) =>
    api.put<AdminUser>(`/admin/users/${userId}/roles`, { roles }).then((r) => r.data),
};

export const notificationsApi = {
  list: (unreadOnly = false) =>
    api.get<AppNotification[]>('/notifications', { params: { unreadOnly } }).then((r) => r.data),
  unreadCount: () => api.get<number>('/notifications/unread-count').then((r) => r.data),
  markRead: (id: number) => api.patch(`/notifications/${id}/read`),
  markAllRead: () => api.patch('/notifications/read-all'),
};
