# SmartFleet relational schema (PostgreSQL)

This diagram reflects the EF Core model in `backend/Data/SmartFleetDbContext.cs`. Solid relationships below are **database foreign keys**, not merely matching identifier values. The local PostgreSQL assessment database is created by EF migrations.

```mermaid
erDiagram
    USERS ||--o{ DISPATCH_REQUESTS : creates
    USERS ||--o{ BREAKDOWN_REPORTS : reports
    USERS o|--o{ APPROVAL_REQUESTS : reviews
    DISPATCH_REQUESTS ||--o{ WORKFLOW_RUNS : starts

    USERS {
        uuid Id PK
        string Name
        string Email UK
        string PasswordHash
        string Role
        datetime CreatedAt
        datetime UpdatedAt
    }
    ROVERS {
        uuid Id PK
        string Identifier UK
        string Status
        int BatteryPercentage
        string LocationZone
        string CurrentMissionId
        datetime CreatedAt
        datetime UpdatedAt
    }
    DISPATCH_REQUESTS {
        uuid Id PK
        uuid OperatorId FK
        uuid RoverId
        string SourceZone
        string DestinationZone
        string CargoType
        string Priority
        datetime PreferredTimeWindow
        string Status
        decimal Latitude
        decimal Longitude
        datetime CreatedAt
        datetime UpdatedAt
    }
    WORKFLOW_RUNS {
        uuid Id PK
        uuid DispatchRequestId FK
        uuid RoverId
        string ObjectiveJson
        string PlanJson
        int CurrentStep
        string Status
        decimal Progress
        string WeatherRisk
        datetime ReservedUntil
        datetime CreatedAt
        datetime UpdatedAt
    }
    BREAKDOWN_REPORTS {
        uuid Id PK
        uuid RoverId
        uuid ReportedById FK
        string SymptomCategory
        string Description
        string PhotoUrl
        string ErrorCode
        string Status
        string DiagnosisResultJson
        datetime CreatedAt
        datetime UpdatedAt
    }
    FAILURE_CATALOGS {
        uuid Id PK
        string SymptomKeyword
        string SymptomCategory
        string LikelyPart
        int EstimatedRepairHours
        string Severity
    }
    APPROVAL_REQUESTS {
        uuid Id PK
        uuid WorkflowRunId
        string DispatchRequestId
        string RoverId
        uuid ReviewedById FK
        string Status
        int RiskScore
        string RiskReason
        datetime CreatedAt
        datetime UpdatedAt
    }
    WORKFLOW_EXECUTION_LOGS {
        uuid Id PK
        uuid WorkflowRunId
        string DispatchRequestId
        string AgentName
        string StepName
        string ValidationResult
        datetime Timestamp
    }
```

`RoverId` in dispatch, workflow and breakdown records is a **logical rover reference without an enforced FK**. `ApprovalRequests.WorkflowRunId`, `WorkflowExecutionLogs.WorkflowRunId`, their `DispatchRequestId` strings, and `Rovers.CurrentMissionId` are also logical references without FKs. The approval row has a unique index on `WorkflowRunId`, while email and rover identifier have unique indexes. Other query indexes are configured in the EF model. This is a schema limitation to fix or justify in the report; the diagram deliberately does not present these logical references as enforced relationships.

The full field types and constraints are in the EF model and migrations. `ObjectiveJson`, `PlanJson`, `DiagnosisResultJson`, agent summaries and execution input/output are serialized text columns, rather than separate normalized tables. They hold workflow evidence and are bounded by application validation; no hidden reasoning, password or API token is intended to be stored there.
