# Local deployment and submission preparation

The team selected local preparation before cloud deployment. No hosting account or cloud resource has been created by this work.

## Current local evidence

- PostgreSQL assessment launcher and persistent volume: `scripts/start-assessment.ps1`.
- React production build: `npm run build --prefix web`; output `web/dist`.
- API publish command: `dotnet publish backend/backend.csproj -c Release -o .demo/publish-api`.
- Native Android prerequisite check: the installed Flutter SDK reports **no Android SDK or Android Studio**. No APK build or emulator run can be claimed. The local Flutter SDK is 3.24.3; the repository's Android Gradle/Kotlin versions must be reconciled with a supported toolchain before building. Do not replace those versions blindly or claim a generated APK.
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
