import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { authApi } from '../api/authApi';
import { ErrorAlert } from '../components/Common/ErrorAlert';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { Bot, KeyRound, Mail, User as UserIcon, Shield } from 'lucide-react';
import { Role } from '../types/auth';

export const Login: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();

  const [isRegistering, setIsRegistering] = useState<boolean>(false);
  const [email, setEmail] = useState<string>('supervisor@smartfleet.internal');
  const [password, setPassword] = useState<string>('Password123!');
  const [name, setName] = useState<string>('Sarah Connor');
  const [role, setRole] = useState<Role>('Supervisor');

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const redirectPath = (location.state as any)?.from?.pathname || '/';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      if (isRegistering) {
        await authApi.register({ name, email, password, role });
        // Automatically login after successful registration
        await login({ email, password });
      } else {
        await login({ email, password });
      }
      navigate(redirectPath, { replace: true });
    } catch (err: any) {
      const serverMessage =
        err.response?.data?.message ||
        err.message ||
        'Authentication failed. Please check your credentials.';
      setError(serverMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const setDemoAccount = (roleChoice: Role, demoEmail: string, demoName: string) => {
    setRole(roleChoice);
    setEmail(demoEmail);
    setPassword('Password123!');
    setName(demoName);
  };

  return (
    <div
      style={{
        display: 'flex',
        minHeight: '100vh',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '1.5rem',
      }}
    >
      <div
        className="glass-card"
        style={{
          width: '100%',
          maxWidth: '460px',
          boxShadow: 'var(--shadow-lg)',
          border: '1px solid var(--border-glow)',
        }}
      >
        {/* Brand Header */}
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div
            className="sidebar-logo-icon"
            style={{ width: '52px', height: '52px', margin: '0 auto 1rem auto' }}
          >
            <Bot size={30} />
          </div>
          <h1 style={{ fontSize: '1.75rem', marginBottom: '0.25rem' }}>SmartFleet</h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>
            {isRegistering ? 'Create system credentials' : 'Autonomous Rover Fleet Management'}
          </p>
        </div>

        {error && <ErrorAlert message={error} onDismiss={() => setError(null)} />}

        <form onSubmit={handleSubmit}>
          {isRegistering && (
            <>
              <div className="form-group">
                <label className="form-label" htmlFor="name">
                  Full Name
                </label>
                <div style={{ position: 'relative' }}>
                  <input
                    id="name"
                    type="text"
                    className="form-input"
                    placeholder="Enter your name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required
                  />
                  <UserIcon
                    size={16}
                    style={{ position: 'absolute', right: '12px', top: '12px', color: 'var(--text-muted)' }}
                  />
                </div>
              </div>

              <div className="form-group">
                <label className="form-label" htmlFor="role">
                  System Role
                </label>
                <div style={{ position: 'relative' }}>
                  <select
                    id="role"
                    className="form-input"
                    value={role}
                    onChange={(e) => setRole(e.target.value as Role)}
                    style={{ appearance: 'none' }}
                  >
                    <option value="Operator">Operator (Mobile/Dispatches)</option>
                    <option value="Technician">Technician (Maintenance)</option>
                    <option value="Supervisor">Supervisor (Approvals/Fleet)</option>
                  </select>
                  <Shield
                    size={16}
                    style={{ position: 'absolute', right: '12px', top: '12px', color: 'var(--text-muted)', pointerEvents: 'none' }}
                  />
                </div>
              </div>
            </>
          )}

          <div className="form-group">
            <label className="form-label" htmlFor="email">
              Email Address
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="email"
                type="email"
                className="form-input"
                placeholder="name@smartfleet.internal"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
              <Mail
                size={16}
                style={{ position: 'absolute', right: '12px', top: '12px', color: 'var(--text-muted)' }}
              />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="password">
              Password
            </label>
            <div style={{ position: 'relative' }}>
              <input
                id="password"
                type="password"
                className="form-input"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
              <KeyRound
                size={16}
                style={{ position: 'absolute', right: '12px', top: '12px', color: 'var(--text-muted)' }}
              />
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: '100%', marginTop: '0.75rem', padding: '0.85rem' }}
            disabled={isLoading}
          >
            {isLoading ? (
              <LoadingSpinner size="sm" text={isRegistering ? 'Registering...' : 'Signing in...'} />
            ) : isRegistering ? (
              'Create Account'
            ) : (
              'Sign In to SmartFleet'
            )}
          </button>
        </form>

        <div style={{ textAlign: 'center', marginTop: '1.25rem' }}>
          <button
            type="button"
            onClick={() => {
              setIsRegistering(!isRegistering);
              setError(null);
            }}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--primary-light)',
              fontSize: '0.85rem',
              cursor: 'pointer',
            }}
          >
            {isRegistering
              ? 'Already registered? Sign In'
              : 'Need an account? Register new role'}
          </button>
        </div>

        {/* Demo Quick Selector */}
        <div style={{ marginTop: '1.75rem', paddingTop: '1.25rem', borderTop: '1px solid var(--border-subtle)' }}>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-faint)', textAlign: 'center', marginBottom: '0.75rem' }}>
            QUICK DEMO ROLES (CLICK TO PREFILL)
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '0.5rem' }}>
            <button
              type="button"
              className="btn btn-secondary"
              style={{ fontSize: '0.75rem', padding: '0.4rem 0.25rem' }}
              onClick={() => setDemoAccount('Operator', 'operator@smartfleet.internal', 'Alex Operator')}
            >
              Operator
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              style={{ fontSize: '0.75rem', padding: '0.4rem 0.25rem' }}
              onClick={() => setDemoAccount('Technician', 'tech@smartfleet.internal', 'Marcus Tech')}
            >
              Technician
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              style={{ fontSize: '0.75rem', padding: '0.4rem 0.25rem' }}
              onClick={() => setDemoAccount('Supervisor', 'supervisor@smartfleet.internal', 'Sarah Connor')}
            >
              Supervisor
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
