# SmartFleet specification compliance review — 27 September 2026

## Verdict and scope

**Not all assignment requirements are satisfied.** SmartFleet has a working integrated backend/web simulation and substantial business logic, but it is not yet evidenced as a submission-ready implementation of the mandatory PostgreSQL, cross-platform, testing, deployment and individual-contribution requirements.

Source: the user's local 17-page **2026-S1-SE3090 Assignment 1 — Integrated Full-Stack and Agentic AI Application Development — Specification With Marking Scheme** PDF. Page numbers below refer to that document. Reviewed repository: `a218b96`, plus the current working tree. Uncommitted Flutter lockfile/generated platform files were present before this review and were not intentionally edited. External deployment/report/GitHub artifacts not supplied or present locally are classified as **unverified**, not asserted never to exist. PDF text was extracted locally; the two architecture figures on page 7 were not visually rendered. The explicit cross-platform rule below those figures was reviewed.

This is a compliance review, not an official mark prediction. No application fixes or deployment changes were made in this review. The assignment permits development AI assistance but requires disclosure, verification and student understanding; its restrictions on the final evaluation are submission requirements, not instructions that prohibit this development-time review.

## Requirement matrix

| Specification requirement | Finding | Evidence / work still needed |
|---|---|---|
| ASP.NET Core public backend; EF Core PostgreSQL provider (pp. 2–3) | Implemented | `backend/Program.cs`, `backend/backend.csproj`; both clients use the API. |
| PostgreSQL database in the assessed system (pp. 2–3, 5, 7) | **Partial; mandatory verification gap** | Npgsql, models and migrations exist. Current explicit simulation mode selects SQLite. SQLite tests and the current demo do not prove PostgreSQL compliance. Run migrations, transactions and the complete assessed flow against PostgreSQL. |
| React functional components, hooks, routing and state management (pp. 3, 5) | Implemented in code; justification incomplete | React, hooks, Router and AuthContext present; add the required state-management ADR. |
| Genuine Flutter app and justified state management (pp. 3, 5) | Partial | Flutter/Dart, Provider/ChangeNotifier and GoRouter present. Device execution, APK, broader workflows and ADR still needed. |
| Three roles with different permissions (pp. 4–5) | Implemented core | Operator, Technician, Supervisor; JWT, server authorization and ownership checks. UI navigation still exposes irrelevant entries/actions to some roles. |
| Four primary business components (pp. 3–4) | Substantially present | Dispatch, rover/telemetry, maintenance and approvals. Their functionality, individual ownership and full-stack evidence need explicit mapping. |
| Every student contributes backend, DB, React, Flutter, tests, docs and distinct AI work (pp. 3–4) | **Not fully evidenced** | Four agent ownership labels/feature branches exist, but mobile mainly covers dispatch and breakdown. No telemetry/approval mobile routes were found. Shared infrastructure alone does not establish every student's required contribution. |
| Four meaningful endpoints and a business operation per owned component (p. 5) | Partial; rover component at risk | Dispatch has create/list/detail/plan; maintenance has create/list/detail/status/diagnose; approvals have list/stats/detail/decisions/logs. Rover controller has only two working read routes: its update/lock routes return 409, and standalone telemetry execute is retired. Weather is another useful endpoint; mock-input and always-rejected endpoints should not be counted as meaningful functionality. Explicitly map any shared workflow endpoints owned by that student or add suitable business operations. |
| CRUD, status workflows, search/filter/sort/page, analytics (pp. 4–5) | Partial | Status workflows, lists, history, filtering, rover sorting, paging and dashboard/approval statistics exist. No HTTP DELETE endpoints and no general edit/delete lifecycle were found; full CRUD is not demonstrated. Add legitimate entity/draft edit/archive/delete operations with suitable audit protections, not unsafe overrides of mission state. |
| Layered API architecture, async methods, validation, errors, DI, logging, CORS, Swagger (p. 4) | Largely implemented | Controllers, DTOs, services, repositories and middleware exist. Some agent orchestration/validation and security hardening remain below. |
| Secure configuration and no committed secrets (pp. 4, 6, 16–17) | Partial; remediation evidence required | Current tracked application configuration no longer contains the earlier Neon credential. A credential was previously committed; rotation/history remediation must be verified. Password hashing and JWT exist. Do not reproduce old credentials in reports. |
| Relational design, ERD, constraints/indexes, migrations, seed data, transactions, audit fields (p. 5) | Partial | EF entities, relationships, migrations, indexes, concurrency tokens, seed data and transactions exist. ERD/schema documentation is missing from reviewed artifacts. PostgreSQL integrity and migration execution still need tests; some references use string/nullable IDs rather than fully enforced relationships. |
| React role navigation, CRUD, accessibility and UI states (p. 5) | Partial | Dashboard, agent logs, forms, loading/error/empty states and approval controls work. Sidebar lists all entries regardless of role; only the approval route has an explicit role guard. Audit all controls, mobile-width layouts and keyboard access. CRUD gaps also affect UI compliance. |
| Flutter registration/login/logout/secure tokens/protected screens (p. 5) | Partial | Login/logout, secure storage and authenticated routing exist. Registration endpoint constant exists, but no registration screen or service method was found. Backend permissions still enforce role restrictions; mobile navigation is not fully role-aware. |
| Flutter forms/search/filter/transactions/status/history (p. 5) | Partial | Dispatch/breakdown forms, histories and status polling exist. User-facing search/filter controls were not found; dispatch history requests only the first 50 records. Pickup/dropoff remain free text on mobile and the target time is initialized rather than exposed through a picker. Backend now rejects invalid zones, so the mobile form needs parity. |
| Meaningful mobile device feature (p. 5) | Implemented in source, runtime unverified | Camera/gallery upload and GPS capture exist, with permission declarations. Demonstrate permission denial, upload and GPS on a device/emulator. |
| Distinct web/mobile purposes (pp. 4–5) | Present at design level | Mobile field dispatch/breakdown capture; web monitoring and supervisor review. Show them sharing the same live identity, data and rules. |
| Meaningful third-party service (p. 8) | Partial | Backend OpenWeather integration, six-second timeout and bounded attempts exist. Demonstrate a real response and explain why weather matters for warehouse/loading-dock operations. Demo fixtures alone are not evidence of an actual external integration. Improve rate-limit handling and response validation; an empty weather array currently defaults to clear weather, and unknown condition codes default to low risk. |
| Full cross-platform approval workflow (p. 7) | **Not demonstrated; mandatory blocker** | Must start in one client, traverse ASP.NET/PostgreSQL/agents, require approval in the other client and return status. Web-only SQLite demo does not satisfy this. Mobile uses normal dispatch creation; demo mode defaults that to low weather, so it normally auto-approves. Provide a repeatable, legitimate high-impact approval trigger accessible from Flutter and prove the complete flow on PostgreSQL. |
| Testing across required layers (p. 8) | Partial; substantial gaps | See test matrix below. Builds and a small unit suite are not the requested full testing evidence. |
| GitHub Actions backend restore/build/test on main pushes/PRs (p. 8) | Configuration present | `.github/workflows/backend-ci.yml` has the required stages/triggers; web CI also exists. A current successful hosted run/link was not verified in this review. Flutter CI is encouraged, not explicitly mandatory. |
| Git issues/PRs/reviews/board/regular contributions (p. 8) | Partial / unverified | Feature branches, commits and merge history exist. Hosted issue, PR-review and project-board evidence was not supplied/verified. Recent integration commits do not establish each student's historical ownership; retain truthful AI disclosure. |
| API/PostgreSQL/React deployed; runnable Flutter APK (p. 9) | **Unverified / artifacts missing locally** | Localhost is not cloud deployment. No verified live React/API/health/Swagger links or deployed PostgreSQL evidence. Neither standard debug nor release APK output was present. Swagger is Development-only in `Program.cs`; arrange an appropriately secured evaluator-accessible deployment URL. |
| Technical documentation and ADR (p. 9) | Partial / ADR missing | Setup and demo guides exist, but `docs/README.md` still lists diagrams/ADRs as planned. Root README and agents README contain outdated placeholder/overstated descriptions. Required ADRs: React state, Flutter state, agent framework/orchestration, workflow-state schema, cloud platform. |
| Consolidated report, reports/evaluation/performance, diagram and access evidence (p. 10) | Not found in reviewed artifacts | Produce one PDF containing group and all individual report sections, including required diagrams, ADRs, references, security/testing/deployment evidence and links. Separate review Markdown files are not that submission. |
| AI logs/declaration/personal reflection/signed declarations (pp. 10, 15–17) | Not found in reviewed artifacts | Document Antigravity and this assistant's actual development/review work, changes and verification. Do not fabricate dated logs or contributions. Each student writes their own approximately one-page reflection; the specification says AI-generated reflections receive no credit. |
| Video, naming, deadline/access and viva (pp. 10–11, 15–17) | Team action required / unverified | See submission checklist below. Software tests cannot certify personal understanding or evaluator access. |

## Agentic AI acceptance audit (pp. 5–6, 8)

The PDF **allows a custom orchestration approach**. It does not explicitly mandate an LLM, LangGraph, Python or a paid model. Absence of an LLM alone is therefore not enough to declare automatic failure. However, naming deterministic classes “agents” alone does not prove compliance either. Explain the architecture in an ADR and obtain lecturer clarification if uncertain about acceptance of template-based planning.

| Minimum assessed element | Current evidence | Assessment |
|---|---|---|
| Domain objective | Dispatch request and serialized objective | Present |
| Structured plan | Planner emits seven labelled steps | Present but fixed template; little objective-dependent planning beyond route text. |
| Delegation to distinct roles | Four different implementations/contracts, invoked by orchestrator | Substantial. Execution is hard-coded rather than driven by `AssignedAgent`; maintenance is skipped when no faults exist. Demonstrate genuine participation of each role, not only a card labelled Skipped. |
| Controlled, allow-listed tools | Injected repository/weather interfaces, no arbitrary model-generated tool execution | Partially evidenced. Document a per-agent permission matrix, validate tool boundaries, and justify broad DbContext access in Safety Guard. |
| Structured inputs/outputs and deterministic validation | DTOs, route/role/battery/reservation checks, safety prerequisites | Substantial. Add explicit output/plan validation and adversarial/malformed-input tests. |
| Durable shared state including completed steps | WorkflowRun, approvals and execution logs | Partial. `PlanJson` retains Pending step statuses. Safety input reconstructs all plan steps as Completed; this is not an accurate persisted per-step execution record. `CurrentStep` is coarse and does not consistently correspond to the seven-step checklist. |
| Authorized high-impact approval | Supervisor approve/reject/revise, reservation expiry and revalidation | Implemented in backend/web; cross-client PostgreSQL demonstration still needed. |
| Auditable success or safe failure | Logs, failed state, completion, emergency stop and rejection | Present for key workflow outcomes. |
| Tool calls/timings/errors/retries/approval observability | Agent-level inputs/outputs and timestamps | Partial. Individual tool-call duration, attempt counts and weather failures/retries are not all recorded durably per run. Console retry logs alone do not satisfy complete execution-history evidence. |
| Security, timeouts, retry limits, recovery | Role gates, typed operations, weather timeout/two attempts, concurrency/idempotency tests | Partial. Worker retries indefinitely without a bounded durable failure policy; distinguish normal waiting for capacity from repeated technical failures. Secret remediation and prompt/tool-input security evaluation remain unverified. |
| Golden-case evaluation including injection resistance | Agent and workflow tests | Partial. Create a traceable acceptance matrix covering every required element, malicious descriptions, unsupported tools/actions, failure recovery and human approval enforcement. With no LLM, explain why free text is treated only as data and test that it cannot change permissions or execute instructions. |

A useful assessed scenario is: Flutter creates a request while a faulty rover is excluded; the planner/telemetry/maintenance/safety roles record their actual work; a healthy rover is reserved for a defined high-impact action; React Supervisor approves; execution completes and Flutter displays the result. Use PostgreSQL throughout. Do not rely on hoping live weather happens to produce medium risk on demonstration day.

## Verification performed and remaining testing (p. 8)

Fresh checks during this review:

- `dotnet test backend/backend.sln --no-build --no-restore --verbosity quiet`: **32 passed** against existing build artifacts. This was not a fresh backend rebuild.
- `npm.cmd run build --prefix web`: **passed**.
- `node scripts/ui-time-check.mjs`: **passed**.
- `node scripts/fleet-layout-check.mjs`: **passed**.
- `flutter.bat test --no-pub`: **1 test passed**, a dispatch-model status parsing test.
- `flutter.bat analyze --no-pub`: **not clean**, exit 1: two informational findings and one warning. Deprecated `background` in `lib/main.dart:49`; `_preferredTime` can be final at dispatch screen line 30; always-true null comparison in `lib/services/breakdown_service.dart:92`.

Previous session HTTP tests exercised demo approval, ownership, fault recovery, scheduling and photo handling. Those were SQLite demo API tests, not a device-to-React/PostgreSQL end-to-end test, and were not rerun in this review.

| Required test category | What is still needed |
|---|---|
| Backend | Broader service/controller/validation/auth failure coverage; preserve existing agent/workflow/API checks. |
| PostgreSQL | Fresh migrations, constraints, transaction rollback, concurrent reservation/approval and persistence/restart tests against PostgreSQL itself. |
| React | Component, form validation, role/protected-route, API error/loading and integration tests. Standalone time/layout scripts do not replace these. |
| Flutter | Widget, form validation, navigation, login/API error, secure-session and integrated workflow tests. One model test is insufficient. |
| Full end-to-end | Capture the required two-client approval flow with PostgreSQL, including actual device feature use. |
| Performance | Repeatable concurrent workload and recorded response-time percentiles, success/failure rate, DB response and agent latency. No such report/tooling found. |
| Agent evaluation | Golden cases, schema/business-rule assertions, permission/adversarial cases, retries/failure recovery and auditable expected-versus-actual results. |

## What is not explicitly mandatory

The specification does not explicitly demand physical robots, ROS/Gazebo, collision-avoidance physics, autonomous charger docking, gradual charging, cargo-specific speed or refrigerated hardware. Current manual charging and category-only cargo handling are therefore not automatic failures by themselves. They can still weaken business-logic marks if your own requirements claim them as completed. Scope and reports must match implementation. A polished map does not replace the mandatory mobile/PostgreSQL/agent acceptance evidence.

## Priority before submission

1. **Prove the mandatory runtime:** PostgreSQL, both clients, a repeatable authorized cross-client approval scenario and a runnable APK. Validate schema and reservations before shared deployment.
2. **Close functional minimums:** mobile registration/search/filter and role UX; four meaningful endpoints/business operation per student-owned component; legitimate CRUD coverage; individual Flutter/React/backend/DB/AI evidence.
3. **Make agent execution evidence accurate:** persisted real step statuses, output validation, per-agent tool permissions, durable timing/error/retry records and complete golden-case evaluation. Decide and justify the custom agent approach rather than adding an LLM solely for appearance.
4. **Complete required tests:** PostgreSQL integration, React components, Flutter widgets/integration, performance and the cross-platform acceptance case. Keep actual run outputs.
5. **Deploy and prepare evaluator access:** React URL, API/health and Swagger URLs, PostgreSQL evidence, APK/install instructions, real third-party service proof and secure secret rotation.
6. **Finish documentation and ownership evidence:** architecture/agent/data diagrams, 3–6 well-grounded ADR decisions, consolidated report, test/performance/evaluation/deployment reports, genuine Git/PR/review links and truthful AI usage records.

## Submission and marks checklist

- Deadline stated in the supplied PDF: **Wednesday 30 September 2026, 11:50 PM**, one Course Web submission by the nominated group leader (p. 10). Check Course Web for any later official amendments.
- One consolidated PDF: Group Report plus one clearly labelled Individual Report per student. Include individual contributions, key commits/PRs/tests, learning, AI usage log, personal reflection and signed declaration.
- Runnable Android APK or an alternative approved in writing, with installation instructions.
- Publicly viewable **10-minute demonstration video link**; use `SE3090_GroupNumber` naming.
- Keep repository, video and deployed services accessible until at least **21 October 2026**. Group leader checks every link in a private browser.
- Final evaluation: **10-minute demonstration + 20-minute viva**, every student present. No external AI assistants/copilots during that evaluation; run the submitted application's own agent subsystem.
- Marks: 30 group (business logic 10; integration/orchestration/state 10; documentation/deployment 10) + 70 individual (API 10; PostgreSQL 10; React 10; Flutter 10; agent contribution 12; integration/security 10; testing/CI/Git 8). Scaled to 25% of the module.
- Do not claim a likely full score from passing tests: **70% is individual**, with understanding, ownership and ability to modify/debug assessed in the viva. No mark estimate is defensible without those demonstrations and missing artifacts.

## Main source files examined

`backend/Program.cs`; `backend/Controllers/*`; `backend/Services/WorkflowOrchestrator.cs`; `MissionSimulator.cs`; `WeatherService.cs`; `backend/Agents/*`; `backend/Data/SmartFleetDbContext.cs`; repository classes; `backend.Tests/*`; `web/src/App.tsx`; `web/src/components/Layout/Sidebar.tsx`; workflow pages; `mobile/lib/routes/app_router.dart`; `mobile/lib/services/*`; dispatch/breakdown screens; `mobile/test/api_configuration_test.dart`; `.github/workflows/*`; existing README and docs.
