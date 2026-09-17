import React from 'react';

interface LoadingSpinnerProps {
  text?: string;
  size?: 'sm' | 'md' | 'lg';
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ text, size = 'md' }) => {
  const dimension = size === 'sm' ? 18 : size === 'lg' ? 36 : 24;

  return (
    <div style={{ display: 'inline-flex', flexDirection: 'column', alignItems: 'center', gap: '0.75rem' }}>
      <div
        className="spinner"
        style={{
          width: dimension,
          height: dimension,
          borderWidth: size === 'sm' ? '2px' : '3px',
        }}
      />
      {text && <span style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>{text}</span>}
    </div>
  );
};
