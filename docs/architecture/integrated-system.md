# Integrated SmartFleet architecture and assessed workflow

The warehouse map and rover movement are a database-backed **simulation**, not physical navigation hardware. React and Flutter are separate clients of the same ASP.NET Core API. The API owns authentication, roles, validation, domain rules, PostgreSQL persistence, and the controlled agent workflow. Neither client has a direct database or agent-tool connection.

```mermaid
flowchart LR
    OP[Flutter mobile<br/>Operator / Technician / Supervisor]
    WEB[React web<br/>Monitoring / Supervisor approval]
    API[ASP.NET Core Web API<br/>JWT, roles, DTOs, validation, services]
    DB[(PostgreSQL<br/>EF Core migrations, business records,<br/>workflow state, approval and logs)]
    ORCH[Workflow orchestrator<br/>typed calls, validation, transaction]
    AGENTS[Planner · Dispatch/Telemetry<br/>Maintenance · Safety Guard]
    WX[Open-Meteo or OpenWeather<br/>server-side only]
    OP <-->|HTTPS REST/JSON| API
    WEB <-->|HTTPS REST/JSON| API
    API <-->|EF Core/Npgsql| DB
    API --> ORCH
    ORCH --> AGENTS
    AGENTS --> ORCH
    ORCH -->|controlled weather check| WX
```

In the local assessment setup the API and clients use HTTP on `localhost`/Android emulator `10.0.2.2`. HTTPS, restricted cloud credentials, hosted PostgreSQL, live health and Swagger URLs remain deployment work; the diagram shows the intended deployed transport rather than claiming it is already live.

The critical-priority scenario is the required cross-platform demonstration:

```mermaid
sequenceDiagram
    actor Operator
    participant Flutter
    participant API as ASP.NET Core API
    participant PG as PostgreSQL
    participant Agents as Controlled agents
    participant React
    actor Supervisor
    Operator->>Flutter: Submit Critical request
    Flutter->>API: Authenticated dispatch creation
    API->>PG: Validate and persist request
    API->>Agents: Objective, structured plan and typed delegation
    Agents->>PG: Validated state, logs, rover reservation, approval
    API-->>Flutter: AwaitingApproval and request ID
    React->>API: Read pending approvals
    API->>PG: Read matching approval and workflow
    API-->>React: Risk, plan and decision context
    Supervisor->>React: Approve or reject
    React->>API: Authorized decision with note
    API->>PG: Revalidate reservation and save decision
    API-->>React: Updated status
    API->>PG: Simulate movement and save final state
    Flutter->>API: Refresh history/status by request ID
    API-->>Flutter: Completed, rejected or safe failure
```

The same request ID must be visible in both clients and the persisted workflow. Movement starts only after approval; a rejection or invalidated reservation releases the rover. The agents are deterministic C# components, not an LLM or physical robot controller. See the [agent permission map](agent-tool-permissions.md), [database ER diagram](database-erd.md), and [orchestration ADR](../adr/003-orchestration-and-state.md) for exact implementation limits.
