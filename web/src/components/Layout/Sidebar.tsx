import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import {
  LayoutDashboard,
  Send,
  Activity,
  Wrench,
  ShieldCheck,
  Bot,
} from 'lucide-react';

export const Sidebar: React.FC = () => {
  const { user } = useAuth();
  const navItems = [
    { path: '/', label: 'Fleet Simulation', icon: LayoutDashboard },
    { path: '/dispatch', label: 'Dispatch Requests', icon: Send },
    { path: '/telemetry', label: 'Fleet Telemetry', icon: Activity },
    { path: '/maintenance', label: 'Maintenance Queue', icon: Wrench },
    ...(user?.role === 'Supervisor' ? [{ path: '/approvals', label: 'Approval Center', icon: ShieldCheck }] : []),
  ];

  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <div className="sidebar-logo-icon">
          <Bot size={22} />
        </div>
        <div>
          <div className="sidebar-logo-text">SmartFleet</div>
          <div style={{ fontSize: '0.68rem', color: 'var(--text-muted)', letterSpacing: '0.08em', textTransform: 'uppercase' }}>
            Mission Control
          </div>
        </div>
      </div>

      <nav className="sidebar-nav">
        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.path}
              to={item.path}
              end={item.path === '/'}
              className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
            >
              <Icon size={18} />
              <span>{item.label}</span>
            </NavLink>
          );
        })}
      </nav>

      <div style={{ padding: '1rem', borderTop: '1px solid var(--border-subtle)', fontSize: '0.75rem', color: 'var(--text-faint)' }}>
        <div>Warehouse A � Simulated robots</div>
        <div>4 agents � Auditable workflow</div>
      </div>
    </aside>
  );
};
