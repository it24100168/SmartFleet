export type Role = 'Operator' | 'Technician' | 'Supervisor';

export interface User {
  id: string;
  name: string;
  email: string;
  role: Role;
}

export interface AuthResponse {
  token: string;
  id: string;
  name: string;
  email: string;
  role: Role;
  expiresAt: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  name: string;
  email: string;
  password: string;
  role: Role;
}
