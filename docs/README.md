# SmartFleet Architecture & Documentation

Start with [implementation phases and PostgreSQL setup](PHASES.md), the [integrated demonstration](INTEGRATED-DEMO.md), [how the prototype works](HOW-SMARTFLEET-WORKS.md), and the [full 17-page requirements audit](evidence/requirements-full-2026-10-05.md). The [Azure + Neon deployment runbook](AZURE-NEON-DEPLOY.md) gives the exact settings and evidence checks; the [hosting decision](DEPLOYMENT-PLAN.md) explains the service choices. Neither document claims a live deployment yet.

Architecture decisions cover [PostgreSQL persistence and critical approval](adr/001-assessment-persistence-and-critical-approval.md), [client state](adr/002-client-state.md), [agent orchestration](adr/003-orchestration-and-state.md), and [hosting preparation](adr/004-hosting-preparation.md). The [integrated architecture and workflow](architecture/integrated-system.md), [database ER diagram](architecture/database-erd.md) and [agent tool-permission map](architecture/agent-tool-permissions.md) record the current implementation and its known boundaries.

The audit contains a branch-based contribution matrix; each student still needs to confirm their own evidence and prepare their individual report section.
