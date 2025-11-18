import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface Religion {
  id: string;
  displayName: string;
}

interface Sect {
  id: string;
  displayName: string;
}

interface TenantSettings {
  id: string;
  name: string;
  slug: string;
  status: string;
  taxonomyId?: string;
  terminology?: Record<string, string>;
}

export default function TenantSettings() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [tenant, setTenant] = useState<TenantSettings | null>(null);
  const [name, setName] = useState('');
  const [status, setStatus] = useState('active');
  const [religionId, setReligionId] = useState('');
  const [taxonomyId, setTaxonomyId] = useState('');
  const [religions, setReligions] = useState<Religion[]>([]);
  const [sects, setSects] = useState<Sect[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [loadingReligions, setLoadingReligions] = useState(false);
  const [loadingSects, setLoadingSects] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!slug) {
      navigate('/tenants');
      return;
    }
    loadTenantSettings();
  }, [slug]);

  useEffect(() => {
    if (religionId) {
      loadSects(religionId);
    } else {
      setSects([]);
    }
  }, [religionId]);

  async function loadTenantSettings() {
    setLoading(true);
    setError(null);

    try {
      const token = localStorage.getItem('auth_token');
      if (!token) {
        navigate(`/login?tenant=${slug}`);
        return;
      }

      // First get basic tenant info by slug
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

      // Then get full settings (requires OrgManageSettings capability)
      try {
        const settingsResp = await fetch(`/api/v1/tenants/${tenantData.id}/settings`, {
          headers: { 'Authorization': `Bearer ${token}` }
        });

        if (settingsResp.ok) {
          const settingsData = await settingsResp.json();
          setTenant(settingsData);
          setName(settingsData.name);
          setStatus(settingsData.status || 'active');
          setTaxonomyId(settingsData.taxonomyId || '');
          
          // Extract religion ID from taxonomy if needed
          if (settingsData.taxonomyId) {
            // We'll need to load religions and check which one is the parent
            await loadReligionsAndDetectParent(settingsData.taxonomyId);
          }
        } else {
          // Fallback to basic tenant data if user doesn't have settings access
          setTenant(tenantData);
          setName(tenantData.name);
          setStatus(tenantData.status || 'active');
          setTaxonomyId(tenantData.taxonomyId || '');
        }
      } catch (e) {
        // Use basic tenant data as fallback
        setTenant(tenantData);
        setName(tenantData.name);
        setStatus(tenantData.status || 'active');
        setTaxonomyId(tenantData.taxonomyId || '');
      }

    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  async function loadReligionsAndDetectParent(taxonomyId: string) {
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) return;

      // Load religions
      const religionsResp = await fetch('/api/v1/taxonomies/religions', {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (religionsResp.ok) {
        const religionsData = await religionsResp.json();
        setReligions(religionsData);

        // Try to get taxonomy node info to find parent
        const taxonomyResp = await fetch(`/api/v1/taxonomies/${encodeURIComponent(taxonomyId)}`, {
          headers: { 'Authorization': `Bearer ${token}` }
        });

        if (taxonomyResp.ok) {
          const taxonomyData = await taxonomyResp.json();
          if (taxonomyData.type === 'sect') {
            // Need to find parent from the sects list or by checking each religion
            for (const religion of religionsData) {
              const sectsResp = await fetch(`/api/v1/taxonomies/religions/${encodeURIComponent(religion.id)}/sects`, {
                headers: { 'Authorization': `Bearer ${token}` }
              });
              if (sectsResp.ok) {
                const sectsData = await sectsResp.json();
                if (sectsData.some((s: Sect) => s.id === taxonomyId)) {
                  setReligionId(religion.id);
                  setSects(sectsData);
                  break;
                }
              }
            }
          } else if (taxonomyData.type === 'religion') {
            setReligionId(taxonomyId);
          }
        }
      }
    } catch (e) {
      console.error('Failed to detect parent religion:', e);
    }
  }

  async function loadReligions() {
    if (religions.length > 0) return;

    setLoadingReligions(true);
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) return;

      const resp = await fetch('/api/v1/taxonomies/religions', {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (resp.ok) {
        const data = await resp.json();
        setReligions(data);
      }
    } catch (e: any) {
      console.error('Failed to load religions:', e);
    } finally {
      setLoadingReligions(false);
    }
  }

  async function loadSects(religionId: string) {
    if (!religionId) {
      setSects([]);
      return;
    }

    setLoadingSects(true);
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) return;

      const resp = await fetch(`/api/v1/taxonomies/religions/${encodeURIComponent(religionId)}/sects`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (resp.ok) {
        const data = await resp.json();
        setSects(data);
      }
    } catch (e: any) {
      console.error('Failed to load sects:', e);
    } finally {
      setLoadingSects(false);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!tenant) return;
    if (!name.trim()) {
      setError('Tenant name is required');
      return;
    }

    setSaving(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');

      const payload: any = {
        name: name.trim(),
        status: status
      };

      // Send taxonomyId if selected
      if (taxonomyId) {
        payload.taxonomyId = taxonomyId;
      } else if (religionId) {
        payload.taxonomyId = religionId;
      }

      const resp = await fetch(`/api/v1/tenants/${tenant.id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });

      if (!resp.ok) {
        const errorText = await resp.text();
        throw new Error(errorText || 'Failed to update tenant');
      }

      const updated = await resp.json();
      setSuccessMessage('Tenant settings updated successfully!');
      
      // Update local state
      if (tenant) {
        setTenant({ ...tenant, name: updated.name, status: updated.status, taxonomyId: updated.taxonomyId });
      }

    } catch (e: any) {
      setError(e.message);
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading tenant settings...</div>
      </div>
    );
  }

  if (error && !tenant) {
    return (
      <div style={styles.container}>
        <div style={styles.error}>
          {error}
          <button onClick={() => navigate('/tenants')} style={styles.linkButton}>
            Back to Tenants
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
          <button onClick={() => navigate('/tenants')} style={styles.linkButton}>
            Back to Tenants
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <div style={styles.header}>
          <h2 style={styles.title}>Tenant Settings</h2>
          <button onClick={() => navigate(`/tenant/${slug}`)} style={styles.backButton}>
            ← Back to Dashboard
          </button>
        </div>
        
        <p style={styles.description}>
          Customize your tenant settings including name, status, and religious affiliation.
        </p>

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

        {error && (
          <div style={styles.error}>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} style={styles.form}>
          <div style={styles.field}>
            <label style={styles.label}>Organization Name *</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g., First Baptist Church, Temple Shalom, etc."
              required
              style={styles.input}
            />
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Slug (read-only)</label>
            <input
              type="text"
              value={tenant.slug}
              disabled
              style={{ ...styles.input, background: '#f5f5f5', color: '#666' }}
            />
            <p style={styles.hint}>The slug cannot be changed after creation.</p>
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Status *</label>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              required
              style={styles.select}
            >
              <option value="active">Active</option>
              <option value="suspended">Suspended</option>
              <option value="archived">Archived</option>
            </select>
            <p style={styles.hint}>
              Active tenants are fully functional. Suspended tenants have limited access. Archived tenants are read-only.
            </p>
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Religion/Faith (Optional)</label>
            <select
              value={religionId}
              onChange={(e) => {
                setReligionId(e.target.value);
                setTaxonomyId('');
              }}
              onFocus={loadReligions}
              disabled={loadingReligions}
              style={styles.select}
            >
              <option value="">Select a religion (optional)</option>
              {religions.map(religion => (
                <option key={religion.id} value={religion.id}>
                  {religion.displayName}
                </option>
              ))}
            </select>
            {loadingReligions && <p style={styles.hint}>Loading religions...</p>}
          </div>

          {religionId && (
            <div style={styles.field}>
              <label style={styles.label}>Denomination/Sect (Optional)</label>
              <select
                value={taxonomyId}
                onChange={(e) => setTaxonomyId(e.target.value)}
                disabled={loadingSects}
                style={styles.select}
              >
                <option value="">Use religion default</option>
                {sects.map(sect => (
                  <option key={sect.id} value={sect.id}>
                    {sect.displayName}
                  </option>
                ))}
              </select>
              {loadingSects && <p style={styles.hint}>Loading sects...</p>}
              {!loadingSects && sects.length === 0 && (
                <p style={styles.hint}>No specific denominations available for this religion.</p>
              )}
            </div>
          )}

          <div style={styles.buttons}>
            <button
              type="button"
              onClick={() => navigate(`/tenant/${slug}`)}
              style={styles.cancelButton}
              disabled={saving}
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving || !name.trim()}
              style={styles.submitButton}
            >
              {saving ? 'Saving...' : 'Save Settings'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'flex-start',
    minHeight: '60vh',
    padding: '2rem'
  },
  card: {
    background: '#fff',
    borderRadius: '12px',
    padding: '2rem',
    boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
    width: '100%',
    maxWidth: '600px'
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '0.5rem'
  },
  title: {
    margin: '0',
    fontSize: '1.5rem',
    fontWeight: '600',
    color: '#1a1a1a'
  },
  backButton: {
    background: '#f8f9fa',
    border: '1px solid #ddd',
    borderRadius: '6px',
    padding: '0.5rem 1rem',
    cursor: 'pointer',
    fontSize: '0.9rem',
    color: '#666',
    transition: 'all 0.2s'
  },
  description: {
    margin: '0 0 2rem 0',
    color: '#666',
    lineHeight: '1.5'
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1.5rem'
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem'
  },
  label: {
    fontWeight: '500',
    fontSize: '0.9rem',
    color: '#333'
  },
  input: {
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem',
    outline: 'none',
    transition: 'border-color 0.2s',
  },
  select: {
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem',
    outline: 'none',
    background: '#fff',
    cursor: 'pointer'
  },
  hint: {
    fontSize: '0.8rem',
    color: '#666',
    margin: '0'
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
    background: '#fee',
    color: '#c33',
    padding: '0.75rem',
    borderRadius: '6px',
    fontSize: '0.9rem',
    marginBottom: '1rem'
  },
  buttons: {
    display: 'flex',
    gap: '1rem',
    marginTop: '1rem'
  },
  cancelButton: {
    flex: '1',
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    background: '#fff',
    color: '#666',
    fontSize: '1rem',
    cursor: 'pointer',
    transition: 'all 0.2s'
  },
  submitButton: {
    flex: '2',
    padding: '0.75rem',
    border: 'none',
    borderRadius: '6px',
    background: '#2b59ff',
    color: '#fff',
    fontSize: '1rem',
    fontWeight: '500',
    cursor: 'pointer',
    transition: 'background-color 0.2s',
    opacity: 1
  },
  loading: {
    textAlign: 'center',
    padding: '3rem',
    fontSize: '1.1rem',
    color: '#666'
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
