# Group 2 Azure + Neon deployment runbook

**Status:** prepared on 5 October 2026. Record the real URLs and successful checks below only after they exist. The team reports a Course Web deadline of **6 October 2026, 11:00 AM Sri Lanka time**.

The first Azure creation attempt returned `RequestDisallowedByAzure` because the chosen region did not meet the student's subscription policy. Before retrying, open Azure **Policy → Assignments → Allowed resource deployment regions → Parameters → Allowed locations**, and choose a listed region for the resource group and Web App. If no allowed location supports App Service, use the portable `Dockerfile.api` on another .NET-capable host and document its uptime/storage limits. Do not claim a hosted URL until it answers the checks below.

This uses the team's Azure for Students account, Azure Static Web Apps Free, one Azure App Service Linux .NET 8 instance, and its new Neon Free PostgreSQL database. The ASP.NET Core API is the only public path to data and agents. Open-Meteo provides real, keyless weather data for this non-commercial educational demonstration; invalid or unavailable weather blocks dispatch. [Open-Meteo's terms](https://open-meteo.com/en/terms) require non-commercial use and CC BY 4.0 attribution.

## 1. Create the resources

In the Azure portal, check that the subscription is **Azure for Students** and note its actual remaining credit. Create a resource group, for example `smartfleet-g02`. Create a **Web App** with **Code**, **.NET 8 (LTS)**, **Linux**, one **Basic B1** instance, and an available region. Read the portal's estimated monthly charge before accepting the paid plan; monitor the student-credit balance. Under Configuration > General settings, turn **Always On** on, because the fleet worker advances missions every second. Enforce HTTPS. In Deployment Center, connect GitHub `it24100168/SmartFleet`, branch `main`, with GitHub Actions. The generated workflow must restore and publish **`backend/backend.csproj`**, not the repository root. Leave the web app stopped until the private settings below are ready.

Create a **Static Web App** on the **Free** plan, connected to the same repository and branch. Its build values are: app location `web`, API location empty, output location `dist`. It must build the Vite React app, not the Flutter folder. This repository includes `web/public/staticwebapp.config.json` for direct links such as `/approvals`; check that the file appears at the root of the deployed `dist` output. Record both Azure `https://...` URLs. Azure's generated GitHub workflows and deployment secrets should remain in GitHub Actions; never paste those tokens into this repository or a report.

In Neon, use the **new** project and its direct PostgreSQL host (without `-pooler` for the initial migration). Use its dashboard's connection details to obtain database, role and password. Keep them private. The database is not a public application endpoint. Do not reuse an old database credential previously put in Git or chat.

## 2. Configure the API privately

In the Web App > Settings > Environment variables, add **App settings** with these names. Use `__` exactly as shown; ASP.NET Core converts this to nested configuration. App Service restarts when settings change.

| Name | Value to enter privately |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Database__Provider` | `PostgreSQL` |
| `Database__ApplyMigrations` | `true` for initial startup; set `false` after verifying migrations |
| `ConnectionStrings__DefaultConnection` | Npgsql format: `Host=<NEON_DIRECT_HOST>;Port=5432;Database=<DB>;Username=<ROLE>;Password=<PRIVATE_PASSWORD>;SSL Mode=VerifyFull;Maximum Pool Size=10;Timeout=15` |
| `JwtSettings__Secret` | A fresh, private random string of at least 32 bytes; keep it stable after deployment |
| `Simulation__Enabled` | `false` |
| `Simulation__DisableWorker` | `false` |
| `Swagger__Enabled` | `true` for the assessment API documentation |
| `Cors__AllowedOrigins__0` | Exact Static Web App origin, e.g. `https://<site>.azurestaticapps.net` with **no** trailing slash |
| `WeatherSettings__Provider` | `OpenMeteo` |
| `WeatherSettings__Latitude` / `WeatherSettings__Longitude` | `6.9271` / `79.8612` for the Colombo demonstration area |
| `Provisioning__Supervisor__Name`, `__Email`, `__Password` | Team-chosen supervisor account; password at least 12 characters |
| `Provisioning__Technician__Name`, `__Email`, `__Password` | Team-chosen technician account; password at least 12 characters |

For the last two rows, each `__` suffix is appended to the full prefix, e.g. `Provisioning__Supervisor__Password`. Public registration creates Operator accounts only. After the first successful startup and a tested Supervisor/Technician login, remove the two plaintext **Password** settings; the hashes persist in PostgreSQL. Do **not** remove the JWT secret, since doing so invalidates tokens and prevents startup. Npgsql's `SSL Mode=VerifyFull` validates the server certificate and host. If certificate validation fails, diagnose the actual hostname and trust chain; do not bypass verification. Keep connection string and passwords out of GitHub workflows, screenshots and chat.

The initial startup applies EF Core migrations and seeds the six fleet rovers through migrations. Startup also clears a known orphan sample mission marker on RO-03. If startup fails, check App Service Log stream for the error category, then correct settings. Avoid sharing full exception logs if they might include a connection string. Do not run two API instances concurrently during automatic migration; this runbook uses one instance.

## 3. Connect the React build and Android app

The Static Web Apps GitHub workflow must set the public build-time value `VITE_API_BASE_URL=https://<api-app>.azurewebsites.net/api` on its build/deploy step. This URL is public configuration, not a secret. A portal runtime setting alone may not update a Vite bundle: trigger a fresh build and inspect browser Network requests to ensure they go to the App Service URL. Set the API's `Cors__AllowedOrigins__0` to the exact Static Web App origin, then test login from the hosted React page.

Build Flutter from the final source with the **same** public API URL:

```powershell
cd mobile
C:\flutter\bin\flutter.bat build apk --release --dart-define=API_BASE_URL=https://<api-app>.azurewebsites.net/api
```

Install the APK on a clean Android emulator/device and check a real login. The public HTTPS API avoids emulator-only `10.0.2.2`. Do not use the local demo password for the hosted system. If release signing/build is blocked, document the error and submit a working debug APK only as a clearly labelled fallback, with install instructions.

## 4. Verify and capture evidence

1. In a private/incognito browser, check `https://<api-app>.azurewebsites.net/api/health` returns `status: ready`, `databaseProvider: PostgreSQL`, `demo: false`. Check `/swagger` opens.
   The read-only `scripts/verify-hosted.mjs` checks health, Swagger JSON, React entry/direct-route fallback and CORS once `SMARTFLEET_API_URL` and `SMARTFLEET_WEB_URL` are set to the two public HTTPS origins.
2. Log into React as Supervisor, then create an Operator through the native app's registration flow. Confirm each role's protected screens and a denied action for the wrong role.
3. From native Flutter, submit a **Critical** dispatch with a valid source/destination and note its request ID. Show the persisted plan, four agents/tool results, validation, and pending approval in React. Confirm the rover has **not** moved before approval. Approve as Supervisor in React, then show final status and rover movement on native Flutter; retain the same request ID in all screenshots and audit logs.
4. Test a standard successful dispatch, a bad zone, a malformed/missing weather response safe failure, a breakdown/repair, and a simulated charging cycle. For real weather, record the provider, WMO code, decision and timestamp without pretending the weather is warehouse sensor data.
5. Confirm breakdown photos still load after an App Service restart/redeploy. App Service local files may have persistence limits; if this fails, document the exact limitation and plan durable object storage rather than claiming permanent upload storage.
6. Capture final GitHub Actions green runs for backend and web, PostgreSQL migrations/constraints, performance output, native APK install and the cross-client workflow. Put actual URLs in the README and consolidated report. Keep evaluator access available through **21 October 2026**, per the specification.

**Fill only after verification:** API URL: ___; Swagger URL: ___; React URL: ___; Neon project/region (no secret): ___; Azure region and B1 estimated/actual cost: ___; final Git commit: ___; backend CI run: ___; web CI/deployment run: ___; APK path/hash: ___; incognito checked at: ___ .
