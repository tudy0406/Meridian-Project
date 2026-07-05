/** Shared API DTO types, mirroring the backend contracts. */

export type Role =
  | 'Administrator'
  | 'HR Employee'
  | 'Manager'
  | 'Team Lead'
  | 'Mentor'
  | 'Employee';

export interface LoginResponse {
  token: string;
  expiresAt: string;
  userId: number;
  fullName: string;
  email: string;
  roles: Role[];
  isOnboarding: boolean;
}

export interface UserProfile {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  jobTitle?: string | null;
  departmentId?: number | null;
  departmentName?: string | null;
  teamId?: number | null;
  teamName?: string | null;
  inOfficeDays?: string | null;
  isOnboarding: boolean;
  onboardingStatus?: string | null;
  canAccessOnboarding: boolean;
  roles: Role[];
}

export interface UserSummary {
  id: number;
  fullName: string;
  email: string;
  jobTitle?: string | null;
  departmentId?: number | null;
  teamId?: number | null;
  roles: Role[];
}

export type TaskCategory = 'Hr' | 'Department' | 'Team' | 'Personal';
export type TaskPriority = 'Low' | 'Medium' | 'High';
export type EmployeeTaskStatus = 'NotStarted' | 'InProgress' | 'Completed';

export interface EmployeeTask {
  id: number;
  onboardingEmployeeId: number;
  title: string;
  description?: string | null;
  category: TaskCategory;
  status: EmployeeTaskStatus;
  priority: TaskPriority;
  deadline?: string | null;
  assignedById: number;
  assignedByName?: string | null;
  contactPersonId?: number | null;
  contactPersonName?: string | null;
  completedAt?: string | null;
  assignedAt: string;
}

export interface Attachment {
  id: number;
  fileName: string;
  url: string;
}

export interface TaskComment {
  id: number;
  authorId: number;
  authorName: string;
  text: string;
  createdAt: string;
}

export interface TaskHistoryEntry {
  status: string;
  changedById: number;
  changedByName: string;
  changedAt: string;
}

export interface EmployeeTaskDetail {
  id: number;
  onboardingEmployeeId: number;
  title: string;
  description?: string | null;
  requirements?: string | null;
  category: TaskCategory;
  status: EmployeeTaskStatus;
  priority: TaskPriority;
  deadline?: string | null;
  assignedById: number;
  assignedByName?: string | null;
  assignedAt: string;
  contactPersonId?: number | null;
  contactPersonName?: string | null;
  completedAt?: string | null;
  attachments: Attachment[];
  comments: TaskComment[];
  history: TaskHistoryEntry[];
}

export interface OnboardingSummary {
  onboardingId: number;
  employeeId: number;
  employeeName: string;
  teamName?: string | null;
  mentorId?: number | null;
  mentorName?: string | null;
  status: string;
  progressPercentage: number;
  startDate: string;
  taskCount: number;
  completedTaskCount: number;
}

export interface TaskTemplate {
  id: number;
  title: string;
  description?: string | null;
  requirements?: string | null;
  category: TaskCategory;
  priority: TaskPriority;
  estimatedCompletionDays: number;
  departmentId?: number | null;
  teamId?: number | null;
  isActive: boolean;
}

export interface Department {
  id: number;
  name: string;
  description?: string | null;
  managerId?: number | null;
  managerName?: string | null;
  teamCount: number;
}

export interface Team {
  id: number;
  name: string;
  description?: string | null;
  departmentId: number;
  departmentName: string;
  managerId?: number | null;
  managerName?: string | null;
  teamLeadId?: number | null;
  teamLeadName?: string | null;
  memberCount: number;
}

export interface TeamDocumentation {
  id: number;
  teamId: number;
  title: string;
  category: string;
  content: string;
  updatedAt: string;
}

export interface Meeting {
  id: number;
  title: string;
  description?: string | null;
  organizerId: number;
  organizerName: string;
  dateTime: string;
  location?: string | null;
  onlineLink?: string | null;
  participants: { userId: number; fullName: string }[];
}

export interface AdminUser {
  id: number;
  fullName: string;
  email: string;
  jobTitle?: string | null;
  departmentId?: number | null;
  departmentName?: string | null;
  teamId?: number | null;
  teamName?: string | null;
  isOnboarding: boolean;
  isActive: boolean;
  roles: Role[];
}

export interface RoleOption {
  id: number;
  name: Role;
}

export interface AppNotification {
  id: number;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}
