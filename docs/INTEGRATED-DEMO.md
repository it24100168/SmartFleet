# SmartFleet integrated fleet demonstration

The four feature branches are combined with a durable backend workflow and a React fleet simulator. Robots are simulated database records, not physical hardware. The map shows illustrative warehouse routes driven by persisted mission progress; it is not a collision-avoidance or robotics physics engine. All four agents currently use deterministic rules, not an LLM.

## Start locally

From `D:\SmartFleet`, open two PowerShell terminals:

```powershell
# Terminal 1
.\scripts\start-demo.ps1

# Terminal 2
npm.cmd install --prefix web
npm.cmd run dev --prefix web -- --host 127.0.0.1
```

Open http://127.0.0.1:5173. Select a demo role on the login page and sign in. Demo accounts are `supervisor@demo.smartfleet`, `operator@demo.smartfleet`, and `technician@demo.smartfleet`; their initial password is `DemoFleet!2026`. They are created only when Simulation:Enabled is explicitly true in Development/Testing. Override `Simulation__Password` before first startup to choose a different password. A generated signing key changes on API restart, so sign in again after restarting.

The API runs at http://localhost:5078. Vite proxies `/api` and `/uploads` to it. Keep `VITE_API_BASE_URL=/api` locally. The isolated persistent database is `backend/smartfleet-demo.db`; the demo does not connect to Neon. To start a fresh, separate demo without deleting anything, set `Simulation__ConnectionString` to `Data Source=another-demo.db;Default Timeout=15` before launching.

## Demonstrate the fleet

1. Sign in as Supervisor. Choose pickup, delivery and cargo on Fleet Simulation.
2. Select **Clear route** and dispatch. Watch a robot reserve, pass Safety Guard and move. A delivery takes about 40 seconds while the API is running. The final state updates destination, battery and availability.
3. Select the mission activity card. Click each agent card to see actual stored inputs/outputs and the execution timeline. Maintenance is explicitly skipped when there are no faults.
4. Choose **Weather caution**. The robot stays Reserved until a Supervisor approves. Add notes and approve, reject or request revision. Revision releases the rover and can be replanned with the selected scenario. Approval reservations expire after ten minutes.
5. Select a moving robot and report a motor fault. It stops and enters Maintenance. The real catalog agent diagnoses the report. A Technician or Supervisor uses **Recover to maintenance (demo)** to simulate a recovery crew, then marks it repaired. A damaged motor does not drive itself. The interrupted mission stays Failed; start a new plan explicitly.
6. Select **Severe weather** to demonstrate safe rejection. No robot should start moving.
7. Use the Maintenance Queue or Flutter app to attach a real photo. Use the Technician account for repair actions. Demo charging is available only to supervisors for idle/charging robots.

The demo's weather values are explicit fixtures. Mission planning, database reservations, catalog lookup, safety checks, approval, audit logs and completion execute in the backend. An offline backend does not advance mission progress. Multiple robots may share an illustrated aisle; physical routing and collision prevention are outside this prototype.

## Mobile

Merge includes both dispatch/GPS and breakdown/camera screens plus native scaffolding. Android emulator defaults to `http://10.0.2.2:5078/api`. A physical device needs a reachable API URL:

```powershell
cd mobile
flutter pub get
flutter analyze
flutter test
flutter run --dart-define=API_BASE_URL=https://your-api.example/api
flutter build apk --release --dart-define=API_BASE_URL=https://your-api.example/api
```

Release builds should use HTTPS. Debug Android permits local HTTP; main Android declares networking/location permissions and iOS declares camera/photo/location usage descriptions. The mobile login has Operator, Technician and Supervisor demo prefill buttons. Supervisor login unlocks role-gated fleet demo charging, but approval decisions remain on the React web dashboard; mobile has no approval screen. The breakdown service resolves a display identifier such as RO-04 to its UUID. Dispatch history refreshes every three seconds. On 4 October 2026 a Pixel 7 Android 16 emulator successfully installed the debug APK, signed in, loaded the six-rover PostgreSQL fleet, and completed a Fragile/High dispatch from A1 to B3 with RO-03. Native camera, GPS, breakdown and the Critical two-client approval demonstration still need device verification. Run `flutter pub get` to reconcile the lockfile before building on a new machine.

If this network presents an untrusted certificate for Google Storage, Flutter's [documented community mirror](https://docs.flutter.dev/community/china) worked for the Android artifact download. In the PowerShell process used for the build:

```powershell
$env:FLUTTER_STORAGE_BASE_URL='https://storage.flutter-io.cn'
cd mobile
flutter build apk --debug --dart-define=API_BASE_URL=http://10.0.2.2:5078/api
```

The debug APK is at `mobile/build/app/outputs/flutter-apk/app-debug.apk`. The Android 16 emulator omitted some login and profile header glyphs under Impeller. The debug manifest now selects Flutter's alternate renderer for this local demo; the login logo/title and profile avatar/name/email were visually verified. This setting affects only debug builds; release builds still use Flutter's default renderer and need separate visual QA on the target device. Flutter marks the Impeller opt-out as deprecated, so remove the workaround after the upstream rendering issue is resolved. The mirror is community operated, so use a network-approved trust root or normal Google Storage when available. Keep TLS verification enabled.

## Validation

```powershell
dotnet test backend/backend.sln
npm.cmd run build --prefix web
node scripts/http-smoke.mjs
```

The HTTP script requires an explicitly enabled demo API at 5078 and creates labelled test requests/reports. It verifies registration privileges, ownership, status bypass protection, idempotent start/approval, pause/revise/resume, breakdown stop/repair, weather rejection and delivery completion. The regression suite uses real relational SQLite for workflow tests; existing agent tests are retained. PostgreSQL-specific concurrency/load behavior has not been validated here. No browser automation surface was available, so visual browser/device QA remains a manual check.

## PostgreSQL deployment

Rotate the Neon password that was previously committed; removing it from current files does not remove it from Git history. No production database was migrated by this work.

Set `Simulation__Enabled=false`, a private `JwtSettings__Secret` (at least 32 bytes), an ADO.NET PostgreSQL `ConnectionStrings__DefaultConnection`, `WeatherSettings__Provider=OpenMeteo` (or `OpenWeather` with a private `WeatherSettings__ApiKey`), and the deployed React origin in `Cors__AllowedOrigins__0`. Missing weather is blocked, not replaced with clear weather. Provision privileged accounts through private `Provisioning__Supervisor__*` and `Provisioning__Technician__*` app settings as described in the [Azure + Neon runbook](AZURE-NEON-DEPLOY.md); registration always creates Operator accounts.

Apply the preserved branch migrations and `IntegrateFleetWorkflow` to a disposable PostgreSQL database first. The integration migration adds run progress, reservation data, correlation IDs and concurrency tokens. It does not delete existing data. Review the generated SQL and check the final schema before migrating a shared environment:

```powershell
dotnet ef migrations script --project backend/backend.csproj --output migration-review.sql
```

The design-time factory intentionally uses a nonproduction placeholder connection for schema generation. For an actual migration, supply `--connection` securely or opt into startup migration with `Database__ApplyMigrations=true` and the configured connection. Never commit connection strings or generated SQL containing credentials. Provide persistent storage for `backend/wwwroot/uploads` when deploying.

## Follow-up fixes: routes, recovery, scheduling and time

- A robot already at pickup no longer leaves and returns to the same pickup before delivery. Fleet positions now come from a consistent backend snapshot. Motor faults retain the last persisted route position until explicit simulated recovery to the maintenance area. Recovery is an immediate demonstration transfer, not autonomous driving or towing physics.
- Normal Dispatch Requests queue until approximately 40 seconds before the delivery target. A five-second intake window groups near-simultaneous submissions. Due requests sort by earliest target, then Critical > High > Medium > Low, then submission time. Running missions are never preempted. With two eligible robots, both orders can proceed; priority determines allocation order, not exclusive use of the fleet. Missing eligible capacity queues work and retries instead of failing. Targets are best-effort; approval delays, maintenance and capacity can make delivery late. The Fleet Simulation scenario button remains an immediate demonstration action.
- Map zones are supplied by GET /api/workflows/zones. Both backend dispatch creation and workflow start reject unknown or identical zones. Historic invalid orders remain in history and cannot be restarted.
- Web times are explicitly IST (UTC+05:30), independent of the browser time zone. API storage remains UTC. Input defaults and conversion use IST, including midnight rollover. Earlier timestamps entered through the buggy UTC-labelled form cannot be reconstructed automatically.
- Refresh reloads current state and reports completion; it does not recharge robots, restart missions or erase history. Reset example form restores only the legacy telemetry example fields and explains this on screen. There is no destructive fleet reset button.
- Select a robot using the map or Inspect robot dropdown. Use Demo charge for idle/charging robots. Faulted robots require recovery and repair before reuse. A route can require more than the general 40% floor (A1 to B3 requires 44%); the rejection message now shows the actual threshold.

Validation: 32 backend tests, web production build, IST conversion checks, and live HTTP workflow/recovery/scheduling regressions. Browser interaction checks confirmed dropdowns, IST display, and refresh/reset feedback; screenshot capture timed out. PostgreSQL and native Flutter validation remain outstanding.

Run additional regression checks against a separate, explicitly enabled demo API so test orders do not mix with a presentation:

```powershell
# Configure a separate Simulation__ConnectionString and API port 5080 first.
$env:SMARTFLEET_TEST_URL='http://localhost:5080/api'
node scripts/http-smoke.mjs
node scripts/workflow-regression.mjs
node scripts/ui-time-check.mjs
```
