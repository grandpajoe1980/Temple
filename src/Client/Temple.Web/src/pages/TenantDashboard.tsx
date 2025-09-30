import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface Tenant {
  id: string;
  name: string;
  slug: string;
  status?: string;
  taxonomyId?: string;
}

interface DashboardStats {
  totalMembers: number;
  upcomingEvents: number;
  recentDonations: number;
  activeChannels: number;
}

export default function TenantDashboard() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [tenant, setTenant] = useState<Tenant | null>(null);
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!slug) {
      navigate('/');
      return;
    }
    loadTenantData();
  }, [slug]);

  async function loadTenantData() {
    setLoading(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) {
        navigate(`/login?tenant=${slug}`);
        return;
      }

      // Load tenant info
      const tenantResp = await fetch(`/api/v1/tenants/by-slug/${encodeURIComponent(slug!)}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (tenantResp.status === 404) {
        setError('Tenant not found');
        return;
      }
      
      if (!tenantResp.ok) {
        throw new Error('Failed to load tenant information');
      }
      
      const tenantData = await tenantResp.json();
      setTenant(tenantData);

      // For now, set some mock stats since the API endpoints might not exist yet
      setStats({
        totalMembers: 0,
        upcomingEvents: 0,
        recentDonations: 0,
        activeChannels: 0
      });

    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  function handleBackToSearch() {
    navigate('/');
  }

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading tenant dashboard...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div style={styles.container}>
        <div style={styles.error}>
          {error}
          <button onClick={handleBackToSearch} style={styles.linkButton}>
            Back to Search
          </button>
        </div>
      </div>
    );
  }

  if (!tenant) {
    return (
      <div style={styles.container}>
        <div style={styles.error}>
          Tenant not found
          <button onClick={handleBackToSearch} style={styles.linkButton}>
            Back to Search
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <header style={styles.header}>
        <div style={styles.tenantInfo}>
          <h1 style={styles.tenantName}>{tenant.name}</h1>
          <span style={styles.tenantSlug}>/{tenant.slug}</span>
          {tenant.status && <span style={styles.status}>{tenant.status}</span>}
        </div>
        <button onClick={handleBackToSearch} style={styles.backButton}>
          ← Back to Search
        </button>
      </header>

      <div style={styles.dashboard}>
        <section style={styles.welcomeSection}>
          <h2 style={styles.sectionTitle}>Welcome to {tenant.name}</h2>
          <p style={styles.welcomeText}>
            This is your community dashboard. Here you can access all the features and tools to manage your organization.
          </p>
        </section>

        {stats && (
          <section style={styles.statsSection}>
            <h3 style={styles.sectionTitle}>Quick Stats</h3>
            <div style={styles.statsGrid}>
              <div style={styles.statCard}>
                <div style={styles.statNumber}>{stats.totalMembers}</div>
                <div style={styles.statLabel}>Total Members</div>
              </div>
              <div style={styles.statCard}>
                <div style={styles.statNumber}>{stats.upcomingEvents}</div>
                <div style={styles.statLabel}>Upcoming Events</div>
              </div>
              <div style={styles.statCard}>
                <div style={styles.statNumber}>{stats.recentDonations}</div>
                <div style={styles.statLabel}>Recent Donations</div>
              </div>
              <div style={styles.statCard}>
                <div style={styles.statNumber}>{stats.activeChannels}</div>
                <div style={styles.statLabel}>Active Channels</div>
              </div>
            </div>
          </section>
        )}

        <section style={styles.actionsSection}>
          <h3 style={styles.sectionTitle}>Quick Actions</h3>
          <div style={styles.actionGrid}>
            <button style={styles.actionCard} onClick={() => navigate(`/tenant/${slug}/schedule`)}>
              <div style={styles.actionIcon}>📅</div>
              <div style={styles.actionLabel}>Manage Schedule</div>
            </button>
            <button style={styles.actionCard} onClick={() => navigate(`/tenant/${slug}/people`)}>
              <div style={styles.actionIcon}>👥</div>
              <div style={styles.actionLabel}>Manage People</div>
            </button>
            <button style={styles.actionCard} onClick={() => alert('Content feature coming soon!')}>
              <div style={styles.actionIcon}>📚</div>
              <div style={styles.actionLabel}>Content Library</div>
            </button>
            <button style={styles.actionCard} onClick={() => alert('Chat feature coming soon!')}>
              <div style={styles.actionIcon}>💬</div>
              <div style={styles.actionLabel}>Chat Channels</div>
            </button>
            <button style={styles.actionCard} onClick={() => navigate(`/tenant/${slug}/donations`)}>
              <div style={styles.actionIcon}>💝</div>
              <div style={styles.actionLabel}>Donations</div>
            </button>
            <button style={styles.actionCard} onClick={() => navigate(`/tenant/${slug}/settings`)}>
              <div style={styles.actionIcon}>⚙️</div>
              <div style={styles.actionLabel}>Settings</div>
            </button>
          </div>
        </section>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    minHeight: '100vh',
    background: 'linear-gradient(135deg, #f5f7fa, #e4ecf5)',
    padding: '1rem'
  },
  header: {
    background: '#fff',
    borderRadius: '12px',
    padding: '1.5rem 2rem',
    marginBottom: '2rem',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  tenantInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: '1rem'
  },
  tenantName: {
    margin: '0',
    fontSize: '1.8rem',
    fontWeight: '700',
    background: 'linear-gradient(90deg, #2b59ff, #bb2bff)',
    WebkitBackgroundClip: 'text',
    color: 'transparent'
  },
  tenantSlug: {
    color: '#666',
    fontSize: '1rem',
    fontFamily: 'monospace'
  },
  status: {
    background: '#28a745',
    color: '#fff',
    padding: '0.3rem 0.8rem',
    borderRadius: '20px',
    fontSize: '0.8rem',
    textTransform: 'uppercase',
    fontWeight: '600'
  },
  backButton: {
    background: '#f8f9fa',
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '0.5rem 1rem',
    cursor: 'pointer',
    fontSize: '0.9rem',
    color: '#666',
    transition: 'all 0.2s'
  },
  dashboard: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2rem'
  },
  welcomeSection: {
    background: '#fff',
    borderRadius: '12px',
    padding: '2rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  sectionTitle: {
    margin: '0 0 1rem 0',
    fontSize: '1.3rem',
    fontWeight: '600',
    color: '#333'
  },
  welcomeText: {
    margin: '0',
    color: '#666',
    lineHeight: '1.6',
    fontSize: '1rem'
  },
  statsSection: {
    background: '#fff',
    borderRadius: '12px',
    padding: '2rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '1rem'
  },
  statCard: {
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    borderRadius: '8px',
    padding: '1.5rem',
    textAlign: 'center',
    color: '#fff'
  },
  statNumber: {
    fontSize: '2.5rem',
    fontWeight: '700',
    marginBottom: '0.5rem'
  },
  statLabel: {
    fontSize: '0.9rem',
    opacity: 0.9
  },
  actionsSection: {
    background: '#fff',
    borderRadius: '12px',
    padding: '2rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  actionGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
    gap: '1rem'
  },
  actionCard: {
    background: '#f8f9fa',
    border: '1px solid #e9ecef',
    borderRadius: '8px',
    padding: '1.5rem 1rem',
    cursor: 'pointer',
    textAlign: 'center',
    transition: 'all 0.2s'
  },
  actionIcon: {
    fontSize: '2rem',
    marginBottom: '0.5rem',
    display: 'block'
  },
  actionLabel: {
    fontSize: '0.9rem',
    fontWeight: '500',
    color: '#333'
  },
  loading: {
    textAlign: 'center',
    padding: '3rem',
    fontSize: '1.1rem',
    color: '#666'
  },
  error: {
    background: '#f8d7da',
    color: '#721c24',
    padding: '1.5rem',
    borderRadius: '8px',
    textAlign: 'center'
  },
  linkButton: {
    background: 'none',
    border: 'none',
    color: '#2b59ff',
    textDecoration: 'underline',
    cursor: 'pointer',
    fontSize: 'inherit',
    marginLeft: '0.5rem'
  }
};
