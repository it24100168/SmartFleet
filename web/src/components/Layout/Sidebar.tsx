import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Send,
  Activity,
  Wrench,
  ShieldCheck,
  Bot,
} from 'lucide-react';

export const Sidebar: React.FC = () => {
  const navItems = [
    { path: '/', label: 'Dashboard', icon: LayoutDashboard },
    { path: '/dispatch', label: 'Dispatch Requests', icon: Send },
    { path: '/telemetry', label: 'Fleet Telemetry', icon: Activity },
    { path: '/maintenance', label: 'Maintenance Queue', icon: Wrench },
    { path: '/approvals', label: 'Approval Center', icon: ShieldCheck },
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
        <div>System: Simulated Ro-1 to Ro-10</div>
        <div>Pipeline: 4 Agents Active</div>
      </div>
    </aside>
  );
};
