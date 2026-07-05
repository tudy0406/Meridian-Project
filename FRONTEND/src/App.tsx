import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { Roles } from './auth/roles';
import { LoginPage } from './features/auth/LoginPage';
import { ForgotPasswordPage } from './features/auth/ForgotPasswordPage';
import { ResetPasswordPage } from './features/auth/ResetPasswordPage';
import { ChangePasswordPage } from './features/auth/ChangePasswordPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { MyOnboardingPage } from './features/onboarding/MyOnboardingPage';
import { MonitorPage } from './features/onboarding/MonitorPage';
import { ProfilePage } from './features/profile/ProfilePage';
import { MyTeamPage } from './features/team/MyTeamPage';
import { MeetingsPage } from './features/meetings/MeetingsPage';
import { NotificationsPage } from './features/notifications/NotificationsPage';
import { CreateEmployeePage } from './features/hr/CreateEmployeePage';
import { OrganizationPage } from './features/admin/OrganizationPage';
import { StaffPage } from './features/admin/StaffPage';
import { TemplatesPage } from './features/tasks/TemplatesPage';
import { ManageDocsPage } from './features/documentation/ManageDocsPage';

const SUPERVISOR_ROLES = [
  Roles.HrEmployee,
  Roles.Manager,
  Roles.TeamLead,
  Roles.Mentor,
  Roles.Administrator,
];

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />

      <Route
        element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<DashboardPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/profile/:userId" element={<ProfilePage />} />
        <Route path="/onboarding" element={<MyOnboardingPage />} />
        <Route path="/team" element={<MyTeamPage />} />
        <Route path="/meetings" element={<MeetingsPage />} />
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/change-password" element={<ChangePasswordPage />} />
        <Route
          path="/monitor"
          element={
            <ProtectedRoute roles={SUPERVISOR_ROLES}>
              <MonitorPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/templates"
          element={
            <ProtectedRoute roles={SUPERVISOR_ROLES}>
              <TemplatesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/documentation/manage"
          element={
            <ProtectedRoute roles={[Roles.TeamLead, Roles.HrEmployee, Roles.Administrator]}>
              <ManageDocsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/organization"
          element={
            <ProtectedRoute roles={[Roles.Administrator]}>
              <OrganizationPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/staff"
          element={
            <ProtectedRoute roles={[Roles.Administrator]}>
              <StaffPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/hr/employees"
          element={
            <ProtectedRoute roles={[Roles.HrEmployee, Roles.Administrator]}>
              <CreateEmployeePage />
            </ProtectedRoute>
          }
        />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
