# SmartFleet Architecture & Documentation

Start with [implementation phases and PostgreSQL setup](PHASES.md), the [integrated demonstration](INTEGRATED-DEMO.md), and [how the prototype works](HOW-SMARTFLEET-WORKS.md).

Current architecture decision: [PostgreSQL persistence and critical approval](adr/001-assessment-persistence-and-critical-approval.md). The remaining diagrams and required architecture decisions below still need completion.

## Planned Artifacts
- **Architecture Decision Records (ADRs)**: Documenting technical decisions, framework selections, and communication protocols.
- **Entity-Relationship Diagrams (ERDs)**: Database schemas representing Users, Rovers, Dispatches, Telemetry events, and Maintenance records.
- **Agent Interaction Flows**: Sequence and state diagrams for the multi-agent AI pipeline (Mission Planner, Dispatch & Telemetry, Maintenance Mechanic, Safety Guard).
