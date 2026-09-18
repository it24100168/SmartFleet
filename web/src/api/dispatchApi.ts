import axiosClient from './axiosClient';
import {
  DispatchRequest,
  CreateDispatchRequestInput,
  DispatchRequestFilter,
  PagedResult,
  MissionPlannerOutput,
  DispatchRequestStatus,
} from '../types/dispatch';

export const dispatchApi = {
  getDispatchRequests: async (params?: DispatchRequestFilter): Promise<PagedResult<DispatchRequest>> => {
    const response = await axiosClient.get<PagedResult<DispatchRequest>>('/dispatch-requests', {
      params,
    });
    return response.data;
  },

  getDispatchRequestById: async (id: string): Promise<DispatchRequest> => {
    const response = await axiosClient.get<DispatchRequest>(`/dispatch-requests/${id}`);
    return response.data;
  },

  createDispatchRequest: async (data: CreateDispatchRequestInput): Promise<DispatchRequest> => {
    const response = await axiosClient.post<DispatchRequest>('/dispatch-requests', data);
    return response.data;
  },

  updateStatus: async (id: string, status: DispatchRequestStatus): Promise<DispatchRequest> => {
    const response = await axiosClient.patch<DispatchRequest>(`/dispatch-requests/${id}/status`, {
      status,
    });
    return response.data;
  },

  generatePlan: async (id: string): Promise<MissionPlannerOutput> => {
    const response = await axiosClient.post<MissionPlannerOutput>(`/dispatch-requests/${id}/plan`);
    return response.data;
  },
};
