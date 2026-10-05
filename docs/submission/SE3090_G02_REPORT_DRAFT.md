# SE3090_G02 — consolidated report working draft

**Status: incomplete submission source.** This document assembles verified technical material for Group 2. The four students must review it, add their truthful individual sections, AI logs, personally written reflections and signatures, then export **one PDF**. Fill every `[REQUIRED]` entry and remove this status line only after checking every claim against the final pushed revision and deployed URLs. The team reports a Course Web deadline of **6 October 2026, 11:00 AM Sri Lanka time**; the leader must confirm it in Course Web.

## Group report — project and scope

SmartFleet addresses internal warehouse dispatch coordination. An Operator requests a delivery and reports faults from Flutter. A Technician diagnoses and repairs rovers. A Supervisor monitors the fleet in React and approves, rejects or requests revision for critical/risky work. The rovers and floor map are a **simulation**; the project does not claim physical robot navigation or refrigerated hardware.

The public application layer is a .NET 8 ASP.NET Core REST API. Both React and Flutter use its authenticated JSON endpoints; EF Core/Npgsql persists users, rovers, requests, workflow state, fault reports, approval decisions and audit logs in PostgreSQL. An optional OpenWeather call runs server-side. The [system and workflow diagrams](../architecture/integrated-system.md), [ERD](../architecture/database-erd.md) and [repository layout](../../README.md) document the architecture. Explain in the PDF that some rover/approval/log identifiers remain logical references rather than enforced database foreign keys.

Technology decisions are in [ADR 001](../adr/001-assessment-persistence-and-critical-approval.md) (PostgreSQL and critical approval), [ADR 002](../adr/002-client-state.md) (React/Flutter state), [ADR 003](../adr/003-orchestration-and-state.md) (custom deterministic agent workflow), and [ADR 004](../adr/004-hosting-preparation.md) (hosting preparation). The proposed final hosting topology is in the [deployment plan](../DEPLOYMENT-PLAN.md); update the ADR after the group chooses and verifies actual services.

## Group report — business rules and assessed workflow

Dispatch accepts known, distinct pickup/drop-off zones, a cargo category, priority and target time. The workflow checks route battery requirements, rover state/faults and weather. A database reservation prevents simultaneous ownership of one rover. Critical priority requires a Supervisor decision before movement. A failed prerequisite cannot be approved into movement. The background worker advances simulated progress from persisted state; completion updates the request, workflow, rover destination and battery. Breakdowns stop affected work and require repair/recovery before reuse. Use the [demo guide](../INTEGRATED-DEMO.md) to capture a single request ID from native Flutter submission through React approval to native Flutter completion and PostgreSQL logs.

The four custom C# agents have separate typed contracts: Mission Planner creates a seven-step plan; Dispatch & Telemetry chooses/reserves a rover and checks weather/battery; Maintenance Mechanic diagnoses known fault patterns or requests manual review; Safety Guard scores risk and opens an approval. The orchestrator validates outputs and persists objective, checklist, timings, decisions and final state. [Tool permissions](../architecture/agent-tool-permissions.md) are limited to injected operations. The planner's sequence remains largely template-based; describe that honestly and cite the [orchestration ADR](../adr/003-orchestration-and-state.md). Do not describe it as an LLM.

## Group report — testing and evaluation

At the audit revision, PostgreSQL-backed backend tests passed **54/54**, Flutter tests passed **8** with an optional live service test skipped, and React Vitest interaction tests passed **6/6**; the React production build passed. Re-run and replace these numbers after the final changes. [PostgreSQL cross-client service evidence](../evidence/phase-1-2-verification.md) and [agent validation evidence](../evidence/phase-3-validation.md) describe what was checked and what was not. The [local performance report](../evidence/performance-local-2026-10-05.md) records actual concurrent HTTP samples and agent timings, including its high-latency rover-list sample. Do not present local observations as hosted performance results.

For the formal Agentic AI evaluation, add a table with scenario, objective, expected agents/tools/plan, expected validation/approval/outcome, actual workflow ID/log evidence, pass/fail and explanation. Minimum rows: safe standard dispatch, Critical waiting/approve, reject/revise, insufficient battery, faulted rover exclusion and diagnosis, invalid zone, unavailable weather, malformed agent output, injection-like free text treated as data, reservation race, and database/service failure recovery. Link each row to a real test or recorded run; do not invent a run.

[REQUIRED] Native Flutter Critical → React Supervisor → native completion evidence with same ID and zero pre-approval movement.

[REQUIRED] Native camera or GPS permission/capture evidence and real OpenWeather response/failure evidence.

[REQUIRED] Final revision's backend, React, Flutter, end-to-end, performance and agent-evaluation outputs, with dates/environment.

## Group report — security, deployment and access

The API validates JWT issuer/audience/lifetime, enforces roles and request ownership, validates DTOs and outputs, bounds weather attempts and keeps API/database credentials server-side. Operator registration cannot grant privileged roles. Cloud Supervisor/Technician accounts use private, one-time provisioner configuration and hashed passwords. Include the real HTTPS API health/Swagger, React, PostgreSQL migration evidence and final APK install instructions once deployed. Do not put connection strings, JWT secrets, weather keys or bearer tokens in this PDF.

[REQUIRED] React live URL: `...`

[REQUIRED] API health URL: `...`; Swagger URL: `...`

[REQUIRED] PostgreSQL provider/host **name only**, migration result, backup/access policy; no password.

[REQUIRED] Android APK filename/location/install steps and verification on a clean emulator/device.

[REQUIRED] Evaluator accounts and credentials delivered by an institution-approved private channel, not this public report or Git.

## Group report — collaboration and references

The four original feature branch heads are merged into `main`. Public [backend CI](https://github.com/it24100168/SmartFleet/actions/runs/36295904907) and [web CI](https://github.com/it24100168/SmartFleet/actions/runs/36295904866) succeeded on the 27 September pushed revision. Push and verify new CI before citing it as evidence for the final submission. The [full requirements audit](../evidence/requirements-full-2026-10-05.md) maps the current work and known limits. At the 5 October audit, the public repository had no PRs or issues; do not state that reviews or a board already exist. If the group uses new issues/PRs for remaining work, link the real items here.

[REQUIRED] Source/library/API references and acknowledgement of Google Antigravity, OpenAI Codex and any other tools actually used. Each student must check their own AI log.

## Individual report — M.H. Hamdhan (IT24100168), rover/telemetry

Verified branch: `rover-telemetry` at `c9502b8`; it adds rover entity/repository/API, telemetry/weather agent, tests and React telemetry. Integration commits attributed to this Git identity are visible later on `main`. **Student to supply:** personally owned Flutter work, exact later commits/files/tests, implementation challenges, architectural choices, AI-use log, personally written approximately one-page reflection and signed declaration. Demonstrate/modify the rover selection or safe business operation in the viva.

## Individual report — R.M.P.T. Rathnayake (IT24103606), maintenance

Verified branch: `feature/maintenance-mechanic` at `98fab79`; it adds breakdown/knowledge-base data and API, maintenance agent/tests, React maintenance queue and Flutter breakdown workflow. **Student to supply:** exact commits/files/tests and later changes, fault diagnosis demonstration, challenges, AI-use log, personally written reflection and signed declaration.

## Individual report — H.M.P.C.B. Herath (IT24100382), dispatch/planner

Verified branch: `feature/dispatch-mission-planner` at `23e111a`; it adds dispatch entity/API/service, planner/tests, React dispatch UI and Flutter dispatch screen. **Student to supply:** exact commits/files/tests and later changes, route/time/priority demonstration, challenges, AI-use log, personally written reflection and signed declaration.

## Individual report — I.G.D.S. Nawarathna (IT24102876), safety/approval

Verified branch: `feature/safety-guard-approval` at `68f3bf0`; it adds approval and log schema/API, Safety Guard/tests, React approval and a large Flutter scaffold. **Student to supply:** distinguish personally implemented Flutter logic from generated scaffolding, exact later commits/files/tests, approval/rejection demonstration, challenges, AI-use log, personally written reflection and signed declaration.

## Group AI-use declaration — team review required

The team reported use of Google Antigravity to develop component branches, and OpenAI Codex has helped review, integrate, test, document and fix SmartFleet. **Before signing, each student must verify the exact tools/models, dates, tasks, accepted/rejected output and verification in their own authentic log.** This draft does not establish that everyone used the same tool or reviewed every change. No external AI assistant may be used to answer questions or modify submitted work during the final demonstration/viva; the submitted application's own agent subsystem must run.

## Submission checklist

- [ ] All four students checked technical claims, contributed their own sections/logs/reflections and signed declarations.
- [ ] One consolidated PDF named under `SE3090_G02`; no separate individual-report uploads unless Course Web instructs otherwise.
- [ ] Final pushed GitHub revision passed backend and web CI; source, APK, URLs and reports match that revision.
- [ ] Publicly viewable working 10-minute video link added and checked in an incognito browser.
- [ ] API health, Swagger, React, database-backed workflow and APK checked from a clean session.
- [ ] Repository, video and deployed services remain accessible to evaluators until at least 21 October 2026.
- [ ] Group leader confirmed the reported 6 October 11:00 AM extension in Course Web and submitted before it.
