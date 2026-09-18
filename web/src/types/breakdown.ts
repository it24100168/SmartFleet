export type BreakdownStatus = 'Reported' | 'Diagnosing' | 'ScheduledForRepair' | 'Repaired';

export interface MaintenanceMechanicOutput {
  breakdownReportId: string;
  likelyPart: string;
  estimatedRepairHours: number;
  severity: string;
  confidenceNote: string;
  recommendedAction: string;
}

export interface BreakdownReport {
  id: string;
  roverId: string | null;
  reportedById: string;
  reportedByName: string;
  reportedByEmail: string;
  symptomCategory: string;
  description: string;
  photoUrl: string | null;
  errorCode: string | null;
  status: BreakdownStatus;
  diagnosisResult: MaintenanceMechanicOutput | null;
  createdAt: string;
  updatedAt: string;
}

export interface PagedBreakdownReports {
  items: BreakdownReport[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
