# Meridian — What I Would Build Next

## Priority 1 — Features that would fundamentally improve the experience

These change how trustworthy and usable the product actually is.

### 1. Automated tests + CI
Right now correctness is verified end-to-end by hand. The single most valuable
next step is a real test suite:
- **Unit tests** for the parts that carry the business rules — the task-template
  strategies and scoping, the role reconciliation (Manager/Team Lead ↔ org
  assignments), the new-employee factory, auth, and ownership checks.
- **Integration tests** over the API (spin up Postgres in a container, hit the
  endpoints).
- A **CI pipeline** that builds both projects and runs the tests on every push.

**Why it matters:** every feature below becomes safer and faster to build. It's
the foundation that lets the system evolve without regressions, and it turns
"I tested it manually" into something repeatable and provable.

### 2. Real email + a proper invite / first-login flow
The `IEmailSender` is a development stub that writes to the log, and today HR
shares a temporary password out-of-band. I would:
- Plug in a real SMTP/provider implementation (the interface is already there).
- Replace the shared temp password with a **secure invitation email**: the new
  hire receives a one-time link to set their own password on first login.

**Why it matters:** this is the new employee's very first interaction with the
company's software. Making it a clean, secure "welcome — set your password" email
instead of a password handed over in chat is a disproportionately large
improvement to the day-one experience, and it removes the assumption that a
secure side channel exists.

### 3. Complete the notification story: deadline & meeting reminders
The brief lists "approaching task deadlines" and "upcoming meetings" as
notification events, and the `NotificationType` values exist — but the only
background worker today is onboarding auto-completion, so nobody is proactively
reminded. I would add a scheduled worker that:
- notifies employees (and optionally their mentor) when a task deadline is near
  or overdue, and
- sends a reminder before an upcoming meeting.

**Why it matters:** notifications currently only fire at the moment of
assignment. Proactive reminders are what actually keep onboarding on track — they
turn the progress bar from a passive display into something that nudges people to
finish, which is the whole point of the product.

---

## Priority 2 — Features that would add significant value

Clear wins once the fundamentals above are solid.

### 1. HR / Admin analytics dashboard
Turn the data already being collected into insight: onboarding completion rates,
average time-to-complete, overdue tasks, per-department/team bottlenecks, and how
many people are currently onboarding.
**Why it matters:** the single HR employee oversees the whole organization. A
dashboard turns "monitor each person" into "see where onboarding is stuck across
the company," which is where HR's time is best spent.

### 2. Audit log viewer (Administrator)
The `audit_logs` table is populated with every security-relevant event, but
there's no screen to read it. Add an admin-only, filterable view (by action,
user, date range) with the acting user resolved to a name.
**Why it matters:** it completes a security requirement from the brief and makes
the audit trail usable for troubleshooting and accountability instead of
requiring direct database access.

### 3. Real file attachments
Attachments are currently URLs. Add proper upload/storage (e.g. object storage)
so a task can carry the actual PDF to sign or the doc to read.
**Why it matters:** many onboarding tasks *are* "read/sign this file." Supporting
real files removes a dependency on external tools and keeps everything the new
hire needs in one place.

### 4. Search, filtering, and pagination
Lists (users, tasks, templates, meetings, audit log) currently return everything.
Add server-side paging, filtering, and search.
**Why it matters:** it's cheap now and essential as data grows; it keeps the app
fast and the screens usable beyond a handful of records.

### 5. Richer meeting management
Editing, cancelling, and rescheduling with change notifications, plus calendar
export (`.ics`) and RSVP.
**Why it matters:** meetings currently support create/notify; real scheduling
needs changes and cancellations, and an `.ics`/calendar hook meets people in the
tools they already live in.

---

## Priority 3 — Nice-to-have improvements

Polish and quality-of-life. Individually small, collectively they make the
product feel finished.

- **Onboarding plans/bundles** — group templates into a named plan per role and
  apply it on hire. *Why:* less repetitive setup and more consistent onboarding.
- **Bulk operations** — CSV import of employees, bulk task assignment. *Why:*
  saves HR time during hiring waves.
- **A settings screen for reference/config data** (e.g. the auto-completion
  period) instead of `appsettings`. *Why:* lets an Administrator tune behavior
  without a redeploy — matching the brief's "maintain reference data" role.
- **Refresh tokens / session management UI** — smoother, longer sessions with
  explicit sign-out-everywhere. *Why:* rounds out the auth experience beyond the
  current per-request role refresh.
- **Accessibility, dark mode, and responsive/mobile polish.** *Why:* inclusivity
  and comfort for daily use, including on the office floor.
- **Internationalization (i18n).** *Why:* onboarding is often the first
  touchpoint for international hires; localized content is welcoming.
- **Exportable onboarding report (PDF)** of a person's progress. *Why:* a simple
  artifact HR/managers can share or archive.

---