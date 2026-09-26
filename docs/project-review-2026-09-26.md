SmartFleet project review — 26 September 2026

The four components contain substantial implementation, but the repository does not yet implement the integrated workflow described in the project brief. Existing unit tests pass independently. That does not establish end-to-end correctness, database concurrency safety, or mobile release readiness. Integration and correctness fixes should take precedence over additional visual polish.

**Scope and evidence**

Reviewed the supplied project description, fetched all remote branches, inspected backend, agent, database, web, mobile, contract and CI code, and ran isolated branch tests/builds. No feature branches were merged, pushed, or modified. Temporary branch exports were used for testing. This report is the only project addition. No live database migrations, live weather verification, device tests, deployed-site tests or load tests were performed. Flutter and Dart were not available on PATH. The assignment's original rubric was not supplied, so compliance with its exact AI requirements remains unverified.

| Branch | Reviewed commit | Existing backend tests | Web production build |
|---|---|---|---|
| main | 6e32f92106065cae3816a4d53c11ae6c7a0622d3 | 2 passed | Failed: ImportMeta.env typing |
| rover-telemetry | c9502b8c1d384fdd5581749e9c583301efa4605f | 6 passed | Failed: env typing and two unused declarations |
| feature/dispatch-mission-planner | 23e111abefc895b2be7ee60b2bbcef2c83ac71f2 | 5 passed | Passed |
| feature/maintenance-mechanic | 98fab798a8a1349056ebac3e1e1b4f236e435457 | 5 passed | Passed |
| feature/safety-guard-approval | 68f3bf0a4b3cb1a6aba8232b03d2306745731bfd | 6 passed | Passed |

Every branch includes the same two basic role/model tests: counts above must not be added together as unique integration coverage. Rover tests mock the repository and weather service; safety tests use EF's in-memory provider. Web builds reused the workspace's installed node_modules with each branch's unchanged source/configuration; clean npm-ci reproducibility was not tested. An initial sandbox-restricted NuGet restore and Vite run failed; permitted reruns succeeded. A package-audit fetch warning remained in safety's restore metadata. All backend builds reported EF Relational 8.0.4/8.0.8 assembly-version conflicts; align compatible EF/Npgsql dependencies and restore again.

Five additional review-only probes in the temporary safety test project confirmed current boundary behavior. These probes assert observed behavior, not the desired policy:

| Input modification from a healthy scenario | Actual outcome | Score |
|---|---|---|
| Empty mission plan | AutoApproved | 25 |
| WeatherRisk = unavailable | AutoApproved | 5 |
| Locked = false | PendingApproval | 30 |
| BatteryOk = false | PendingApproval | 45 |
| Evaluate identical pending input twice | Two pending approval rows | — |

**Findings, ordered by urgency**

1. **Critical — anyone can register as Supervisor.** The anonymous register endpoint accepts Role, and AuthService assigns request.Role directly before issuing a token. Supervisor-only approval attributes therefore do not establish a trustworthy privilege boundary. Restrict privileged account provisioning; if public registration is retained, force Operator server-side. Reject undefined role values. Evidence: [AuthController](https://github.com/it24100168/SmartFleet/blob/c9502b8c1d384fdd5581749e9c583301efa4605f/backend/Controllers/AuthController.cs), [AuthService](https://github.com/it24100168/SmartFleet/blob/c9502b8c1d384fdd5581749e9c583301efa4605f/backend/Services/AuthService.cs).

2. **Critical — normal mutation endpoints bypass workflow decisions.** DispatchRequestsController's PATCH status endpoint has no role restriction beyond authentication; DispatchService allows an Operator to set their own request directly to Approved, InTransit or Completed. Rover simulation PATCH explicitly allows Operators to mark any rover Idle and clear its mission. The public lock endpoint accepts a mission string without verifying a dispatch or approval. Restrict simulation controls to a development/demo mode and authorized supervisors, enforce legal state transitions, and make mission assignment/finalization an internal workflow operation. Evidence: dispatch branch `backend/Controllers/DispatchRequestsController.cs:80`, `backend/Services/DispatchService.cs:115`; rover branch `backend/Controllers/RoversController.cs`.

3. **Blocker — the four-agent orchestration is missing.** GeneratePlanAsync persists a WorkflowRun and stops at Planned. Rover execution is independent. Safety's evaluate endpoint always constructs mocked input. DispatchSyncService only logs a requested change and returns true; it never updates DispatchRequest, resumes WorkflowRun, starts a mission or releases a rover. Add an application-level WorkflowOrchestrator that passes real outputs between agents and persists every transition. Implement approve/reject/revise against that workflow. Evidence: [DispatchService](https://github.com/it24100168/SmartFleet/blob/23e111abefc895b2be7ee60b2bbcef2c83ac71f2/backend/Services/DispatchService.cs), [DispatchSyncService](https://github.com/it24100168/SmartFleet/blob/68f3bf0a4b3cb1a6aba8232b03d2306745731bfd/backend/Services/DispatchSyncService.cs), safety `ApprovalRequestsController.cs:210` onward.

4. **High — breakdowns do not block or restore rovers.** CreateReport, Diagnose and UpdateStatus update only BreakdownReport. No rover availability change occurs; no rover repository is injected. Rover selection only checks Rover.Status/battery, so a report alone cannot prevent selection. Require a real RoverId, validate the relationship, atomically block the rover on report creation, interrupt/reassess an active mission, and release it after repair only when no other blocking reports remain. Maintenance's React create form does not send any RoverId. Evidence: [BreakdownReportsController](https://github.com/it24100168/SmartFleet/blob/98fab798a8a1349056ebac3e1e1b4f236e435457/backend/Controllers/BreakdownReportsController.cs), maintenance `web/src/pages/MaintenanceQueue.tsx:70`.

5. **High — safety scoring permits invalid prerequisites.** The tested cases above show empty plans and unknown weather passing automatically. Failed battery/lock conditions can be sent for human approval, although a human click cannot supply battery capacity or ownership of a reservation. Validate IDs, plan structure, weather vocabulary and required results before scoring; make impossible/unsafe prerequisites hard blocks. Revalidate rover ownership, battery, weather freshness and maintenance status when resuming after a human pause. Evidence: [SafetyGuardAgent](https://github.com/it24100168/SmartFleet/blob/68f3bf0a4b3cb1a6aba8232b03d2306745731bfd/backend/Agents/SafetyGuardAgent/SafetyGuardAgent.cs:40).

6. **High — weather service failures become safe-looking weather.** Missing keys, errors and timeouts produce a fixed low-risk simulated result. IsSimulatedFallback is present in the weather DTO but discarded from DispatchTelemetryAgentOutput, so downstream Safety Guard cannot tell real weather from unavailable data. Introduce an explicit Demo/Live provider setting and propagate source, observedAt and availability. In Live mode unavailable weather should pause/block an exposed route, not silently become clear. Current calls use one configured city, not source/destination coordinates. An indoor-only route can explicitly mark weather not applicable. Evidence: rover `backend/Services/WeatherService.cs` and `backend/Agents/DispatchTelemetryAgent/DispatchTelemetryAgent.cs`.

7. **High — rover locking needs a real PostgreSQL concurrency test and correction.** Candidates are loaded as tracked entities before the weather call. The lock method re-queries the same entity in the same DbContext inside its transaction. EF can return the already-tracked instance without refreshing values. If another request assigns the rover between candidate selection and transaction start, the stale Idle value can pass the check and overwrite its mission. This is a code-derived race scenario, not a reproduced PostgreSQL failure in this review. Use an atomic conditional update with status/mission predicates and inspect the affected row count, or correctly reload and lock within the transaction. Recheck battery and maintenance there too. [Microsoft's EF tracking documentation](https://learn.microsoft.com/en-us/ef/core/querying/tracking) confirms that tracked values are not overwritten by a subsequent tracking query. Evidence: rover `backend/Data/Repositories/RoverRepository.cs:85` onward.

8. **High — reservation and mission lifecycle are incomplete.** Locking immediately sets Dispatched before Safety Guard approves. No reservation expiry, rejection cleanup, completion transition or simulated delivery runner connects the modules. Repeating the same dispatch execution can reserve more than one rover because mission-level idempotency is absent. Add Reserved (or equivalent separate reservation), one active reservation per run, expiry, release on rejection/cancellation/failure, and completion that updates rover location and dispatch status. Do not keep a database transaction open while waiting for a supervisor.

9. **High — approval consistency and audit gaps.** Repeated evaluation inserts duplicate pending requests. Approve/reject read Pending and save without a concurrency token/conditional update, so competing supervisor decisions are not protected. ResolveValidSupervisorIdAsync can attribute an invalid reviewer to an arbitrary existing supervisor. Enforce one approval per run/revision, make decisions atomic and idempotent, and reject invalid reviewer identities. Restrict approval details/log access by role/ownership. Evidence: safety `backend/Agents/SafetyGuardAgent/SafetyGuardAgent.cs:154`, `backend/Services/ApprovalService.cs:103` and `:220`.

10. **High — mobile rover identification is incompatible with the API.** The Flutter breakdown field suggests RO-04 and sends that string; CreateBreakdownReportDto.RoverId is Guid?. Entering the suggested identifier cannot bind as a UUID and should yield HTTP 400. Use a rover dropdown displaying Identifier but posting Id. Keep roverId as a UUID throughout the database/API and roverIdentifier as a separate display field. Safety/telemetry currently use the identifier in fields named roverId/selectedRoverId, while dispatch and maintenance use Guid placeholders. Evidence: maintenance `mobile/lib/screens/breakdown/breakdown_report_screen.dart:278`, `mobile/lib/services/breakdown_service.dart`, `backend/DTOs/Breakdown/CreateBreakdownReportDto.cs:7`.

11. **Build blocker — main and rover React production builds fail.** main lacks Vite environment typing at `web/src/api/axiosClient.ts:3`. Rover also has unused ArrowRight and isLoadingWeather in `FleetTelemetry.tsx:20` and `:41`. Preserve dispatch/safety's vite-env.d.ts during integration and remove/use the unused declarations. A dev server displaying a page is insufficient evidence that a production build succeeds.

12. **CI blocker — wrong test path.** `.github/workflows/backend-ci.yml` calls `dotnet test backend/backend.Tests/backend.Tests.csproj`; the project is actually `backend.Tests/backend.Tests.csproj`. Correct the path or test `backend/backend.sln` using the Release build configuration. Add web production build and Flutter analyze/test checks. Current CI only triggers for pushes to main and PRs targeting main; feature pushes alone do not run it. No live GitHub Actions run status was verified.

13. **Mobile release gap — native configuration is unfinished.** Platform folders exist only on the safety branch. Its main Android manifest lacks INTERNET and location declarations; debug/profile manifests alone are not sufficient for release networking. iOS usage-description entries for location/camera/photo access are absent. The widget test references MyApp, but main.dart declares SmartFleetApp; it is still the generated counter test. Merge the platform scaffolding with both actual mobile features, configure permissions and API URLs, replace the test, and build/install a release APK. Maintenance's non-web URL always uses Android emulator address 10.0.2.2, including other native platforms. Flutter wasn't executed here. [Flutter release guidance](https://docs.flutter.dev/deployment/android) and [geolocator's setup instructions](https://pub.dev/packages/geolocator/versions/11.1.0) document the required networking/location configuration.

14. **Web defect — maintenance photographs resolve against the wrong origin.** Backend returns `/uploads/breakdowns/...`; React uses that directly as image src. With web on 5173 and API on 5000, the browser requests the image from the web server. The Vite config has no upload proxy. Resolve photo paths against the API origin or configure a shared-origin proxy. Mobile already prefixes the API origin. Evidence: maintenance `BreakdownReportsController.cs:79`, `web/src/pages/MaintenanceQueue.tsx:383`, `web/vite.config.ts`.

15. **Maintenance access/diagnosis gaps.** Diagnose has only class-level authentication and no ownership check, unlike GetById. An Operator knowing another report's ID can trigger a mutation. Restrict by role/ownership. FailureCatalogRepository accepts errorCode but never uses it. Keyword matching prioritizes the first matching description fragment and has no explicit ambiguity handling. Add an error-code mapping, deterministic precedence and manual review for conflicting matches. Upload validation checks extensions only; add an explicit small size limit, actual image validation and storage lifecycle handling. Evidence: maintenance `BreakdownReportsController.cs:206`, `FailureCatalogRepository.cs:22`.

16. **Contract drift and migration integration need deliberate review.** The committed contract omits assignedAgent even though Mission Planner emits it. `plan` must explicitly map to `planSteps`; telemetry output must be assembled into SafetyGuardInput rather than passed unchanged. RevisionRequested is used by ApprovalService but absent from DispatchRequestStatus. Battery output has a boolean only, not the numeric margin described in the brief. Add typed shared DTOs, a schema version, validation and serialization handoff tests. Pairwise read-only merge checks confirmed conflicts in Program.cs, SmartFleetDbContext.cs, the migration snapshot (maintenance/safety), and the safety/rover test csproj. These are not a completed multi-branch merge rehearsal. Preserve all entities, DI registrations and test dependencies. Do not select one branch's entire snapshot blindly. Existing migration names are distinct even where timestamps coincide; validate final schema on a fresh disposable PostgreSQL database and compare snapshot/model before generating further migrations.

17. **Observability mixes fixtures and real events.** Reading the approvals list seeds fictional approvals and planner/telemetry logs when empty. Only Safety Guard and supervisor decisions write actual shared execution logs in the inspected implementations; planner and mechanic do not. Auto-approved/rejected evaluations do not create approval rows, while the log API is addressed through an approval ID, making those runs difficult to retrieve. Move demo seeding to an explicit action in a dedicated demo database. Query logs by WorkflowRunId/DispatchRequestId for every outcome and mark simulated inputs. Dashboard counts remain hardcoded; fleet and mobile status views rely on refresh rather than a continuous feed. Evidence: safety `ApprovalService.cs:35`, `:236`, `:284`; shared `web/src/pages/Dashboard.tsx`.

18. **Deployment configuration is development-oriented.** CORS is hardcoded to localhost/127.0.0.1 even though settings contain origin values. A separately hosted React app will be blocked unless allowed origins are configured. Program.cs contains a known JWT fallback secret; production must require a private configured secret and fail startup if missing. Startup migration failures are caught and logged while the API continues starting; add readiness/failure handling. Use HTTPS and configurable mobile API URLs; persist uploaded photos beyond an ephemeral deployment filesystem. No claim is made here about the state of any actual deployment.

19. **Feature completeness differs from the description.** Battery uses a constant 40% threshold, independent of route distance/cargo. Mission Planner produces essentially a fixed checklist and does not delegate a mechanic step; route scheduling is a label, not an implemented route solver. Maintenance search filters only the current web page, with no server search/sort contract. Dispatch priority sorting is alphabetical, not business priority. These should be implemented or described accurately in the report/demo.

**Integration approach and acceptance checks**

Keep the existing ASP.NET monolith and separate injected agent classes. Separate agents do not require four servers. Create an integration branch from main; merge Rover → Dispatch → Maintenance → Safety, resolving shared files intentionally and testing after each. Before wiring execution, agree the IDs, statuses, plan schema, weather availability and reservation semantics. Preserve applied migration history; validate merged migrations against an empty isolated database rather than resetting teammates' databases.

The orchestrator should own durable run state, input validation, invocation order, retry/idempotency, output aggregation, failure cleanup and resume. Agents should retain their individual responsibilities. Store a WorkflowRunId, attempt/revision, step/agent, start/end time, validated inputs/outputs, source mode and error/result for each execution. Keep weather calls outside long database transactions; commit state transitions in short atomic operations. A human pause must survive an API restart and resume exactly once.

Minimum acceptance cases:

| Scenario | Required observable result |
|---|---|
| Healthy rover, low weather | Plan → validated reservation → auto-approval → simulated delivery → completed; rover returns available at destination |
| Healthy rover, medium weather | Durable pause; no delivery until Supervisor approves; mobile sees updated status |
| Supervisor rejects/revises | Release or explicitly retain/expire reservation per policy; reject ends run; revision creates a new version and reevaluates |
| Low battery, unknown weather or missing plan | Cannot be made executable merely by an approval click |
| Breakdown before selection | Affected rover excluded, even when another report is already repaired |
| Breakdown during reservation/delivery | Active run stops/reassesses safely and logs the event |
| Two requests for one idle rover | Exactly one owns it; the other gets a controlled conflict/fallback |
| Duplicate start / approve / evaluation | One mission, one effective approval decision, no duplicate execution |
| Cross-role/cross-user mutation | Denied; cannot self-register as Supervisor or edit others' reports |
| Camera/GPS and uploaded image | Actual device capture/location, successful upload and visible evidence in React |
| Restart during approval pause | Same pending run remains resumable, without duplicate reservation |

Run these through real HTTP against an isolated PostgreSQL database; EF InMemory cannot establish PostgreSQL transaction correctness. Add a small concurrent dispatch test only after functional paths pass. Preserve the request/run IDs and logs as viva evidence.

**Recommended presentation: an Agent Workflow Lab inside the existing React dashboard**

Use a single additional dashboard page with two modes: Individual Agent and Full Workflow. Reuse the existing agent forms and Approval Center, and bind the display to real API results. A simple two-dimensional warehouse map is enough; simulated rovers are part of your stated design. A new robotics/3D platform would add scope without demonstrating the missing integration.

Individual Agent mode: select one agent, load a clearly labelled fixture, inspect editable validated input, invoke it, and show output plus persisted effects. Use a dedicated demo database because telemetry invocation reserves a rover. Each teammate explains their input, decision rules/tool calls, output contract and failure case. Show Maintenance using a real catalog record and an unknown symptom; photographs are evidence, not computer vision.

Full Workflow mode: select a seeded scenario, submit a real request, and show four agent cards plus an executor and supervisor gate. Cards display Idle/Running/Succeeded/Skipped/Waiting/Failed, duration, input/output JSON and a short rule-based reason. A shared RunId links cards to logs. A mechanic card may say Skipped — no fault found; do not claim all four must run for every successful delivery. If telemetry is blocked solely by weather/battery, do not invent a fault report to call the mechanic.

Suggested screen layout:

```text
Scenario: Healthy | Supervisor review | Breakdown | No capacity   [Start]
Data mode: DEMO FIXTURES / LIVE PROVIDERS             Run: <UUID>

[Mission Planner] → [Dispatch & Telemetry] → [Safety Guard] → [Executor]
                          ↕                       ↕
                 [Maintenance Mechanic]       [Supervisor]

[Warehouse map / rover state]    [Selected agent input → output]
[Timeline: actual persisted events, validation, decisions and timestamps]
```

The arrows are a proposed design, not a claim that today's branches execute this flow. Use polling every one or two seconds for a deadline-sized implementation; add a pushed event channel later if needed. Animate a rover only from actual mission state. Display a visible fixture/live badge for weather, catalog and telemetry sources; never fabricate execution logs to make an animation look integrated.

Suggested eight-minute demonstration:

| Time | Demonstration | Evidence |
|---|---|---|
| 0–1 min | Three roles, simulated hardware and system architecture | Role-specific screens and live API |
| 1–3 min | Each teammate invokes their agent independently | Input, tool/rule, output, negative case |
| 3–5 min | Operator submits delivery; medium weather pauses it; Supervisor approves | Same RunId across Flutter, React, DB logs; rover moves only after approval |
| 5–6 min | Photograph breakdown; mechanic diagnoses; rover excluded; technician repairs | Photo, catalog match, fleet status before/after |
| 6–7 min | Safe failure plus two competing requests for one rover | No unsafe dispatch and a single winning reservation |
| 7–8 min | Audit timeline and automated test evidence | Real logs, test results and honest limitations |

Fallback before integration is complete: present each feature branch independently using its real endpoints and label the architecture walkthrough as a proposed workflow. Existing planner/telemetry/safety fixture controls can support that presentation. Do not describe the seeded Approval Center timeline as proof of cross-agent execution.

**AI claim and four-day priority**

The inspected agents are deterministic C# services: fixed checklist generation, repository/weather checks, catalog matching and safety scoring. No LLM invocation or model-driven replanning was found in these implementations. Describe them accurately as rule-based agents. Whether that meets the assignment's definition of Agentic AI depends on the original rubric. If model reasoning is required, a constrained structured-output Mission Planner is the smallest potential addition; validate its outputs against allowed steps and preserve deterministic safety and authorization. Do not replace safety checks with model judgment.

| Date | Priority |
|---|---|
| Sept 26 | Merge on integration branch; repair web/CI; agree schema/states; fix privilege escalation and status bypasses |
| Sept 27 | Implement orchestrator, maintenance exclusion, reservation lifecycle and real approval resume; test PostgreSQL schema/concurrency |
| Sept 28 | Pass HTTP golden paths and role tests; configure native permissions; install release APK; resolve photo/CORS issues |
| Sept 29 | Add the workflow viewer using real logs; deploy and smoke-test; prepare report/ADRs, AI usage evidence and video |
| Sept 30 | Buffer for failures, final rehearsal and submission before the stated 11:50 PM deadline |

The most persuasive deliverable is one complete, auditable mission with a genuine human pause and a demonstrable safe failure, backed by the four individually testable agent implementations.
