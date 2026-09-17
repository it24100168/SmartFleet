import axiosClient from './axiosClient';
import { AuthResponse, LoginCredentials, RegisterCredentials } from '../types/auth';

export const authApi = {
  login: async (credentials: LoginCredentials): Promise<AuthResponse> => {
    const response = await axiosClient.post<AuthResponse>('/auth/login', credentials);
    return response.data;
  },

  register: async (credentials: RegisterCredentials): Promise<AuthResponse> => {
    const response = await axiosClient.post<AuthResponse>('/auth/register', credentials);
    return response.data;
  },

  testOperatorEndpoint: async () => {
    const response = await axiosClient.get('/test/operator-only');
    return response.data;
  },

  testTechnicianEndpoint: async () => {
    const response = await axiosClient.get('/test/technician-only');
    return response.data;
  },

  testSupervisorEndpoint: async () => {
    const response = await axiosClient.get('/test/supervisor-only');
    return response.data;
  },

  testAuthenticatedEndpoint: async () => {
    const response = await axiosClient.get('/test/authenticated');
    return response.data;
  },
};
