# Supervisor rover management verification

On 6 October 2026, the `assessment/final-verification` branch gained two
Supervisor-only operations: `POST /api/rovers` registers an idle rover with
100% demo battery, and `PUT /api/rovers/{id}/configuration` changes the code
and mapped zone of an idle, unassigned rover without an open breakdown.
Both accept only a `RO-##` or `RO-###` code and a known warehouse zone. The
database's unique rover-code index protects against duplicate registration.
The update uses a conditional database operation so a rover reserved between
page load and Save is not relocated. Raw status, battery and mission overrides
remain disabled. React Fleet Telemetry now provides Register and Edit controls
to Supervisors; the mobile fleet remains a read-only view.

Verification in this workspace:

- PostgreSQL-backed `dotnet test backend.Tests/backend.Tests.csproj --no-restore --verbosity quiet`: **62 passed, 0 failed, 0 skipped**. The new test covers registration, duplicate and invalid-zone rejection, idle update, and reserved-rover refusal. It used the isolated local PostgreSQL container on `127.0.0.1:55432`; each PostgreSQL test creates and drops only its random test database.
- React `npm.cmd test`: **6 passed**.
- React `npm.cmd run build`: passed TypeScript and Vite production build.
- `git diff --check`: passed.

These checks establish local behavior. The new feature is **not yet on main or
the hosted Azure deployment**. After a teammate reviews the PR, merge it,
confirm backend/web CI and Azure deployments, then test Register and Edit with
a Supervisor account in the hosted UI. Do not create a demo rover on the
shared production database until the team is ready to show and retain it.
