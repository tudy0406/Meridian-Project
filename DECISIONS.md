
# Meridian — Design & Implementation Decisions

For this project, I wanted to showcase my **software engineering** skills rather than simply writing a large amount of code. 

My goal was not just to create a working onboarding application, but to demonstrate how I approach software development from an engineering perspective. Every major technology, architectural pattern, and design decision was chosen to solve a specific problem or support future growth. I wanted the project to reflect how I think about building software that is easy to understand, maintain, and evolve over time.

By using modern AI coding agents to handle repetitive boilerplate and routine implementation tasks, I was able to spend more time on what I believe matters most: designing a clean architecture, making thoughtful engineering decisions, and building a system that is reliable, maintainable, and easy to extend.

---

## Product decisions

### Which features were included?
The build covers the full onboarding loop described in the brief:

- **Authentication & security** — JWT login, BCrypt password hashing, role-based
  access control with ownership rules, self-service password reset (email token)
  and change, rate-limited auth endpoints, audit logging.
- **Accounts & roles** — HR onboards new employees; an Administrator creates
  existing-staff accounts and manages roles; profiles for everyone.
- **Organization** — departments and teams with a single source of truth for
  managers and team leads.
- **Onboarding process** — per-employee progress tracking, a role-scoped
  monitor, mentor assignment, and time-based auto-completion.
- **Tasks** — reusable, category-scoped templates that auto-assign to the right
  hires; custom and template-based assignment; rich task detail (description,
  requirements, attachments, comments, completion history) grouped by category.
- **Meetings** — scheduling onboarding employees with participant notifications.
- **Team documentation** — categorized, team-lead-maintained onboarding docs.
- **Notifications** — in-app and **real-time** (SignalR).

### How were they prioritized?

1. **The core onboarding loop before the periphery** — account → auto-generated
   tasks → progress → notifications is the product's reason to exist, so it was
   built and made to work end-to-end first.
2. **The functional requirements in the brief came first** — specs for each role.
3. **Iterative refinement** — features such as
   category-grouped tasks, expandable meeting cards, clickable profile
   references, and the manager/team-lead coherence fixes were prioritized as
   app testing performed (see *UX decisions*).

### What was intentionally left out of scope, and why?
- **An audit-log viewer UI.** Audit events are captured in the database, but no
  admin screen reads them yet — the writing side is the security requirement; the
  viewer is a clean, additive follow-up. Left out to keep focus on the core loop.
- **File upload / storage.** Attachments are stored as **URLs** to existing tools
  (shared drive, wiki). A blob store adds real infrastructure for little
  onboarding value at this scale.
- **Email / SMS / push notifications.** Notifications are in-app + real-time; the
  email sender is a development stub. Wiring a real provider is a deployment
  concern, not a product one.
- **Multi-tenancy, reporting/analytics dashboards, and a calendar view.** Out of
  scope for a single ~200-person company that onboards 2–3 people/month.
- **Automated test suite.** correctness was verified
  end-to-end against the running API for every feature rather than with unit/
  integration tests — a conscious trade-off, and the most important gap (see
  below).
- **Full destructive CRUD** (hard-deleting departments/teams/users) and
  **list pagination/search**. Not needed at this scale; deactivation and simple
  lists suffice for now.

---

## Technical decisions

### Why this database structure?
- **Relational (PostgreSQL) in ~3NF via EF Core**, as the brief mandates.
  Onboarding data is highly relational and integrity-sensitive, so a relational
  model with foreign keys is the natural fit.
- **One `User` table for every person**, with responsibilities modelled as
  **roles** (`UserRole` join). Avoids duplicate accounts and lets one person be
  Employee + Mentor + Team Lead at once.
- **`TaskTemplate` separate from `EmployeeTask`, copied not linked.** When
  onboarding begins, templates are *materialized* into standalone tasks, so later
  template edits never rewrite an in-progress onboarding and history is
  preserved. This is the single most important schema decision.
- **`Onboarding` separate from `User`.** Transient onboarding state (status,
  progress, mentor, dates) is kept apart from permanent employee data, so it can
  complete/expire independently.
- **Positional roles are both a role *and* a structural link** —
  `Department.ManagerId` / `Team.TeamLeadId` are the source of truth, and the
  Manager/Team Lead *roles* are reconciled from them. This resolved a real class
  of bugs where the role and the assignment drifted apart.
- **Enums stored as strings** for readable, self-documenting rows; a
  cross-cutting **`AuditLog`** table for security events.

### Why these technologies?

The project uses **ASP.NET Core Web API** on the backend and **React + TypeScript** on the frontend because these were the recommended technologies. This let me focus less on choosing frameworks and more on designing the application itself.

The remaining technologies were chosen based on the project's needs:

- **PostgreSQL** instead of **SQLite** because this is a multi-user application where several people can perform actions simultaneously. PostgreSQL handles concurrent access much better, integrates seamlessly with Entity Framework Core, and leaves room for future growth.
- **JWT Bearer** for stateless authentication between the API and the React frontend.
- **BCrypt.Net** for secure password hashing.
- **SignalR** for real-time notifications and updates.
- **Vite**, **React Router**, **Axios**, and the **SignalR client** for a lightweight and modern frontend development experience.

### Why a modular monolith?

Instead of using microservices, I chose a **modular monolith** with a feature-based structure and a layered architecture (**Controller → Service → Repository**).

Although microservices are powerful, they would add unnecessary complexity for an internal application of around 200 employees. Features such as Authentication, Tasks, and Notifications don't need to be deployed independently, so a modular monolith keeps the project much simpler to develop, test, and maintain.

Each feature has its own controllers, services, repositories, and DTOs, providing good separation between modules while keeping everything in a single application. Another advantage is that a well-structured modular monolith provides a clear migration path to a microservice architecture in the future, if the application's scale or requirements ever make that necessary.

**Design Patterns** — I used each pattern to solve a concrete problem in the system, not just because it is a common design pattern.

**Repository (+ Unit of Work)** — Keeps the business logic independent of the database. Services define **what** data they need, while the repositories and `DbContext` handle **how** it is stored. This keeps Entity Framework Core details out of the business layer, making the application easier to maintain, test, and extend.

**Dependency Injection** — Decouples modules by depending on interfaces instead of concrete implementations. Each feature registers itself (e.g., `Add<Feature>Feature()`), making the modular monolith easy to compose and allowing components such as repositories, password hashers, or email services to be replaced without affecting the rest of the application.

**Factory** — Centralizes the creation of a new employee. Creating an employee involves multiple steps—creating the user account, assigning roles, creating the onboarding process, and assigning the initial tasks. The factory ensures every employee is created consistently while keeping this logic out of the service layer.

**Strategy** — Selects the appropriate task templates based on their category. Each category (HR, Department, Team, and Personal) has its own strategy. Adding a new category only requires creating a new strategy, without modifying the existing ones, following the **Open/Closed Principle**.

**Observer (Domain Events)** — Allows different parts of the system to react to events without being tightly coupled. When a task is assigned or completed, a meeting is scheduled, or documentation changes, an event is published and independent handlers create notifications and deliver them in real time through SignalR. This cleanly implements the requirement that employees and supervisors are notified immediately about important events.

**Audit Logging** — A dedicated audit logger records security-relevant actions, such as logins, account creation, role changes, and organizational updates. Treating auditing as a cross-cutting concern keeps logging consistent across the entire application instead of duplicating it in each feature.

### If there were more time, what would be built differently?

- **Automated tests** — the biggest gap. Unit tests for services/strategies and
  integration tests over the API.
- **Real email + token revocation / refresh tokens** — currently role changes
  take effect by re-reading roles from the DB each request (which works well);
  a proper refresh-token flow would round it out.
- **File storage for attachments**, **pagination/filtering/search** on lists,
  and an **audit-log viewer**.
- **Production hardening** — containerized backend image, CI/CD, TLS termination
  at a proxy, and secrets via a vault rather than config placeholders.
- **Frontend polish** — richer loading/empty/error states and optimistic UI on
  task actions.

---

## UX decisions

### Why this user flow?
- **Role-adaptive, not role-siloed.** Rather than separate apps per role, one UI
  adapts: navigation is filtered by role, the "Onboarding Process" tab appears
  only for people who actually have an onboarding, and supervisory actions
  (assign task, edit team/department) show only where permitted. Users see only
  what applies to them, so no training is needed.
- **A single Profile page serves both "my profile" and "an employee I
  supervise."** The same screen adapts based on whether you're viewing yourself
  or someone in your scope — one mental model instead of two.
- **Consistency of interaction.** Tasks are expandable accordions grouped by
  category, and meetings reuse the *same* collapsible card pattern — learn it
  once, use it everywhere. Timestamps use a single `HH:MM` format app-wide.
- **People are always reachable.** Names throughout — team members, meeting
  participants, task assigners, managers, team leads — are clickable links to the
  relevant profile.
- **The org chart is the single source of truth.** Managers and team leads are
  assigned only on the Organization page (and shown read-only in Staff & Roles),
  which removed a confusing path where the same fact could be set in two places
  and disagree.
- **Immediate feedback.** Progress bars, the notification bell, and task lists
  update in real time via SignalR, so a supervisor's action is visible to the
  new employee without a refresh.

Development was **iterative and feedback-driven**. Many concrete improvements came directly from testing and review, including:

- Tasks now show the **template's author** as "assigned by," instead of the HR/Admin who created the account.
- Task lists are **grouped by category** (HR / Department / Team / Personal).
- Meetings became **collapsible cards**, matching the task layout, and "Join online" became a simple link.
- **Clickable profile references** were added across tasks, meetings, and the team page.
- **Manager / Team Lead** became **derived** from organizational assignments and read-only in Staff & Roles, with role changes taking effect immediately instead of after re-login.
- Manager and Team Lead assignments were **restricted to members** of the corresponding department or team.
- **Templates were scoped** by department, team, and creator, so users only see templates relevant to them.
- The standalone **"My Tasks"** page was merged into **"My Onboarding"** under the progress section.
- The application was moved to **HTTPS** for local development.

**Another user** also tested the application from the perspective of a new employee. Based on their feedback, I concluded that an integrated **chatbot assistant** would be a valuable future addition, helping new employees quickly find information about the onboarding process, assigned tasks, and company resources.
