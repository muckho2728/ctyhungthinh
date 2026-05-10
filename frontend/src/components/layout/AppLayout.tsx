import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import Topbar from './Topbar';
import { useSignalR } from '../../hooks/useSignalR';

export default function AppLayout() {
  // Initialize SignalR connection
  useSignalR();

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: 'var(--bg-primary)' }}>
      <Sidebar />

      {/* Main content shifted right of sidebar */}
      <div style={{
        flex: 1,
        marginLeft: 220,
        display: 'flex',
        flexDirection: 'column',
        minHeight: '100vh',
        transition: 'margin-left 0.3s ease',
      }}>
        <Topbar />
        <main style={{
          flex: 1,
          padding: '24px',
          overflow: 'auto',
          maxWidth: 1600,
          width: '100%',
        }}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
