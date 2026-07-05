# Meridian — Design Assumptions

## About the users

### Who uses the application?
- **New Employee** — the person being onboarded; the primary end user.
- **HR Employee** — the central owner of onboarding; per the brief there is
  effectively one.
- **Administrator** — manages accounts, roles, organizational structure, and
  reference data.
- **Manager / Team Lead / Mentor** — supervisory responsibilities held by
  regular employees. One person can hold several at once (e.g. a Team Lead who
  also mentors).
- **Colleagues / team members** — seen through profiles and team pages.

**Assumptions & why:**
- **Internal, authenticated-only app — no public sign-up.** Every user is an
  existing company member, so the app uses role-based access control and has no
  self-registration.
- **Accounts are provisioned, never self-created.** HR creates new-employee
  accounts; the Administrator creates other staff accounts and assigns roles.
  The user therefore always receives credentials from someone else.
- **One account, many hats.** Responsibilities are additive and a person's
  permissions are the union of their roles — this matches real teams where the
  same person mentors *and* leads.
- **Positional roles follow assignments.** "Manager" and "Team Lead" are derived
  from *being* a department's manager or a team's lead — not toggled by hand — so
  the org chart is the single source of truth and permissions can't drift out of
  sync with reality.

### What does the user already know on first login?
- The new employee knows **their email and a temporary password**, handed over
  by HR out-of-band (the app shows that password once to HR at creation). We
  assume a **secure side channel** exists to deliver it (in person, corporate
  email, company chat).
- They do **not** need to know the org structure, their tasks, or their mentor
  beforehand — the app surfaces all of it after login.
- **Basic web literacy only.** No training is required: the UI is
  self-explanatory and role-scoped, so users see only what applies to them.
- First-time users are **expected (but not forced) to change** their temporary
  password via *Security → Change Password*.

---

## About the data

### Who enters the information?
- **Administrator:** departments, teams, staff accounts, role assignments,
  department managers, team leads.
- **HR:** new-employee accounts (department/team/mentor), HR task templates;
  monitors all onboarding.
- **Manager:** department-level templates/tasks; edits their department.
- **Team Lead:** team information, team templates/tasks, team documentation,
  meetings.
- **Mentor:** personal templates/tasks for their mentees, meetings.
- **New Employee:** marks tasks complete, comments, and edits a limited set of
  their own profile fields (phone, job title, in-office days).

We assume onboarding content is **authored by whoever is responsible for it** and
flows automatically to the right hires (via department/team/mentor scoping),
rather than being re-entered by hand for each person.

### When is the information added?
1. **Before a hire (setup):** the organizational structure (departments, teams,
   managers, leads) and the reusable **task templates** must already exist. We
   assume HR/managers/leads/mentors prepare templates ahead of time so onboarding
   is consistent.
2. **At account creation:** when HR creates the employee, the system **snapshots
   the applicable templates into concrete tasks** — company-wide HR tasks, plus
   the tasks for their department, their team, and their mentor. We assume the
   department and team are known at creation (a mentor is optional).
3. **During onboarding (ongoing):** tasks are completed, meetings scheduled,
   documentation updated, mentors (re)assigned.

### What happens if information is missing or incorrect?
- **Validated on both client and server; the server is authoritative.** Bad
  input returns a clear error rather than a crash or silent corruption.
- **Structural prerequisites are enforced:** you cannot onboard into a
  non-existent team; a mentor must belong to the employee's team; a team lead
  must be a member of the team; a department manager must belong to the
  department. Violations are rejected with a message.
- **Optional-but-missing data degrades gracefully:** no manager/team lead yet →
  shown as "—"; no mentor → no personal tasks assigned; the app keeps working.
- **Scope prevents leakage:** a template's department/team scope is derived from
  its author, so a Legal template never reaches an Engineering hire.
- **Recoverable inconsistencies self-heal on startup** (idempotently): each
  team's manager is re-synced to its department's manager, and legacy
  authorless templates are given a real author. We assumed automatic
  reconciliation is preferable to manual cleanup.
- **Password reset** uses a single-use email token; in development the "email"
  is written to the server log (the sender is a stub). We assume a real email
  provider is configured in production.

---

## About the context

### What device does the new employee use on day one?
- The application is designed as a deployable web app, so the new employee needs just a computer with browser installed and internet access
- We assume a **reasonably current browser** (WebSocket support, for the live
  notifications); legacy browsers are out of scope.

### Do they have access before their first working day?
- **The account can exist before day one** — HR can provision it in advance, and
  the onboarding process begins at creation. We assume HR sets accounts up
  slightly ahead of the start date.
- **Login is not gated by start date**, so if credentials are shared early,
  **pre-boarding access is possible** (viewing tasks, team info, and meetings
  before day one). We assumed this is *desirable but not required* — a smoother
  first day — and nothing breaks if credentials are only handed over on day one.
- We assume **day-one readiness** (account provisioning in other systems) is    handled by HR **outside** Meridian; the
  app only assumes the person can already reach it.

---

## Additional assumptions

### Onboarding lifecycle
- An onboarding is **complete at 100% task completion**, or is **auto-completed
  after a configurable period (default 90 days)** even if tasks remain — assuming
  onboarding is time-bounded and shouldn't stay open indefinitely.
- The `IsOnboarding` flag means "this account has an onboarding process"; it
  stays set after completion so the person can still review it, while the status
  reflects *Completed*.

### Notifications & content
- Notifications are **in-app and real-time** (a live bell + a notifications list).
  We assume users work inside the app, so we did not assume email/SMS/push for
  every event.
- **Meeting links and task attachments are URLs**, not uploaded files — we assume
  documents live in existing tools (shared drive, wiki) and Meridian references
  them.
