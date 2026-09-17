import React from 'react';
import { useAuth } from '../../context/AuthContext';
import { LogOut, User as UserIcon } from 'lucide-react';

export const Navbar: React.FC = () => {
  const { user, logout } = useAuth();

  const getRoleBadgeClass = (role?: string) => {
    switch (role) {
      case 'Supervisor':
        return 'badge-supervisor';
      case 'Technician':
        return 'badge-technician';
      default:
        return 'badge-operator';
    }
  };

  return (
    <header className="top-navbar">
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
        <h2 style={{ fontSize: '1.25rem', fontWeight: 600 }}>Warehouse Fleet Management</h2>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '1.25rem' }}>
        {user && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '50%',
                background: 'rgba(255, 255, 255, 0.08)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--primary-light)',
              }}
            >
              <UserIcon size={18} />
            </div>
            <div style={{ display: 'flex', flexDirection: 'column' }}>
              <span style={{ fontSize: '0.9rem', fontWeight: 600 }}>{user.name}</span>
              <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{user.email}</span>
            </div>
            <span className={`badge ${getRoleBadgeClass(user.role)}`}>
              {user.role}
            </span>
          </div>
        )}

        <button
          className="btn btn-secondary"
          onClick={logout}
          style={{ padding: '0.5rem 0.85rem', fontSize: '0.85rem' }}
          title="Sign out"
        >
          <LogOut size={16} />
          <span>Sign Out</span>
        </button>
      </div>
    </header>
  );
};
