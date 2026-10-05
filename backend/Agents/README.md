# SmartFleet agents

SmartFleet uses four distinct, deterministic C# agents coordinated by
`WorkflowOrchestrator`. They operate on validated inputs and persist workflow
steps, tool outcomes, validation results, approval decisions and execution
summaries in PostgreSQL. These agents do not use an external language model or
control physical robots.

| Agent | Original component owner | Implemented responsibility |
| --- | --- | --- |
| `MissionPlannerAgent` | H.M.P.C.B. Herath (IT24100382), dispatch | Creates a seven-step mission checklist and delegates rover, battery, weather and safety work. The checklist is structured but mostly template-based; it does not optimize multi-stop routes. |
| `DispatchTelemetryAgent` | M.H. Hamdhan (IT24100168), rover telemetry | Selects an available rover for the pickup zone, checks route battery reserve and server-side weather, then attempts a transactional reservation. It returns a structured failure when no safe rover can be reserved. |
| `MaintenanceMechanicAgent` | R.M.P.T. Rathnayake (IT24103606), maintenance | Looks up allow-listed fault patterns and returns a likely part, severity and repair recommendation, or `NeedsManualReview`. A Technician or Supervisor records the actual repair. |
| `SafetyGuardAgent` | I.G.D.S. Nawarathna (IT24102876), safety and approval | Checks the aggregated plan, reservation, battery, weather, fault and priority evidence. It rejects failed prerequisites, allows safe ordinary requests, or persists a pending decision for Supervisor approval. |

The dispatch workflow runs these agents through
`backend/Services/WorkflowOrchestrator.cs`. The Maintenance Mechanic diagnoses
an open report when one exists; a no-open-reports check is logged otherwise.
Critical-priority work pauses for an authorized Supervisor decision in the React
Approval Center. Rover movement and charging are simulated from database state
on the warehouse map; route geometry follows the illustrative aisles in
`backend/Services/WarehouseLayout.cs`.

The names and ownership in this table describe the original component branches.
Each student must separately identify and explain their own backend, database,
React, Flutter, test and agent contributions in the individual report. Branch
ownership alone does not prove that every student authored work in every layer.
