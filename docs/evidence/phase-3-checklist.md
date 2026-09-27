# Phase 3: persisted checklist states

The orchestrator now persists the outcome of rover discovery, battery/weather checks, reservation and route preparation in `WorkflowRun.PlanJson`. Safety evaluation changes from InProgress to Completed or Blocked. Human approval stays Pending until a decision; explicit approval becomes Completed, automatic approval is Skipped, and rejection, revision, expiry and fault cancellation retain distinct outcomes.

`CurrentStep` identifies the first unfinished preparation step. Value 8 means the seven-step preparation checklist is finished; it does not mean delivery is finished. Delivery remains governed by workflow status and progress.

The approval's agent summary is an immutable snapshot taken before the safety decision. React labels it accordingly. Previously stored summaries and completed historical runs are not rewritten or fabricated. Use new missions to demonstrate the corrected evidence.

Regression checks cover a persisted human pause across database contexts, actual supervisor approval, automatic approval without a human decision, rejection and expiry. The remaining phase-3 work is structured plan/output validation, per-tool timing/error/attempt evidence, bounded technical retries and adversarial evaluation. This change alone does not close that entire phase.
