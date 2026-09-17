import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User, Role, LoginCredentials, AuthResponse } from '../types/auth';
import { authApi } from '../api/authApi';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<AuthResponse>;
  logout: () => void;
  hasRole: (roles: Role | Role[]) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Hydrate user and token from localStorage on initial load
  useEffect(() => {
    try {
      const storedToken = localStorage.getItem('smartfleet_token');
      const storedUser = localStorage.getItem('smartfleet_user');

      if (storedToken && storedUser) {
        setToken(storedToken);
        setUser(JSON.parse(storedUser));
      }
    } catch (e) {
      console.error('Failed to parse stored auth session:', e);
      localStorage.removeItem('smartfleet_token');
      localStorage.removeItem('smartfleet_user');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const login = async (credentials: LoginCredentials): Promise<AuthResponse> => {
    const authData = await authApi.login(credentials);
    const authenticatedUser: User = {
      id: authData.id,
      name: authData.name,
      email: authData.email,
      role: authData.role,
    };

    localStorage.setItem('smartfleet_token', authData.token);
    localStorage.setItem('smartfleet_user', JSON.stringify(authenticatedUser));

    setToken(authData.token);
    setUser(authenticatedUser);

    return authData;
  };

  const logout = () => {
    localStorage.removeItem('smartfleet_token');
    localStorage.removeItem('smartfleet_user');
    setToken(null);
    setUser(null);
  };

  const hasRole = (roles: Role | Role[]): boolean => {
    if (!user) return false;
    if (Array.isArray(roles)) {
      return roles.includes(user.role);
    }
    return user.role === roles;
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token,
        isLoading,
        login,
        logout,
        hasRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
