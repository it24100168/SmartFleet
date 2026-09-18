export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'RevisionRequested';

export interface ApprovalRequest {
  id: string;
  dispatchRequestId: string;
  roverId: string;
  agentSummaryJson: string;
  riskScore: number;
  riskReason: string;
  status: ApprovalStatus;
  reviewedById?: string | null;
  reviewedByName?: string | null;
  reviewNotes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PagedApprovalResult {
  items: ApprovalRequest[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface WorkflowExecutionLog {
  id: string;
  dispatchRequestId: string;
  stepName: string;
  agentName: string;
  inputJson: string;
  outputJson: string;
  validationResult: string;
  timestamp: string;
}

export interface ApprovalStats {
  totalPending: number;
  totalApproved: number;
  totalRejected: number;
  totalRevisionRequested: number;
  averageRiskScore: number;
}

export interface PlanStep {
  stepNumber: number;
  stepName: string;
  status: string;
}

export interface TelemetrySummary {
  batteryOk: boolean;
  weatherRisk: 'low' | 'medium' | 'high' | string;
  locked: boolean;
}

export interface MaintenanceSummary {
  breakdownReportId: string;
  likelyPart: string;
  estimatedRepairHours: number;
  severity: string;
  confidenceNote: string;
  recommendedAction: string;
}

export interface ParsedAgentSummary {
  dispatchRequestId?: string;
  roverId?: string;
  missionPlanSummary?: {
    plan?: PlanStep[];
  };
  telemetryResult?: TelemetrySummary;
  maintenanceResult?: MaintenanceSummary | null;
}
