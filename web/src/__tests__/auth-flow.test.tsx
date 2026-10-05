import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '../context/AuthContext';
import { ProtectedRoute } from '../components/ProtectedRoute';
import { Login } from '../pages/Login';
import { authApi } from '../api/authApi';
import client from '../api/axiosClient';
import type { Role } from '../types/auth';

vi.mock('../api/authApi', () => ({
  authApi: { login: vi.fn(), register: vi.fn() },
}));
vi.mock('../api/axiosClient', () => ({
  default: { get: vi.fn() },
}));

const supervisor = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Test Supervisor',
  email: 'supervisor@example.test',
  role: 'Supervisor' as Role,
};

function renderProtected() {
  render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/approvals']}>
        <Routes>
          <Route path="/login" element={<div>Login destination</div>} />
          <Route
            path="/approvals"
            element={
              <ProtectedRoute allowedRoles={['Supervisor']}>
                <div>Approval Center content</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );
}

function renderLogin() {
  render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/" element={<div>Fleet dashboard</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );
}

beforeEach(() => {
  localStorage.clear();
  vi.clearAllMocks();
  vi.mocked(client.get).mockResolvedValue({ data: { demo: false } });
});

afterEach(() => cleanup());

describe('React authentication and role navigation', () => {
  it('redirects an unauthenticated visitor from approvals to login', async () => {
    renderProtected();
    expect(await screen.findByText('Login destination')).toBeTruthy();
  });

  it('blocks an Operator from the Supervisor approval route', async () => {
    localStorage.setItem('smartfleet_token', 'test-token');
    localStorage.setItem('smartfleet_user', JSON.stringify({ ...supervisor, role: 'Operator' }));
    renderProtected();
    expect(await screen.findByText('Access Restricted')).toBeTruthy();
    expect(screen.queryByText('Approval Center content')).toBeNull();
  });

  it('allows a Supervisor into the approval route', async () => {
    localStorage.setItem('smartfleet_token', 'test-token');
    localStorage.setItem('smartfleet_user', JSON.stringify(supervisor));
    renderProtected();
    expect(await screen.findByText('Approval Center content')).toBeTruthy();
  });

  it('keeps an invalid email from reaching the API', async () => {
    renderLogin();
    const email = screen.getByLabelText('Email Address') as HTMLInputElement;
    fireEvent.change(email, { target: { value: 'invalid-address' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'example-pass' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign In to SmartFleet' }));
    expect(email.checkValidity()).toBe(false);
    expect(authApi.login).not.toHaveBeenCalled();
  });

  it('shows an API authentication error without storing a session', async () => {
    vi.mocked(authApi.login).mockRejectedValue({ response: { data: { message: 'Invalid credentials' } } });
    renderLogin();
    fireEvent.change(screen.getByLabelText('Email Address'), { target: { value: 'operator@example.test' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'wrong-pass' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign In to SmartFleet' }));
    expect(await screen.findByText('Invalid credentials')).toBeTruthy();
    expect(localStorage.getItem('smartfleet_token')).toBeNull();
  });

  it('stores a successful API login and opens the fleet dashboard', async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      ...supervisor,
      token: 'session-token',
      expiresAt: '2026-10-06T00:00:00Z',
    });
    renderLogin();
    fireEvent.change(screen.getByLabelText('Email Address'), { target: { value: supervisor.email } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'valid-pass' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign In to SmartFleet' }));
    expect(await screen.findByText('Fleet dashboard')).toBeTruthy();
    await waitFor(() => expect(localStorage.getItem('smartfleet_token')).toBe('session-token'));
  });
});
