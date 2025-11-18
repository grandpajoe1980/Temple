import { Routes, Route, Link, Navigate } from 'react-router-dom';
import Tenants from './Tenants';
import Home from './Home';
import Login from './Login';
import CreateTenant from './CreateTenant';
import TenantDashboard from './TenantDashboard';
import TenantSettings from './TenantSettings';
import Donations from './Donations';
import Schedule from './Schedule';
import People from './People';

function isAuthenticated() {
  return !!localStorage.getItem('auth_token');
}

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  return isAuthenticated() ? <>{children}</> : <Navigate to="/login" replace />;
}

export default function App() {
  const handleLogout = () => {
    localStorage.removeItem('auth_token');
    window.location.reload();
  };

  return (
    <div style={{ fontFamily: 'sans-serif', padding: 16 }}>
      <h1>Temple UAT</h1>
      <nav style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
        {isAuthenticated() ? (
          <>
            <Link to="/">Home</Link>
            <Link to="/tenants">Tenants</Link>
            <button onClick={handleLogout} style={{ padding: '4px 8px', cursor: 'pointer' }}>Logout</button>
            <a href="/swagger" target="_blank" rel="noreferrer">Swagger</a>
          </>
        ) : (
          <Link to="/login">Login</Link>
        )}
      </nav>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/" element={<ProtectedRoute><Home /></ProtectedRoute>} />
        <Route path="/tenants" element={<ProtectedRoute><Tenants /></ProtectedRoute>} />
        <Route path="/tenants/create" element={<ProtectedRoute><CreateTenant /></ProtectedRoute>} />
        <Route path="/tenant/:slug" element={<ProtectedRoute><TenantDashboard /></ProtectedRoute>} />
        <Route path="/tenant/:slug/settings" element={<ProtectedRoute><TenantSettings /></ProtectedRoute>} />
        <Route path="/tenant/:slug/donations" element={<ProtectedRoute><Donations /></ProtectedRoute>} />
        <Route path="/tenant/:slug/schedule" element={<ProtectedRoute><Schedule /></ProtectedRoute>} />
        <Route path="/tenant/:slug/people" element={<ProtectedRoute><People /></ProtectedRoute>} />
      </Routes>
    </div>
  );
}
