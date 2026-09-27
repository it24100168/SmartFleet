# ADR 001: PostgreSQL assessment persistence and critical dispatch authorization

Date: 2026-09-27. Status: Accepted for local assessment preparation.

## Context

The assignment requires PostgreSQL and a workflow that starts in one client, pauses for approval in another and returns a result. The earlier local simulator coupled its simulated behavior to SQLite. Ordinary low-weather mobile requests automatically passed safety, making the approval demonstration unreliable.

## Options

- Keep SQLite and wait for external weather to trigger approval: convenient but does not meet the database requirement or give a repeatable demonstration.
- Add a UI-only force-approval switch: easy to demonstrate but weak business justification and client-controlled policy.
- Separate database choice from simulation and require supervisor authorization for Critical dispatches: repeatable and enforced by persisted business data.

## Decision

PostgreSQL is the default provider. SQLite requires explicit configuration and simulation enabled. The local assessment environment runs real EF migrations on an isolated PostgreSQL 16 database while retaining explicitly marked simulated weather and motion. Startup migrations are opt-in; deployment migration policy remains a later decision.

Safety Guard obtains priority from the stored dispatch request through the orchestrator. A Critical request that passes hard safety checks must await supervisor authorization even with low numerical risk. Hard rejection conditions take precedence. Approval continues through the existing role checks, durable approval record, reservation expiry and resume validation.

## Consequences

Demo movement does not imply SQLite. The team must explain which inputs are simulated and which records persist. PostgreSQL integration tests exercise actual migrations/concurrency rather than substituting an in-memory provider. Critical requests no longer move automatically, and documentation/UI/tests must reflect the policy. The local volume and generated password must be retained together. Deployment, live device evidence and the remaining required architecture ADRs are not resolved by this decision.
