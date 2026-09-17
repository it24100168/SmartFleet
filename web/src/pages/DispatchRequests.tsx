import React from 'react';
import { EmptyState } from '../components/Common/EmptyState';
import { Send } from 'lucide-react';

export const DispatchRequests: React.FC = () => {
  return (
    <div>
      <div style={{ marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.75rem', marginBottom: '0.25rem' }}>Dispatch Requests</h1>
        <p style={{ color: 'var(--text-muted)' }}>
          Monitor and manage cargo transport orders submitted by Factory Operators.
        </p>
      </div>

      <EmptyState
        icon={<Send size={32} color="var(--primary-light)" />}
        title="No Active Dispatch Requests"
        description="Factory Operators will submit cargo transport requests via the mobile app. The Mission Planner Agent will schedule rovers once orders arrive."
      />
    </div>
  );
};
