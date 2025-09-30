import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface Donation {
  id: string;
  amountCents: number;
  currency: string;
  recurring: boolean;
  status: string;
  createdUtc: string;
  provider?: string;
}

interface DonationSummary {
  total: number;
  sumCents: number;
  recurringCount: number;
}

export default function Donations() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [donations, setDonations] = useState<Donation[]>([]);
  const [summary, setSummary] = useState<DonationSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newDonation, setNewDonation] = useState({ amount: '', recurring: false });
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    setLoading(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');

      // Load donations
      const donationsResp = await fetch('/api/v1/donations?page=1&pageSize=20', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!donationsResp.ok) throw new Error('Failed to load donations');
      const donationsData = await donationsResp.json();
      setDonations(donationsData.data || []);

      // Load summary
      const summaryResp = await fetch('/api/v1/donations/summary', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!summaryResp.ok) throw new Error('Failed to load summary');
      const summaryData = await summaryResp.json();
      setSummary(summaryData);

    } catch (e) {
      setError(e instanceof Error ? e.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }

  async function createDonation() {
    if (!newDonation.amount) return;
    
    setCreating(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');

      const amountCents = Math.round(parseFloat(newDonation.amount) * 100);
      
      const resp = await fetch('/api/v1/donations', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          amountCents,
          currency: 'usd',
          recurring: newDonation.recurring
        })
      });

      if (!resp.ok) throw new Error('Failed to create donation');
      
      setNewDonation({ amount: '', recurring: false });
      loadData(); // Reload the data
      
    } catch (e) {
      setError(e instanceof Error ? e.message : 'An error occurred');
    } finally {
      setCreating(false);
    }
  }

  function formatCurrency(cents: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(cents / 100);
  }

  function formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading donations...</div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <header style={styles.header}>
        <h1 style={styles.title}>Donations</h1>
        <button onClick={() => navigate(`/tenant/${slug}`)} style={styles.backButton}>
          ← Back to Dashboard
        </button>
      </header>

      {error && <div style={styles.error}>{error}</div>}

      {summary && (
        <section style={styles.summarySection}>
          <h2 style={styles.sectionTitle}>Summary</h2>
          <div style={styles.summaryGrid}>
            <div style={styles.summaryCard}>
              <div style={styles.summaryNumber}>{summary.total}</div>
              <div style={styles.summaryLabel}>Total Donations</div>
            </div>
            <div style={styles.summaryCard}>
              <div style={styles.summaryNumber}>{formatCurrency(summary.sumCents)}</div>
              <div style={styles.summaryLabel}>Total Amount</div>
            </div>
            <div style={styles.summaryCard}>
              <div style={styles.summaryNumber}>{summary.recurringCount}</div>
              <div style={styles.summaryLabel}>Recurring</div>
            </div>
          </div>
        </section>
      )}

      <section style={styles.createSection}>
        <h2 style={styles.sectionTitle}>Record New Donation</h2>
        <div style={styles.createForm}>
          <div style={styles.formRow}>
            <input
              type="number"
              placeholder="Amount (USD)"
              value={newDonation.amount}
              onChange={(e) => setNewDonation(prev => ({ ...prev, amount: e.target.value }))}
              style={styles.input}
              step="0.01"
              min="0"
            />
            <label style={styles.checkboxLabel}>
              <input
                type="checkbox"
                checked={newDonation.recurring}
                onChange={(e) => setNewDonation(prev => ({ ...prev, recurring: e.target.checked }))}
              />
              Recurring
            </label>
            <button
              onClick={createDonation}
              disabled={creating || !newDonation.amount}
              style={styles.createButton}
            >
              {creating ? 'Recording...' : 'Record Donation'}
            </button>
          </div>
        </div>
      </section>

      <section style={styles.listSection}>
        <h2 style={styles.sectionTitle}>Recent Donations</h2>
        {donations.length === 0 ? (
          <div style={styles.emptyState}>No donations recorded yet.</div>
        ) : (
          <div style={styles.donationsList}>
            {donations.map(donation => (
              <div key={donation.id} style={styles.donationCard}>
                <div style={styles.donationAmount}>
                  {formatCurrency(donation.amountCents)}
                </div>
                <div style={styles.donationDetails}>
                  <div style={styles.donationDate}>
                    {formatDate(donation.createdUtc)}
                  </div>
                  <div style={styles.donationMeta}>
                    <span style={styles.donationStatus}>{donation.status}</span>
                    {donation.recurring && <span style={styles.recurringBadge}>Recurring</span>}
                    {donation.provider && <span style={styles.providerBadge}>{donation.provider}</span>}
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
  summarySection: {
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
  summaryGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '1rem'
  },
  summaryCard: {
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    borderRadius: '8px',
    padding: '1.5rem',
    textAlign: 'center',
    color: '#fff'
  },
  summaryNumber: {
    fontSize: '2rem',
    fontWeight: '700',
    marginBottom: '0.5rem'
  },
  summaryLabel: {
    fontSize: '0.9rem',
    opacity: 0.9
  },
  createSection: {
    background: '#fff',
    borderRadius: '12px',
    padding: '1.5rem',
    marginBottom: '2rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  createForm: {
    marginTop: '1rem'
  },
  formRow: {
    display: 'flex',
    gap: '1rem',
    alignItems: 'center',
    flexWrap: 'wrap'
  },
  input: {
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem',
    minWidth: '150px'
  },
  checkboxLabel: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem',
    cursor: 'pointer'
  },
  createButton: {
    background: '#28a745',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    padding: '0.75rem 1.5rem',
    fontSize: '1rem',
    fontWeight: '500',
    cursor: 'pointer',
    transition: 'background-color 0.2s'
  },
  listSection: {
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
  donationsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.75rem'
  },
  donationCard: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '1rem',
    border: '1px solid #e9ecef',
    borderRadius: '8px',
    background: '#f8f9fa'
  },
  donationAmount: {
    fontSize: '1.25rem',
    fontWeight: '600',
    color: '#28a745'
  },
  donationDetails: {
    textAlign: 'right'
  },
  donationDate: {
    fontSize: '0.9rem',
    color: '#666',
    marginBottom: '0.25rem'
  },
  donationMeta: {
    display: 'flex',
    gap: '0.5rem',
    justifyContent: 'flex-end'
  },
  donationStatus: {
    background: '#28a745',
    color: '#fff',
    padding: '0.2rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.8rem',
    textTransform: 'uppercase'
  },
  recurringBadge: {
    background: '#17a2b8',
    color: '#fff',
    padding: '0.2rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.8rem'
  },
  providerBadge: {
    background: '#6c757d',
    color: '#fff',
    padding: '0.2rem 0.5rem',
    borderRadius: '4px',
    fontSize: '0.8rem'
  }
};
