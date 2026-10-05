# Local deployment and submission preparation

The team selected local preparation before cloud deployment. No hosting account or cloud resource has been created by this work.

## Current local evidence

- PostgreSQL assessment launcher and persistent volume: `scripts/start-assessment.ps1`.
- React production build: `npm run build --prefix web`; output `web/dist`.
- API publish command: `dotnet publish backend/backend.csproj -c Release -o .demo/publish-api`.
- Native Android evidence (4 October 2026): Flutter 3.47.5, Android Studio, SDK platforms 34–36, and NDK 28.2.13676358 are installed. The Pixel 7 Android 16 emulator (`emulator-5554`) ran the debug APK at `mobile/build/app/outputs/flutter-apk/app-debug.apk` (156,457,438 bytes). The app signed in to the local PostgreSQL API, displayed six rovers, submitted request `37a4f0c4-47dd-4b5c-b082-0bac22c4fd21` from A1 to B3 with Fragile cargo and High priority, and showed it Completed. A separate authenticated fleet snapshot confirmed `RO-03`, 100% progress, and final zone `WarehouseA-DockB3`. This verifies one native operator workflow; the cross-client Critical approval presentation remains to be captured. The Visual Studio desktop workload is not required for Android.
- Upgraded-SDK checks: `flutter test --no-pub` passed six tests, with the opt-in live PostgreSQL test skipped. `flutter analyze --no-pub` reported 23 informational deprecation notices (`withOpacity` and dropdown `value`); it exited nonzero, so this is not a clean analyzer run. No Dart errors were reported. The first Android build exposed an image picker Android compile SDK mismatch; upgrading `image_picker_android` from `0.8.12+21` to `0.8.12+25` in the lockfile resolved it. Google Storage downloads on this network present an untrusted Fortinet certificate. For this build, `FLUTTER_STORAGE_BASE_URL=https://storage.flutter-io.cn` selected a [Flutter documented community mirror](https://docs.flutter.dev/community/china); the exact artifact URL returned HTTP 200 with a valid certificate. The override was scoped to the build process, and TLS verification remained enabled. The login logo and profile header now render on the API 36 emulator through a debug-only renderer setting; release visual QA remains open.
- Browser rehearsal: Flutter with `--no-web-resources-cdn` on port 5180, React on 5173, API on 5078. This is not native-device evidence.

## Ten-minute presentation rehearsal

| Time | Presenter activity | Evidence to show |
| --- | --- | --- |
| 0–1 min | Explain warehouse problem, three roles and simulated hardware | Architecture and fleet overview |
| 1–3 min | Operator submits map-based Critical request in Flutter | Request ID, IST time, waiting status |
| 3–5 min | Explain planner, rover selection, maintenance exclusion and safety authorization | Real logs and checklist; distinguish skipped maintenance when no fault exists |
| 5–6 min | Supervisor approves in React | Same request ID, reviewer note, rover starts only after approval |
| 6–7 min | Show Flutter completion and persisted destination/battery | Completed state in both clients |
| 7–8 min | Report a motor fault on a different active mission | Stopped position, recovery to maintenance, repair |
| 8–9 min | Show tests, PostgreSQL persistence and failure cases | Actual test counts and CI links; no fabricated performance results |
| 9–10 min | Explain prototype limits and each student's contribution | Ownership evidence, remaining gaps, AI-use disclosure |

## Submission assembly checklist

Prepare one consolidated PDF with group architecture, schema, workflow, security, testing, evaluation, deployment/access evidence and individual component sections. Include the required ADRs, sources, repository/CI links, API documentation and actual screenshots. Keep an explicit table of tested versus untested capabilities.

Each student supplies their own identity, ownership evidence, signed declarations and personally authored reflection. Do not invent development dates, contributions, user evaluation, screenshots or deployment URLs. The assistant can help organize technical evidence but must not write the personally authored reflection as if it were the student's own work.

Before submission: confirm the specification's deadline, filename rules, video duration and evaluator-access period against the supplied PDF. Test evaluator credentials from a clean session. Cloud availability and Android device features remain separate gates.
