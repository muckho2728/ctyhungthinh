import { useEffect, useState } from 'react';
import { Bell, RefreshCw, User, LogOut } from 'lucide-react';
import { useAuthStore, useMarketStore } from '../../store';
import { useNavigate } from 'react-router-dom';

export default function Topbar() {
  const { livePrice, livePercentChange, liveChange, lastUpdated, priceDirection } = useMarketStore();
  const { user, logout, isAuthenticated } = useAuthStore();
  const navigate = useNavigate();
  const [flashClass, setFlashClass] = useState('');

  useEffect(() => {
    if (!priceDirection) return;
    setFlashClass(priceDirection === 'up' ? 'price-flash-up' : 'price-flash-down');
    const t = setTimeout(() => setFlashClass(''), 1200);
    return () => clearTimeout(t);
  }, [livePrice]);

  const isPositive = (liveChange ?? 0) >= 0;
  const changeColor = isPositive ? 'var(--accent-green)' : 'var(--accent-red)';

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header style={{
      height: 64,
      background: 'var(--bg-secondary)',
      borderBottom: '1px solid var(--border)',
      display: 'flex',
      alignItems: 'center',
      padding: '0 24px',
      gap: 16,
      position: 'sticky',
      top: 0,
      zIndex: 90,
    }}>
      {/* Live Price Display */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, flex: 1 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <div className="live-dot" />
          <span style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600 }}>LIVE</span>
        </div>

        <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
          <span style={{
            fontSize: 22,
            fontWeight: 700,
            color: 'var(--text-primary)',
            fontVariantNumeric: 'tabular-nums',
          }}
            className={flashClass}
          >
            {livePrice != null
              ? `$${livePrice.toFixed(2)}`
              : <span className="skeleton" style={{ width: 90, height: 22, display: 'inline-block' }} />
            }
          </span>

          {liveChange != null && (
            <span style={{ fontSize: 13, fontWeight: 600, color: changeColor }}>
              {isPositive ? '+' : ''}{liveChange?.toFixed(2)}
              {' '}({isPositive ? '+' : ''}{livePercentChange?.toFixed(2)}%)
            </span>
          )}
        </div>

        <div style={{
          padding: '2px 8px',
          borderRadius: 6,
          background: 'rgba(59,130,246,0.1)',
          border: '1px solid rgba(59,130,246,0.2)',
          fontSize: 11,
          fontWeight: 700,
          color: 'var(--accent-blue)',
        }}>
          KC1 • Coffee Futures
        </div>

        {lastUpdated && (
          <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>
            Updated {lastUpdated.toLocaleTimeString()}
          </span>
        )}
      </div>

      {/* Right Side */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <button className="btn-ghost" style={{ padding: '6px 10px' }} title="Refresh">
          <RefreshCw size={14} />
        </button>

        <button className="btn-ghost" style={{ padding: '6px 10px', position: 'relative' }}>
          <Bell size={14} />
          <span style={{
            position: 'absolute',
            top: 4, right: 4,
            width: 6, height: 6,
            borderRadius: '50%',
            background: 'var(--accent-red)',
          }} />
        </button>

        {isAuthenticated ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginLeft: 8 }}>
            <div style={{
              width: 32, height: 32,
              borderRadius: '50%',
              background: 'linear-gradient(135deg, #6366f1, #3b82f6)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 12, fontWeight: 700, color: 'white',
            }}>
              {user?.fullName?.[0]?.toUpperCase() || 'U'}
            </div>
            <div style={{ fontSize: 12 }}>
              <div style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{user?.fullName}</div>
              <div style={{ color: 'var(--text-muted)' }}>{user?.role}</div>
            </div>
            <button
              className="btn-ghost"
              style={{ padding: '6px 10px' }}
              onClick={handleLogout}
              title="Logout"
            >
              <LogOut size={14} />
            </button>
          </div>
        ) : (
          <button
            className="btn-primary"
            onClick={() => navigate('/login')}
            style={{ padding: '6px 14px', fontSize: 13 }}
          >
            <User size={14} />
            Sign In
          </button>
        )}
      </div>
    </header>
  );
}
