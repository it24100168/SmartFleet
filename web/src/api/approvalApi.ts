import { axiosClient } from './axiosClient';
import { ApprovalRequest, PagedApprovalResult, ApprovalStats, WorkflowExecutionLog } from '../types/approval';

export interface GetApprovalsParams {
  status?: string;
  sortBy?: string;
  sortOrder?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export const approvalApi = {
  getApprovals: async (params?: GetApprovalsParams): Promise<PagedApprovalResult> => {
    const response = await axiosClient.get<PagedApprovalResult>('/approval-requests', { params });
    return response.data;
  },

  getApprovalById: async (id: string): Promise<ApprovalRequest> => {
    const response = await axiosClient.get<ApprovalRequest>(`/approval-requests/${id}`);
    return response.data;
  },

  getStats: async (): Promise<ApprovalStats> => {
    const response = await axiosClient.get<ApprovalStats>('/approval-requests/stats');
    return response.data;
  },

  approve: async (id: string, reviewNotes?: string): Promise<ApprovalRequest> => {
    const response = await axiosClient.post<ApprovalRequest>(`/approval-requests/${id}/approve`, {
      reviewNotes,
    });
    return response.data;
  },

  reject: async (id: string, reviewNotes?: string): Promise<ApprovalRequest> => {
    const response = await axiosClient.post<ApprovalRequest>(`/approval-requests/${id}/reject`, {
      reviewNotes,
    });
    return response.data;
  },

  requestRevision: async (id: string, reviewNotes?: string): Promise<ApprovalRequest> => {
    const response = await axiosClient.post<ApprovalRequest>(`/approval-requests/${id}/request-revision`, {
      reviewNotes,
    });
    return response.data;
  },

  getExecutionLogs: async (id: string): Promise<WorkflowExecutionLog[]> => {
    const response = await axiosClient.get<WorkflowExecutionLog[]>(`/approval-requests/${id}/execution-log`);
    return response.data;
  },

  simulateEvaluation: async (scenario: string = 'pending-approval'): Promise<any> => {
    const response = await axiosClient.post('/approval-requests/evaluate', { scenario });
    return response.data;
  },
};
