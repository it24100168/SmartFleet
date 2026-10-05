# SmartFleet

SmartFleet is a database-backed **warehouse rover simulation** for dispatch, fleet monitoring, breakdown diagnosis and supervisor authorization. Operators submit deliveries in Flutter; a React dashboard shows the shared fleet and lets Supervisors review high-impact requests. An ASP.NET Core API enforces the rules, stores state in PostgreSQL, and invokes four typed, deterministic agent components. The rovers and routes shown on the map are simulated; this repository does not control physical hardware.

The [integrated architecture and cross-client workflow](docs/architecture/integrated-system.md), [database ER diagram](docs/architecture/database-erd.md) and [agent tool-permission map](docs/architecture/agent-tool-permissions.md) show how the parts connect. The [full 17-page requirements audit](docs/evidence/requirements-full-2026-10-05.md) distinguishes implemented features from remaining assessment evidence.

## Technology and roles

| Layer | Implementation |
| --- | --- |
| API | .NET 8 ASP.NET Core, EF Core/Npgsql, JWT roles, validation, Swagger in Development or when explicitly enabled for assessment |
| Database | PostgreSQL 16 for the local assessment setup; EF migrations and seed data |
| Web | React, TypeScript, Vite, React Router and AuthContext |
| Mobile | Flutter/Dart, go_router, Provider, secure token storage, camera and GPS integration |
| Agent workflow | Mission Planner, Dispatch & Telemetry, Maintenance Mechanic and Safety Guard, coordinated by a C# orchestrator |
| Outside weather service | Keyless Open-Meteo by default outside demo, or configured OpenWeather, called only by the API; local demo fixtures are explicitly simulated |

Operator self-registration creates only Operator accounts. Technician and Supervisor access must be provisioned by trusted backend configuration or demo seed data. The API checks authorization regardless of what either client displays.

## Run locally with PostgreSQL

Prerequisites: Docker Desktop, .NET 8 SDK, Node.js/npm, and Flutter plus Android Studio to run the Android app. From a PowerShell session in the repository root:

```powershell
./scripts/start-assessment.ps1
```

The launcher starts an isolated PostgreSQL container on port `55432`, applies migrations and starts the API at `http://localhost:5078`. It creates a local password in ignored `.demo/assessment-local.json`; keep that file with the Docker volume. The API health check is `http://localhost:5078/api/health`, and Development Swagger UI is `http://localhost:5078/swagger`. In separate terminals:

```powershell
npm run dev --prefix web
```

```powershell
cd mobile
flutter run -d emulator-5554 --dart-define=API_BASE_URL=http://10.0.2.2:5078/api
```

React runs at `http://localhost:5173`. See the [mobile instructions](mobile/README.md) if the emulator has another device ID or you use a physical phone. The [integrated demo guide](docs/INTEGRATED-DEMO.md) provides the demo accounts and the Critical dispatch → React approval → mobile completion walkthrough. Demo accounts, simulated charging, movement and weather are for evaluation, not production services.

## Verification

```powershell
./scripts/start-assessment.ps1 -DatabaseOnly
dotnet test backend/backend.sln
npm run build --prefix web
cd mobile
flutter test --no-pub
flutter analyze --no-pub
```

The PostgreSQL tests use `SMARTFLEET_TEST_POSTGRES`, which the launcher sets in the same PowerShell session. Without it, database tests skip. The [phases checklist](docs/PHASES.md) and [evidence files](docs/evidence) record what has actually passed and what remains. The [native/React Critical workflow](docs/evidence/native-react-critical-workflow-2026-10-05.md) and [live Open-Meteo observation](docs/evidence/weather-live-2026-10-05.md) have local evidence. Cloud deployment, a final public-API Android APK, broader interaction tests and the group submission artifacts are still open.

## Repository map

- `backend/` and `backend.Tests/`: API, EF model/migrations, agents, workflow and tests.
- `web/`: React management and supervisor application.
- `mobile/`: Flutter field application and Android project.
- `docs/`: architecture, ADRs, setup, evidence and assessment checklists.
- `.github/workflows/`: backend and web GitHub Actions checks.
- `scripts/`: local PostgreSQL and demo launchers.

Configuration secrets belong in environment variables or local ignored files. Do not commit database passwords, JWT signing keys or third-party API keys.
