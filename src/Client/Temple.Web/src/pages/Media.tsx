import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

interface MediaAsset {
  id: string;
  title: string;
  type: string;
  storageKey: string;
  status: string;
  durationSeconds?: number;
  createdUtc: string;
}

export default function Media() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [assets, setAssets] = useState<MediaAsset[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterType, setFilterType] = useState<'all' | 'video' | 'audio'>('all');
  const [showUploadForm, setShowUploadForm] = useState(false);
  const [newAsset, setNewAsset] = useState({
    title: '',
    type: 'audio'
  });
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    loadAssets();
  }, []);

  async function loadAssets() {
    setLoading(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');

      const resp = await fetch('/api/v1/media/assets?page=1&pageSize=100', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (!resp.ok) throw new Error('Failed to load media');
      const data = await resp.json();
      setAssets(data.data || []);

    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  async function createAsset() {
    if (!newAsset.title) return;
    
    setUploading(true);
    setError(null);
    
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Authentication required');
      
      const resp = await fetch('/api/v1/media/assets', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          title: newAsset.title,
          type: newAsset.type,
          storageKey: `placeholder-${Date.now()}`
        })
      });

      if (!resp.ok) throw new Error('Failed to create media asset');
      
      setNewAsset({ title: '', type: 'audio' });
      setShowUploadForm(false);
      loadAssets();
      
    } catch (e: any) {
      setError(e.message);
    } finally {
      setUploading(false);
    }
  }

  function formatDuration(seconds?: number): string {
    if (!seconds) return 'Unknown';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  function formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  function getTypeIcon(type: string): string {
    switch (type) {
      case 'video': return '🎥';
      case 'audio': return '🎵';
      case 'document': return '📄';
      default: return '📁';
    }
  }

  function getStatusColor(status: string): string {
    switch (status) {
      case 'ready': return '#28a745';
      case 'processing': return '#ffc107';
      case 'failed': return '#dc3545';
      default: return '#6c757d';
    }
  }

  const filteredAssets = assets.filter(asset => {
    if (filterType === 'all') return true;
    return asset.type === filterType;
  });

  if (loading) {
    return (
      <div style={styles.container}>
        <div style={styles.loading}>Loading media library...</div>
      </div>
    );
  }

  return (
    <div style={styles.container}>
      <header style={styles.header}>
        <h1 style={styles.title}>Media Library</h1>
        <div style={styles.headerActions}>
          <button 
            onClick={() => setShowUploadForm(!showUploadForm)} 
            style={styles.createButton}
          >
            {showUploadForm ? 'Cancel' : 'Add Media'}
          </button>
          <button onClick={() => navigate(`/tenant/${slug}`)} style={styles.backButton}>
            ← Back to Dashboard
          </button>
        </div>
      </header>

      {error && <div style={styles.error}>{error}</div>}

      {showUploadForm && (
        <section style={styles.uploadSection}>
          <h2 style={styles.sectionTitle}>Add New Media</h2>
          <div style={styles.uploadForm}>
            <input
              type="text"
              placeholder="Title"
              value={newAsset.title}
              onChange={(e) => setNewAsset(prev => ({ ...prev, title: e.target.value }))}
              style={styles.input}
            />
            <select
              value={newAsset.type}
              onChange={(e) => setNewAsset(prev => ({ ...prev, type: e.target.value }))}
              style={styles.select}
            >
              <option value="audio">Audio (Podcast/Sermon)</option>
              <option value="video">Video</option>
              <option value="document">Document</option>
            </select>
            <button
              onClick={createAsset}
              disabled={uploading || !newAsset.title}
              style={styles.submitButton}
            >
              {uploading ? 'Creating...' : 'Create Media Asset'}
            </button>
            <p style={styles.uploadNote}>
              Note: File upload functionality will be added in a future update.
            </p>
          </div>
        </section>
      )}

      <section style={styles.mediaSection}>
        <div style={styles.filterBar}>
          <div style={styles.filterButtons}>
            {(['all', 'video', 'audio'] as const).map(type => (
              <button
                key={type}
                onClick={() => setFilterType(type)}
                style={{
                  ...styles.filterButton,
                  background: filterType === type ? '#28a745' : '#f8f9fa',
                  color: filterType === type ? '#fff' : '#333'
                }}
              >
                {type === 'all' ? 'All Media' : type === 'video' ? '🎥 Videos' : '🎵 Audio/Podcasts'}
              </button>
            ))}
          </div>
        </div>

        {filteredAssets.length === 0 ? (
          <div style={styles.emptyState}>
            {filterType === 'all' 
              ? 'No media assets yet. Add your first sermon, video, or podcast!'
              : `No ${filterType} files found.`}
          </div>
        ) : (
          <div style={styles.mediaGrid}>
            {filteredAssets.map(asset => (
              <div key={asset.id} style={styles.mediaCard}>
                <div style={styles.mediaIcon}>
                  {getTypeIcon(asset.type)}
                </div>
                <div style={styles.mediaInfo}>
                  <h3 style={styles.mediaTitle}>{asset.title}</h3>
                  <div style={styles.mediaDetails}>
                    <span style={styles.mediaType}>
                      {asset.type.toUpperCase()}
                    </span>
                    <span 
                      style={{
                        ...styles.mediaStatus,
                        color: getStatusColor(asset.status)
                      }}
                    >
                      {asset.status}
                    </span>
                  </div>
                  {asset.durationSeconds && (
                    <div style={styles.mediaDuration}>
                      Duration: {formatDuration(asset.durationSeconds)}
                    </div>
                  )}
                  <div style={styles.mediaDate}>
                    Added: {formatDate(asset.createdUtc)}
                  </div>
                </div>
                <div style={styles.mediaActions}>
                  {asset.type === 'video' && asset.status === 'ready' && (
                    <button style={styles.playButton}>▶ Play</button>
                  )}
                  {asset.type === 'audio' && asset.status === 'ready' && (
                    <button style={styles.playButton}>▶ Listen</button>
                  )}
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
  uploadSection: {
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
  uploadForm: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem'
  },
  input: {
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem'
  },
  select: {
    padding: '0.75rem',
    border: '1px solid #ddd',
    borderRadius: '6px',
    fontSize: '1rem',
    background: '#fff',
    cursor: 'pointer'
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
  uploadNote: {
    fontSize: '0.9rem',
    color: '#666',
    fontStyle: 'italic',
    margin: '0'
  },
  mediaSection: {
    background: '#fff',
    borderRadius: '12px',
    padding: '1.5rem',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
  },
  filterBar: {
    marginBottom: '1.5rem'
  },
  filterButtons: {
    display: 'flex',
    gap: '0.5rem',
    flexWrap: 'wrap'
  },
  filterButton: {
    border: 'none',
    borderRadius: '6px',
    padding: '0.5rem 1rem',
    cursor: 'pointer',
    fontSize: '0.9rem',
    fontWeight: '500',
    transition: 'all 0.2s'
  },
  emptyState: {
    textAlign: 'center',
    padding: '3rem',
    color: '#666',
    fontSize: '1rem'
  },
  mediaGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
    gap: '1rem'
  },
  mediaCard: {
    border: '1px solid #e9ecef',
    borderRadius: '8px',
    padding: '1.5rem',
    background: '#f8f9fa',
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem'
  },
  mediaIcon: {
    fontSize: '3rem',
    textAlign: 'center'
  },
  mediaInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem'
  },
  mediaTitle: {
    margin: 0,
    fontSize: '1.1rem',
    fontWeight: '600',
    color: '#333'
  },
  mediaDetails: {
    display: 'flex',
    gap: '0.75rem',
    alignItems: 'center'
  },
  mediaType: {
    fontSize: '0.8rem',
    fontWeight: '600',
    color: '#666',
    background: '#e9ecef',
    padding: '2px 8px',
    borderRadius: '4px'
  },
  mediaStatus: {
    fontSize: '0.8rem',
    fontWeight: '600'
  },
  mediaDuration: {
    fontSize: '0.9rem',
    color: '#666'
  },
  mediaDate: {
    fontSize: '0.85rem',
    color: '#999'
  },
  mediaActions: {
    display: 'flex',
    gap: '0.5rem'
  },
  playButton: {
    background: '#28a745',
    color: '#fff',
    border: 'none',
    borderRadius: '6px',
    padding: '0.5rem 1rem',
    fontSize: '0.9rem',
    fontWeight: '500',
    cursor: 'pointer',
    flex: 1
  }
};
