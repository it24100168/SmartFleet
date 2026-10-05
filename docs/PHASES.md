# Specification gap implementation

This is a delivery checklist, not a claim that the assignment is fully satisfied. See the dated compliance review for the original assessment.

Current milestone: phase 1 is verified locally; phase 2's workflow is implemented and verified through Flutter web and React. On 4 October 2026, the Android emulator also completed a native operator dispatch against PostgreSQL; the native Critical request plus React supervisor approval sequence still needs presentation evidence. Phase 3 now includes persisted checklist states, typed output validation, weather attempts and bounded scheduling retries; see [remaining observability limits](evidence/phase-3-validation.md). Phase 4 adds mobile fleet inspection/filtering and supervisor demo charging. Phase 5 has a working local debug APK, while cloud deployment remains deferred at the team's request. Phase 6 has [local presentation preparation](LOCAL-READINESS.md).

| Phase | Deliverable | Acceptance gate |
| --- | --- | --- |
| 1 | PostgreSQL persistence and repeatable local setup | Fresh migrations, constraints, rollback, concurrent reservation and persisted approval tests on PostgreSQL; CI runs those tests. |
| 2 | Cross-client approval workflow | Flutter operator requests Critical delivery; React supervisor approves; Flutter receives completion. Authorization and no movement before approval are tested. Capture a real device/client demonstration. |
| 3 | Accurate agent execution evidence | Persist actual step states, validate plan/output contracts, record tool attempts/timings/errors, bounded technical retries; golden and adversarial cases. |
| 4 | Complete each student's component | Fill verified endpoint, React and Flutter business-operation gaps; component/widget tests, role validation, filtering and reports. |
| 5 | Deployment and evaluation | Hosted API/React/PostgreSQL, Android APK, end-to-end and performance evidence, successful hosted CI and evaluator access. |
| 6 | Submission and presentation | ERD/architecture/workflow diagrams, required ADRs, consolidated report, truthful AI declarations, personally authored reflections, video and viva rehearsal. |

## Run the PostgreSQL assessment environment

Prerequisites: Docker Desktop running, .NET 8, Node dependencies installed, Flutter for mobile tests.

From the repository root in PowerShell:

```powershell
./scripts/start-assessment.ps1
```

This creates an isolated PostgreSQL 16 container on `127.0.0.1:55432`, applies the EF migrations and starts the API on `http://localhost:5078`. Its named volume persists restarts. A generated local password stays in ignored `.demo/assessment-local.json`; retain this file while retaining the volume. It enables explicit simulated movement, weather and demo accounts while storing records in PostgreSQL. Existing SQLite data is not migrated or deleted. Stop another API on port 5078 first.

In another terminal:

```powershell
npm run dev --prefix web
```

`GET /api/health` reports `databaseProvider: PostgreSQL`. The SQLite fallback remains available through `scripts/start-demo.ps1`. Setting `Simulation:Enabled` alone no longer selects SQLite; that launcher explicitly sets the provider.

Run backend PostgreSQL tests in one PowerShell session:

```powershell
./scripts/start-assessment.ps1 -DatabaseOnly
dotnet test backend/backend.sln
```

Each PostgreSQL test creates and drops only its own randomly named `smartfleet_test_*` database on localhost. Existing assessment records are preserved. Without `SMARTFLEET_TEST_POSTGRES`, these tests explicitly skip; a skipped run is not PostgreSQL evidence. GitHub Actions provisions its own PostgreSQL service.

## Present the two-client flow

1. Start the PostgreSQL API and React. On Flutter, configure the same API (`--dart-define=API_BASE_URL=.../api`). Android emulator uses `10.0.2.2:5078`; a real phone needs a reachable host and suitable development network configuration.
2. Sign into Flutter as `operator@demo.smartfleet`, password `DemoFleet!2026`. Select map pickup/drop-off zones, Standard cargo, Critical priority and **Next available slot**. Submit.
3. Open My History. After the scheduling intake delay, the request should show **AwaitingApproval**. The selected rover is reserved with zero movement. Other available rovers can serve other orders while approval is pending.
4. Sign into React as `supervisor@demo.smartfleet` with the same demo password. Open approval requests, inspect the policy reason and approve. An operator cannot approve its own request.
5. Watch the fleet progress to delivery and Flutter history refresh to **Completed**. Show the matching request ID and execution logs in both clients.
6. Repeat with reject, an unsafe weather scenario, or a motor fault. Approval never bypasses the existing safety checks. Approval expires if left waiting too long; submit a fresh request for another demonstration.

Critical priority determines ordering among equal delivery targets; it does not override human authorization or hazards. All critical dispatches now require supervisor approval, so existing automation that expected automatic Critical execution must approve first.

## Mobile validation

```powershell
cd mobile
flutter analyze --no-pub
flutter test --no-pub
flutter test --no-pub --dart-define=LIVE_POSTGRES=true --dart-define=API_BASE_URL=http://localhost:5078/api test/postgres_workflow_live_test.dart
```

The last command creates a real request using Flutter's service, approves through a separate authenticated HTTP session, and checks completion through that same Flutter service. It requires the running local PostgreSQL assessment API and available fleet capacity. It is **HTTP integration evidence**, not evidence of a native device or React UI interaction. Widget tests separately exercise the mobile form and refreshed status display. Record the real two-client demonstration before declaring phase 2's presentation gate complete.

For a browser-based rehearsal of the Flutter UI, run `flutter run -d web-server --web-port 5180 --no-web-resources-cdn --dart-define=API_BASE_URL=http://localhost:5078/api` from `mobile`, then visit `http://localhost:5180`. The assessment launcher allows this local origin. Serving rendering resources locally avoids the external CDN dependency encountered during verification. This does not replace testing the native app.
