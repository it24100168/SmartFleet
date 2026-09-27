# ADR 003: Deterministic orchestration and durable state

Status: documents current implementation, 27 September 2026.

The custom ASP.NET orchestrator calls four typed components with fixed server capabilities. It uses deterministic planning and catalog/rule-based decisions. It is not an LLM, learning system or autonomous physical robot controller. A model framework was not introduced merely to rename the same fixed operations; lecturer acceptance of this interpretation should be confirmed if required.

DispatchRequests stores the objective and business state. WorkflowRuns stores the structured plan, step state, reservation, progress and correlation. WorkflowExecutionLogs stores observed inputs, outputs and outcomes. ApprovalRequests persists human decisions. PostgreSQL transactions, conditional rover reservation and concurrency tokens protect transitions. JSON is stored in text columns for the current EF model; relational columns support routing and filtering. A fully normalized step table was deferred to avoid migration churn, at the cost of weaker SQL-level validation of each JSON step.

The public API cannot select arbitrary tools or raw rover state. Typed output validators reject unsupported delegations and inconsistent outcomes. A Critical request pauses for supervisor authorization; unsafe prerequisites still reject. Scheduling technical failures are bounded at three persisted attempts, while waiting for available capacity is not an error. Infrastructure outages remain a separate recovery limitation.
