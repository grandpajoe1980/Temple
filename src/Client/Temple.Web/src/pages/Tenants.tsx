import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';

interface Tenant {
  id: string;
  name: string;
  slug: string;
  status?: string;
}

export default function Tenants() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    // Check for success message from tenant creation
    if (location.state?.message) {
      setSuccessMessage(location.state.message);
      // Clear the state to prevent showing the message again if user navigates back
      window.history.replaceState({}, document.title);
    }
    
    loadTenants();
  }, [location.state]);

  async function loadTenants() {
    setLoading(true);
    setError(null);
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const resp = await fetch('/api/v1/tenants', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!resp.ok) throw new Error('Failed to load tenants');
      const data = await resp.json();
      setTenants(Array.isArray(data) ? data : []);
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h2 style={styles.title}>Tenants</h2>
        <button 
          onClick={() => navigate('/tenants/create')}
          style={styles.createButton}
        >
          Create New Tenant
        </button>
      </div>
      
      {successMessage && (
        <div style={styles.success}>
          {successMessage}
          <button 
            onClick={() => setSuccessMessage(null)}
            style={styles.dismissButton}
          >
            ×
          </button>
        </div>
      )}
      
      {error && <p style={styles.error}>{error}</p>}
      
      {loading ? (
        <p style={styles.loading}>Loading tenants...</p>
      ) : (
        <div style={styles.tenantsList}>
          {tenants.length === 0 ? (
            <p style={styles.emptyState}>
              No tenants found. <button onClick={() => navigate('/tenants/create')} style={styles.linkButton}>Create your first tenant</button>
            </p>
          ) : (
            <ul style={styles.list}>
              {tenants.map(t => (
                <li key={t.id} style={styles.listItem}>
                  <div style={styles.tenantInfo}>
                    <div style={styles.tenantDetails}>
                      <strong>{t.name}</strong>
                      <span style={styles.slug}>/{t.slug}</span>
                      {t.status && <span style={styles.status}>{t.status}</span>}
                    </div>
                    <button 
                      onClick={() => navigate(`/tenant/${t.slug}`)}
                      style={styles.enterButton}
                    >
                      Enter
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '1rem'
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '1.5rem'
  },
  title: {
    margin: '0',
    fontSize: '1.5rem',
    fontWeight: '600'
  },
  createButton: {
    padding: '0.75rem 1.5rem',
    background: '#2b59ff',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    fontSize: '1rem',
    fontWeight: '500',
    cursor: 'pointer',
    transition: 'background-color 0.2s'
  },
  success: {
    background: '#d4edda',
    color: '#155724',
    padding: '0.75rem 1rem',
    borderRadius: '6px',
    marginBottom: '1rem',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  dismissButton: {
    background: 'none',
    border: 'none',
    color: '#155724',
    fontSize: '1.2rem',
    cursor: 'pointer',
    padding: '0',
    width: '20px',
    height: '20px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center'
  },
  error: {
    color: '#dc3545',
    background: '#f8d7da',
    padding: '0.75rem',
    borderRadius: '6px',
    marginBottom: '1rem'
  },
  loading: {
    color: '#666',
    fontStyle: 'italic'
  },
  tenantsList: {
    marginTop: '1rem'
  },
  emptyState: {
    color: '#666',
    textAlign: 'center',
    padding: '2rem'
  },
  linkButton: {
    background: 'none',
    border: 'none',
    color: '#2b59ff',
    textDecoration: 'underline',
    cursor: 'pointer',
    fontSize: 'inherit'
  },
  list: {
    listStyle: 'none',
    padding: '0',
    margin: '0'
  },
  listItem: {
    background: '#f8f9fa',
    padding: '1rem',
    borderRadius: '6px',
    marginBottom: '0.5rem',
    border: '1px solid #e9ecef'
  },
  tenantInfo: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    width: '100%'
  },
  tenantDetails: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem'
  },
  enterButton: {
    background: '#2b59ff',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    padding: '0.5rem 1rem',
    fontSize: '0.9rem',
    fontWeight: '500',
    cursor: 'pointer',
    transition: 'background-color 0.2s'
  },
  slug: {
    color: '#666',
    fontSize: '0.9rem',
    fontFamily: 'monospace'
  },
  status: {
    background: '#28a745',
    color: '#fff',
    padding: '0.2rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.8rem',
    textTransform: 'uppercase'
  }
};
