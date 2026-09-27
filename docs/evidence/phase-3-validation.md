# Phase 3 validation and execution evidence

Implemented after the checklist-state correction:

- Planner output must correlate to its request, contain seven ordered Pending steps and use only the expected agent delegations. Unsupported tool/agent names, duplicated steps, pre-completed steps and changed request identities are rejected before reservation.
- Telemetry output must correlate to the request; a claimed reservation requires a rover, sufficient battery and admissible weather. Safety outputs have bounded risk scores and consistent approval flags; Critical cannot be automatically approved. Maintenance output must correlate to its report and use known severity/action values.
- Weather HTTP calls allow at most two attempts, do not retry authentication failures, propagate caller cancellation, and treat malformed/empty observations as unavailable. Unknown condition codes remain unknown. Each attempt records duration, safe outcome code and HTTP status, without URLs, API keys or exception messages. These attempts are persisted in the workflow log when planning commits.
- Agent-level duration and attempt metadata is persisted separately from original contract payloads. This measures each agent operation, not each individual SQL statement.
- Scheduling technical failures roll back first, clear tracked state, then record a separate durable failure. Three technical failures mark the request Failed. Other pending requests continue. Ordinary waiting for capacity is not a technical failure and does not consume this limit.

Tests exercise malformed weather, bounded retry, authentication failure, cancellation, unsupported delegation, identity/status corruption, Critical approval bypass and instruction-like cargo data. Free text is data in this deterministic implementation; it is never executed as a tool or used to assign permissions.

Remaining limits: an unavailable database cannot persist its own failure log; the worker still retries infrastructure failures. Per-repository SQL timing and failed agent-call duration are not yet recorded. Weather retry evidence on an approval recheck is not yet included in the approval audit. These are remaining phase-3 items, not completed capabilities.
