import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

interface Religion {
  id: string;
  displayName: string;
}

interface Sect {
  id: string;
  displayName: string;
}

export default function CreateTenant() {
  const [name, setName] = useState('');
  const [religionId, setReligionId] = useState('');
  const [taxonomyId, setTaxonomyId] = useState('');
  const [religions, setReligions] = useState<Religion[]>([]);
  const [sects, setSects] = useState<Sect[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingReligions, setLoadingReligions] = useState(false);
  const [loadingSects, setLoadingSects] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  async function loadReligions() {
    if (religions.length > 0) return; // Already loaded
    
    setLoadingReligions(true);
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const resp = await fetch('/api/v1/taxonomies/religions', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!resp.ok) throw new Error('Failed to load religions');
      const data = await resp.json();
      setReligions(data);
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoadingReligions(false);
    }
  }

  async function loadSects(religionId: string) {
    if (!religionId) {
      setSects([]);
      setTaxonomyId('');
      return;
    }

    setLoadingSects(true);
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const resp = await fetch(`/api/v1/taxonomies/religions/${encodeURIComponent(religionId)}/sects`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!resp.ok) throw new Error('Failed to load sects');
      const data = await resp.json();
      setSects(data);
      // Auto-select first sect if available
      if (data.length > 0) {
        setTaxonomyId(data[0].id);
      }
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoadingSects(false);
    }
  }

  useEffect(() => {
    if (religionId) {
      loadSects(religionId);
    } else {
      setSects([]);
      setTaxonomyId('');
    }
  }, [religionId]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) {
      setError('Tenant name is required');
      return;
    }

    setLoading(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');

      const payload: any = { name: name.trim() };
      // Send taxonomyId if a sect is selected, otherwise send religionId
      if (taxonomyId) {
        payload.taxonomyId = taxonomyId;
      } else if (religionId) {
        payload.religionId = religionId;
      }

      const resp = await fetch('/api/v1/tenants', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });

      if (!resp.ok) {
        const errorText = await resp.text();
        throw new Error(errorText || 'Failed to create tenant');
      }

      const created = await resp.json();
      
      // Navigate back to tenants page with success message
      navigate('/tenants', { 
        state: { 
          message: `Tenant "${created.name}" created successfully!`,
          newTenantId: created.id 
        }
      });
      
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h2 style={styles.title}>Create New Tenant</h2>
        <p style={styles.description}>
          Set up a new community organization in the Temple system.
        </p>
        
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
            <label style={styles.label}>Religion/Faith (Optional)</label>
            <select
              value={religionId}
              onChange={(e) => setReligionId(e.target.value)}
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
                <option value="">Select a sect (optional)</option>
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

          {error && <div style={styles.error}>{error}</div>}

          <div style={styles.buttons}>
            <button
              type="button"
              onClick={() => navigate('/tenants')}
              style={styles.cancelButton}
              disabled={loading}
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading || !name.trim()}
              style={styles.submitButton}
            >
              {loading ? 'Creating...' : 'Create Tenant'}
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
    maxWidth: '500px'
  },
  title: {
    margin: '0 0 0.5rem 0',
    fontSize: '1.5rem',
    fontWeight: '600',
    color: '#1a1a1a'
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
  error: {
    background: '#fee',
    color: '#c33',
    padding: '0.75rem',
    borderRadius: '6px',
    fontSize: '0.9rem'
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
  }
};
