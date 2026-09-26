export type DispatchRequestStatus =
  | 'Pending'
  | 'Planned'
  | 'AwaitingApproval'
  | 'Approved'
  | 'Rejected'
  | 'InTransit'
  | 'Completed'
  | 'Failed';

export interface MissionPlanStep {
  stepNumber: number;
  stepName: string;
  status: string;
  assignedAgent?: string;
}

export interface MissionPlannerOutput {
  dispatchRequestId: string;
  plan: MissionPlanStep[];
  createdAt: string;
}

export interface DispatchRequest {
  id: string;
  operatorId: string;
  operatorName?: string;
  operatorEmail?: string;
  roverId?: string | null;
  sourceZone: string;
  destinationZone: string;
  cargoType: string;
  priority: string;
  preferredTimeWindow: string;
  status: DispatchRequestStatus;
  latitude?: number | null;
  longitude?: number | null;
  createdAt: string;
  updatedAt: string;
  latestPlan?: MissionPlannerOutput | null;
}

export interface CreateDispatchRequestInput {
  sourceZone: string;
  destinationZone: string;
  cargoType: string;
  priority: string;
  preferredTimeWindow: string;
  latitude?: number;
  longitude?: number;
}

export interface DispatchRequestFilter {
  search?: string;
  status?: string;
  zone?: string;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
