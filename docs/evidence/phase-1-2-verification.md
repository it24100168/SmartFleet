# Phase 1–2 verification — 27 September 2026

## Environment and scope

Local ASP.NET API (`localhost:5078`), PostgreSQL 16 in the isolated `smartfleet-assessment` Docker Compose project, React/Vite (`127.0.0.1:5173`) and the Flutter application served as a web client (`localhost:5180`). Weather and robot motion remain explicitly simulated. API health returned `ready`, `demo: true`, `databaseProvider: PostgreSQL`.

This record reports checks actually performed. It does not certify cloud deployment, Android APK behavior, physical robot operation or completion of the full specification.

## Automated results

| Check | Result |
| --- | --- |
| Fresh backend build and full suite with `SMARTFLEET_TEST_POSTGRES` configured | 36 passed, zero skipped/failed |
| PostgreSQL migration chain | All six migrations applied successfully to fresh databases |
| PostgreSQL approval persistence | New database context loaded the waiting workflow; operator approval denied; supervisor approval resumed it; repeated approval/completion did not duplicate delivery |
| Concurrent PostgreSQL rover reservation | Exactly one winner for two simultaneous requests |
| PostgreSQL constraints and transactions | Duplicate email rejected; rolled-back dispatch not persisted |
| Critical priority with unsafe weather | Safely failed, without approval or retained rover reservation |
| Flutter analyzer | No issues found |
| Default Flutter test suite | Three passed; opt-in live test intentionally skipped |
| Opt-in Flutter service against PostgreSQL API | Passed: submit → AwaitingApproval → operator denied (403) → separate supervisor session approves → Flutter service reads Completed |
| HTTP workflow regression against PostgreSQL API | Passed: invalid zones, future scheduling, equal-target Critical reservation first, approval pause/resume, motor-fault stop-position persistence and maintenance recovery before repair |
| React TypeScript/Vite production build | Passed |
| IST utility and fleet marker layout checks | Passed |

The initial PostgreSQL image download failed before completion; retry succeeded. Flutter's initial CDN rendering-asset load also failed; serving resources locally with `--no-web-resources-cdn` resolved startup. These are environment findings, not passing tests.

## Two-client browser interaction

The Flutter application was exercised through its accessible UI in a browser; React used a separate browser tab and supervisor session. No native Android device was connected.

1. Flutter Operator selected Critical priority, the known A1/B3 map zones and Next available slot. The existing default Fragile cargo was retained.
2. Flutter submitted request `336974dc-5536-413f-9736-e627171d63c3` and displayed AwaitingApproval with the reserved/no-movement explanation.
3. React Approval Center displayed the matching request, Critical authorization reason, low numerical risk (5), battery OK and reserved rover RO-04.
4. Supervisor entered a review note and approved using React's Approve Mission button. React displayed the successful approval and recorded reviewer/note/time.
5. Flutter's initiating history refreshed through InTransit to Completed for the same request ID.
6. The isolated PostgreSQL container was restarted. The API reconnected, and a newly authenticated Operator read the same persisted request as Completed.

## Remaining acceptance work

- Repeat on a native Flutter device/emulator, exercise a real device feature and retain a presentation recording. The Flutter web run is not native-device evidence.
- Phase 3 must fix the existing plan-step representation: the approval summary still marks every planned step Completed, including Await approval, while the mission is waiting. This milestone enforces the real execution gate correctly but does not claim the displayed per-step audit is accurate yet.
- Hosted CI results, cloud URLs, performance/evaluation reports, APK and remaining submission artifacts are separate acceptance gates.
