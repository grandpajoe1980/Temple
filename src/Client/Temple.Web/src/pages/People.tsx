import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface Person {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  status: string;
  birthDate?: string;
  createdUtc: string;
}

export default function People() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [people, setPeople] = useState<Person[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newPerson, setNewPerson] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    status: 'guest',
    birthDate: ''
  });
  const [creating, setCreating] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');

  useEffect(() => {
    loadPeople();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter]);

  async function loadPeople() {
    setLoading(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');

      let url = '/api/v1/people?page=1&pageSize=50';
      if (statusFilter) {
        url += `&status=${encodeURIComponent(statusFilter)}`;
      }

      const resp = await fetch(url, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!resp.ok) throw new Error('Failed to load people');
      const data = await resp.json();
      setPeople(data.data || []);

    } catch (e) {
      setError(e instanceof Error ? e.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }

  async function createPerson() {
    if (!newPerson.firstName || !newPerson.lastName) return;
    
    setCreating(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const payload: {
        firstName: string;
        lastName: string;
        status: string;
        email?: string;
        phone?: string;
        birthDate?: string;
      } = {
        firstName: newPerson.firstName,
        lastName: newPerson.lastName,
        status: newPerson.status
      };
      
      if (newPerson.email) payload.email = newPerson.email;
      if (newPerson.phone) payload.phone = newPerson.phone;
      if (newPerson.birthDate) payload.birthDate = newPerson.birthDate;
      
      const resp = await fetch('/api/v1/people', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });

      if (!resp.ok) throw new Error('Failed to create person');
      
      setNewPerson({
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        status: 'guest',
        birthDate: ''
      });
      setShowCreateForm(false);
      loadPeople(); // Reload the data
      
    } catch (e) {
      setError(e instanceof Error ? e.message : 'An error occurred');
    } finally {
      setCreating(false);
    }
  }

  async function updatePersonStatus(personId: string, newStatus: string) {
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const resp = await fetch(`/api/v1/people/${personId}/status`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ status: newStatus })
      });

      if (!resp.ok) throw new Error('Failed to update status');
      
      loadPeople(); // Reload the data
      
    } catch (e) {
      setError(e instanceof Error ? e.message : 'An error occurred');
    }
  }

  function formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  function getStatusColor(status: string): string {
    switch (status) {
      case 'member': return '#28a745';
      case 'attender': return '#17a2b8';
      case 'guest': return '#ffc107';
      case 'inactive': return '#6c757d';
      default: return '#6c757d';
    }
  }

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading people...</div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <header style={styles.header}>
        <h1 style={styles.title}>People</h1>
        <div style={styles.headerActions}>
          <button 
            onClick={() => setShowCreateForm(!showCreateForm)} 
            style={styles.createButton}
          >
            {showCreateForm ? 'Cancel' : 'Add Person'}
          </button>
          <button onClick={() => navigate(`/tenant/${slug}`)} style={styles.backButton}>
            ← Back to Dashboard
          </button>
        </div>
      </header>

      {error && <div style={styles.error}>{error}</div>}

      <div style={styles.filters}>
        <label style={styles.filterLabel}>
          Filter by Status:
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            style={styles.filterSelect}
          >
            <option value="">All</option>
            <option value="member">Members</option>
            <option value="attender">Attenders</option>
            <option value="guest">Guests</option>
            <option value="inactive">Inactive</option>
          </select>
        </label>
      </div>

      {showCreateForm && (
        <section style={styles.createSection}>
          <h2 style={styles.sectionTitle}>Add New Person</h2>
          <div style={styles.createForm}>
            <div style={styles.formRow}>
              <input
                type="text"
                placeholder="First Name *"
                value={newPerson.firstName}
                onChange={(e) => setNewPerson(prev => ({ ...prev, firstName: e.target.value }))}
                style={styles.input}
              />
              <input
                type="text"
                placeholder="Last Name *"
                value={newPerson.lastName}
                onChange={(e) => setNewPerson(prev => ({ ...prev, lastName: e.target.value }))}
                style={styles.input}
              />
              <select
                value={newPerson.status}
                onChange={(e) => setNewPerson(prev => ({ ...prev, status: e.target.value }))}
                style={styles.select}
              >
                <option value="guest">Guest</option>
                <option value="attender">Attender</option>
                <option value="member">Member</option>
              </select>
            </div>
            <div style={styles.formRow}>
              <input
                type="email"
                placeholder="Email"
                value={newPerson.email}
                onChange={(e) => setNewPerson(prev => ({ ...prev, email: e.target.value }))}
                style={styles.input}
              />
              <input
                type="tel"
                placeholder="Phone"
                value={newPerson.phone}
                onChange={(e) => setNewPerson(prev => ({ ...prev, phone: e.target.value }))}
                style={styles.input}
              />
              <input
                type="date"
                placeholder="Birth Date"
                value={newPerson.birthDate}
                onChange={(e) => setNewPerson(prev => ({ ...prev, birthDate: e.target.value }))}
                style={styles.input}
              />
            </div>
            <button
              onClick={createPerson}
              disabled={creating || !newPerson.firstName || !newPerson.lastName}
              style={styles.submitButton}
            >
              {creating ? 'Adding...' : 'Add Person'}
            </button>
          </div>
        </section>
      )}

      <section style={styles.peopleSection}>
        <h2 style={styles.sectionTitle}>
          People Directory
          {statusFilter && <span style={styles.filterIndicator}>({statusFilter})</span>}
        </h2>
        {people.length === 0 ? (
          <div style={styles.emptyState}>
            {statusFilter ? `No ${statusFilter}s found.` : 'No people in directory yet.'}
          </div>
        ) : (
          <div style={styles.peopleGrid}>
            {people.map(person => (
              <div key={person.id} style={styles.personCard}>
                <div style={styles.personHeader}>
                  <div style={styles.personName}>
                    {person.firstName} {person.lastName}
                  </div>
                  <span 
                    style={{
                      ...styles.statusBadge,
                      backgroundColor: getStatusColor(person.status)
                    }}
                  >
                    {person.status}
                  </span>
                </div>
                
                <div style={styles.personDetails}>
                  {person.email && (
                    <div style={styles.contactInfo}>
                      <strong>Email:</strong> {person.email}
                    </div>
                  )}
                  {person.phone && (
                    <div style={styles.contactInfo}>
                      <strong>Phone:</strong> {person.phone}
                    </div>
                  )}
                  {person.birthDate && (
                    <div style={styles.contactInfo}>
                      <strong>Birth Date:</strong> {formatDate(person.birthDate)}
                    </div>
                  )}
                  <div style={styles.contactInfo}>
                    <strong>Added:</strong> {formatDate(person.createdUtc)}
                  </div>
                </div>

                <div style={styles.personActions}>
                  <div style={styles.statusActions}>
                    <label style={styles.statusLabel}>Update Status:</label>
                    <select
                      value={person.status}
                      onChange={(e) => updatePersonStatus(person.id, e.target.value)}
                      style={styles.statusSelect}
                    >
                      <option value="guest">Guest</option>
                      <option value="attender">Attender</option>
                      <option value="member">Member</option>
                      <option value="inactive">Inactive</option>
                    </select>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    padding: '1rem',
    maxWidth: '1200px',
    margin: '0 auto'
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '2rem'
  },
  title: {
    margin: '0',
    fontSize: '2rem',
    fontWeight: '700',
    color: '#333'
  },
  headerActions: {
    display: 'flex',
    gap: '1rem'
  },
  createButton: {
    background: '#28a745',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    padding: '0.75rem 1.5rem',
    fontSize: '1rem',
    fontWeight: '500',
    cursor: 'pointer'
  },
  backButton: {
    background: '#f8f9fa',
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '0.5rem 1rem',
    cursor: 'pointer',
    fontSize: '0.9rem',
    color: '#666'
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
    padding: '1rem',
    borderRadius: '8px',
    marginBottom: '1rem'
  },
  filters: {
    background: '#fff',
    borderRadius: '12px',
    padding: '1rem 1.5rem',
    marginBottom: '2rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  filterLabel: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem',
    fontWeight: '500'
  },
  filterSelect: {
    padding: '0.5rem',
    border: '1px solid #ddd',
    borderRadius: '4px',
    fontSize: '1rem',
    background: '#fff'
  },
  createSection: {
    background: '#fff',
    borderRadius: '12px',
    padding: '1.5rem',
    marginBottom: '2rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  sectionTitle: {
    margin: '0 0 1rem 0',
    fontSize: '1.3rem',
    fontWeight: '600',
    color: '#333'
  },
  filterIndicator: {
    fontSize: '1rem',
    fontWeight: '400',
    color: '#666'
  },
  createForm: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem'
  },
  formRow: {
    display: 'flex',
    gap: '1rem',
    flexWrap: 'wrap'
  },
  input: {
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem',
    flex: 1,
    minWidth: '200px'
  },
  select: {
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem',
    background: '#fff',
    cursor: 'pointer',
    minWidth: '150px'
  },
  submitButton: {
    background: '#28a745',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    padding: '0.75rem 1.5rem',
    fontSize: '1rem',
    fontWeight: '500',
    cursor: 'pointer',
    alignSelf: 'flex-start'
  },
  peopleSection: {
    background: '#fff',
    borderRadius: '12px',
    padding: '1.5rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  emptyState: {
    textAlign: 'center',
    padding: '2rem',
    color: '#666'
  },
  peopleGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
    gap: '1rem'
  },
  personCard: {
    border: '1px solid #e9ecef',
    borderRadius: '8px',
    padding: '1.5rem',
    background: '#f8f9fa'
  },
  personHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '1rem'
  },
  personName: {
    fontSize: '1.25rem',
    fontWeight: '600',
    color: '#333'
  },
  statusBadge: {
    color: '#fff',
    padding: '0.25rem 0.75rem',
    borderRadius: '4px',
    fontSize: '0.8rem',
    textTransform: 'uppercase',
    fontWeight: '500'
  },
  personDetails: {
    marginBottom: '1rem'
  },
  contactInfo: {
    fontSize: '0.9rem',
    color: '#666',
    marginBottom: '0.25rem'
  },
  personActions: {
    borderTop: '1px solid #dee2e6',
    paddingTop: '1rem'
  },
  statusActions: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem'
  },
  statusLabel: {
    fontSize: '0.9rem',
    fontWeight: '500',
    color: '#333'
  },
  statusSelect: {
    padding: '0.5rem',
    border: '1px solid #ddd',
    borderRadius: '4px',
    fontSize: '0.9rem',
    background: '#fff',
    cursor: 'pointer'
  }
};
