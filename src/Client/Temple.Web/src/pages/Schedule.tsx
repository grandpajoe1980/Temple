import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface ScheduleEvent {
  id: string;
  title: string;
  startUtc: string;
  endUtc: string;
  type: string;
  description?: string;
  recurrenceRule?: string;
}

export default function Schedule() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [events, setEvents] = useState<ScheduleEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newEvent, setNewEvent] = useState({
    title: '',
    startUtc: '',
    endUtc: '',
    type: 'service',
    description: ''
  });
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    loadEvents();
  }, []);

  async function loadEvents() {
    setLoading(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');

      const resp = await fetch('/api/v1/schedule/events?page=1&pageSize=50', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!resp.ok) throw new Error('Failed to load events');
      const data = await resp.json();
      setEvents(data.data || []);

    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  async function createEvent() {
    if (!newEvent.title || !newEvent.startUtc || !newEvent.endUtc) return;
    
    setCreating(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const resp = await fetch('/api/v1/schedule/events', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          title: newEvent.title,
          startUtc: new Date(newEvent.startUtc).toISOString(),
          endUtc: new Date(newEvent.endUtc).toISOString(),
          type: newEvent.type,
          description: newEvent.description || undefined
        })
      });

      if (!resp.ok) throw new Error('Failed to create event');
      
      setNewEvent({
        title: '',
        startUtc: '',
        endUtc: '',
        type: 'service',
        description: ''
      });
      setShowCreateForm(false);
      loadEvents(); // Reload the data
      
    } catch (e: any) {
      setError(e.message);
    } finally {
      setCreating(false);
    }
  }

  async function deleteEvent(eventId: string) {
    if (!confirm('Are you sure you want to delete this event?')) return;
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const resp = await fetch(`/api/v1/schedule/events/${eventId}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (!resp.ok) throw new Error('Failed to delete event');
      
      loadEvents(); // Reload the data
      
    } catch (e: any) {
      setError(e.message);
    }
  }

  function formatDateTime(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  function getEventTypeColor(type: string): string {
    switch (type) {
      case 'service': return '#28a745';
      case 'meeting': return '#17a2b8';
      case 'group': return '#ffc107';
      case 'community': return '#6f42c1';
      default: return '#6c757d';
    }
  }

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading schedule...</div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <header style={styles.header}>
        <h1 style={styles.title}>Schedule</h1>
        <div style={styles.headerActions}>
          <button 
            onClick={() => setShowCreateForm(!showCreateForm)} 
            style={styles.createButton}
          >
            {showCreateForm ? 'Cancel' : 'Add Event'}
          </button>
          <button onClick={() => navigate(`/tenant/${slug}`)} style={styles.backButton}>
            ← Back to Dashboard
          </button>
        </div>
      </header>

      {error && <div style={styles.error}>{error}</div>}

      {showCreateForm && (
        <section style={styles.createSection}>
          <h2 style={styles.sectionTitle}>Create New Event</h2>
          <div style={styles.createForm}>
            <div style={styles.formRow}>
              <input
                type="text"
                placeholder="Event Title"
                value={newEvent.title}
                onChange={(e) => setNewEvent(prev => ({ ...prev, title: e.target.value }))}
                style={styles.input}
              />
              <select
                value={newEvent.type}
                onChange={(e) => setNewEvent(prev => ({ ...prev, type: e.target.value }))}
                style={styles.select}
              >
                <option value="service">Service</option>
                <option value="meeting">Meeting</option>
                <option value="group">Group</option>
                <option value="community">Community</option>
                <option value="other">Other</option>
              </select>
            </div>
            <div style={styles.formRow}>
              <div style={styles.dateField}>
                <label style={styles.label}>Start Time</label>
                <input
                  type="datetime-local"
                  value={newEvent.startUtc}
                  onChange={(e) => setNewEvent(prev => ({ ...prev, startUtc: e.target.value }))}
                  style={styles.input}
                />
              </div>
              <div style={styles.dateField}>
                <label style={styles.label}>End Time</label>
                <input
                  type="datetime-local"
                  value={newEvent.endUtc}
                  onChange={(e) => setNewEvent(prev => ({ ...prev, endUtc: e.target.value }))}
                  style={styles.input}
                />
              </div>
            </div>
            <textarea
              placeholder="Description (optional)"
              value={newEvent.description}
              onChange={(e) => setNewEvent(prev => ({ ...prev, description: e.target.value }))}
              style={styles.textarea}
              rows={3}
            />
            <button
              onClick={createEvent}
              disabled={creating || !newEvent.title || !newEvent.startUtc || !newEvent.endUtc}
              style={styles.submitButton}
            >
              {creating ? 'Creating...' : 'Create Event'}
            </button>
          </div>
        </section>
      )}

      <section style={styles.eventsSection}>
        <h2 style={styles.sectionTitle}>Upcoming Events</h2>
        {events.length === 0 ? (
          <div style={styles.emptyState}>No events scheduled yet.</div>
        ) : (
          <div style={styles.eventsList}>
            {events.map(event => (
              <div key={event.id} style={styles.eventCard}>
                <div style={styles.eventHeader}>
                  <div style={styles.eventTitle}>{event.title}</div>
                  <div style={styles.eventActions}>
                    <span 
                      style={{
                        ...styles.eventType,
                        backgroundColor: getEventTypeColor(event.type)
                      }}
                    >
                      {event.type}
                    </span>
                    <button 
                      onClick={() => deleteEvent(event.id)}
                      style={styles.deleteButton}
                    >
                      Delete
                    </button>
                  </div>
                </div>
                <div style={styles.eventDetails}>
                  <div style={styles.eventTime}>
                    <strong>Start:</strong> {formatDateTime(event.startUtc)}
                  </div>
                  <div style={styles.eventTime}>
                    <strong>End:</strong> {formatDateTime(event.endUtc)}
                  </div>
                </div>
                {event.description && (
                  <div style={styles.eventDescription}>{event.description}</div>
                )}
                {event.recurrenceRule && (
                  <div style={styles.recurrenceInfo}>
                    <strong>Recurrence:</strong> {event.recurrenceRule}
                  </div>
                )}
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
  dateField: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem',
    flex: 1,
    minWidth: '200px'
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
  textarea: {
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem',
    resize: 'vertical',
    fontFamily: 'inherit'
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
  eventsSection: {
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
  eventsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem'
  },
  eventCard: {
    border: '1px solid #e9ecef',
    borderRadius: '8px',
    padding: '1.5rem',
    background: '#f8f9fa'
  },
  eventHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '1rem'
  },
  eventTitle: {
    fontSize: '1.25rem',
    fontWeight: '600',
    color: '#333'
  },
  eventActions: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem'
  },
  eventType: {
    color: '#fff',
    padding: '0.25rem 0.75rem',
    borderRadius: '4px',
    fontSize: '0.8rem',
    textTransform: 'uppercase',
    fontWeight: '500'
  },
  deleteButton: {
    background: '#dc3545',
    color: '#fff',
    border: 'none',
    borderRadius: '4px',
    padding: '0.25rem 0.75rem',
    fontSize: '0.8rem',
    cursor: 'pointer'
  },
  eventDetails: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: '0.5rem',
    marginBottom: '1rem'
  },
  eventTime: {
    fontSize: '0.9rem',
    color: '#666'
  },
  eventDescription: {
    fontSize: '1rem',
    color: '#555',
    lineHeight: '1.5',
    marginBottom: '0.5rem'
  },
  recurrenceInfo: {
    fontSize: '0.9rem',
    color: '#6c757d',
    fontStyle: 'italic'
  }
};
