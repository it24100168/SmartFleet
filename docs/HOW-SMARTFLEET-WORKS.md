# How the current SmartFleet prototype works

This describes the implemented code, not planned capabilities. The four agents are deterministic software components, not LLMs or independently learning robots. The backend coordinates their calls and stores decisions. The warehouse map visualizes simulated robot movement.

## Workflow and responsibilities

1. An Operator or Supervisor creates a request with pickup, destination, cargo category, priority and delivery target. Normal orders queue until about 40 seconds before the target, with a five-second intake window. Earliest target wins; equal targets use Critical, High, Medium, Low, then submission time. Already moving robots are not interrupted. The Fleet Simulation scenario button starts an immediate demo request instead.
2. Mission Planner creates a checklist: find a rover, check battery and weather, reserve it, schedule the route and evaluate safety. The checklist is a fixed template populated with the requested route. The backend scheduler handles timing and priority; the planner itself does not choose robots.
3. Dispatch & Telemetry looks for an eligible idle rover at pickup, highest battery first. If none is available there, it considers idle rovers elsewhere, highest battery first. Eligibility requires no existing mission, enough battery and no unresolved breakdown. Reservation uses a conditional database update to prevent double assignment. Lack of eligible capacity leaves the request queued for retry.
4. Maintenance Mechanic matches reported symptoms, descriptions and error codes against the failure catalog. It returns a likely part, severity, estimated repair hours and a recommendation. No catalog match means manual review. The workflow diagnoses open reports and excludes affected rovers. If there are no reports, maintenance is logged as skipped. Uploaded photographs are evidence; there is no image-analysis diagnosis.
5. Safety Guard checks the plan, reservation, battery and weather. A healthy low-weather-risk mission proceeds automatically unless its priority is Critical. Critical dispatches always require supervisor authorization before movement. Medium weather also normally creates a pending supervisor approval and reserves the rover without moving. High/unknown weather or missing mandatory prerequisites blocks execution. Risk scores of 30 or higher require review; 75 or higher reject, and hard safety failures can reject below that score.
6. The backend mission simulator advances persisted progress about once per second, for an approximately 40-second delivery. It updates destination and battery on completion, releases the reservation and records the result. The frontend polls and draws the result; it does not drive the backend workflow. The API must remain running.

Approval can be approved, rejected or sent back for revision. Approving rechecks eligibility; pending reservations expire after ten minutes. A reported motor fault automatically stops/fails an active mission and excludes the rover. A Supervisor or Technician explicitly recovers it to maintenance in demo mode and marks it repaired. A failed mission is not automatically resumed; replan or create another request.

## How battery and charging work

Battery eligibility is the greater of 40% and a simple pickup-to-destination route estimate. A1 to B3 currently requires 44%. This is an illustrative estimate, not a physical energy model; it does not account for cargo weight, refrigeration or the approach from the robot's current location.

Battery decreases once at delivery completion, not continuously. A1 to B3 subtracts 24 percentage points: an example rover starting at 80% finishes at 56%. Completion or repair sets a rover below 40% to Charging. That state does not drive it to a charging bay or increase its battery over time.

**Demo charge** is a manual Supervisor-only action, available only in demo mode for Idle/Charging robots with no mission. It immediately sets battery to 100% and status to Idle. It does not move the robot. Reserved, moving and maintenance robots cannot use it. Automatic charger travel, docking, gradual charging and charger capacity are not implemented.

## Cargo categories

| Choice | What the current prototype does | Not yet implemented |
|---|---|---|
| Standard | Stores the category in the request, planning input and mission display | Payload/weight capability checks |
| Fragile | Stores the category; uses the same selection and movement rules | Reduced speed, gentler acceleration, vibration/shock limits |
| Refrigerated | Stores the category; uses the same selection and movement rules | Refrigerated rover capability, temperature monitoring, cold-chain alarms and cooling energy |

These choices do not currently change duration, battery calculation, risk scoring or robot equipment selection. Present them as recorded classifications, not completed special-handling features.

## Manual versus automatic

| Human action | Automatic system response |
|---|---|
| Submit request and choose route/cargo/priority/time | Validate, queue, schedule, build checklist, select and reserve an eligible rover |
| Choose a demo weather scenario | Feed its labelled weather fixture through real backend decision rules |
| Supervisor reviews a flagged mission | Revalidate then start, release on rejection/revision, or expire after ten minutes |
| Report a breakdown and optionally attach a photo | Stop the active mission, exclude rover, look up diagnosis and record evidence |
| Recover the robot and confirm repair | Move its demo position to maintenance; release after all open reports are repaired |
| Click Demo charge | Immediately set an eligible robot to 100%/Idle |
| Replan a failed/revised mission | Run the checks again and reserve when eligible |
| Open the dashboard or press Refresh | Read persisted state; the worker advances missions even if the page is closed |

The map now spreads nearby rover markers apart and uses dotted leader lines to show actual anchors. The roster below the map shows every rover individually. These are display offsets, not physical parking slots, collision avoidance or changes to the stored location.

## Suggested presentation

Start with at least two charged, repaired rovers. Show a clear-weather delivery and the stored input/output for each agent. Then show a medium-weather pause and a supervisor decision. Report a motor fault during a later mission, show catalog diagnosis and exclusion, then recover/repair it. Finally submit two equal-target orders two minutes ahead with different priorities. Explain that both can run if capacity exists, while priority determines allocation order. Show cargo categories as a current limitation and explain the additional capability and handling rules needed next.
