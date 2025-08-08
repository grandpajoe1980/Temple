import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

interface Tenant { id: string; name: string; slug: string; }

export default function Home() {
  const [query, setQuery] = useState('');
  const [result, setResult] = useState<Tenant | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [favorites, setFavorites] = useState<Tenant[]>([]);
  const navigate = useNavigate();

  useEffect(() => {
    try {
      const raw = localStorage.getItem('fav_tenants');
      if (raw) setFavorites(JSON.parse(raw));
    } catch { /* ignore */ }
  }, []);

  function saveFavorites(list: Tenant[]) {
    setFavorites(list);
    try { localStorage.setItem('fav_tenants', JSON.stringify(list)); } catch { /* ignore */ }
  }

  async function search(e?: React.FormEvent) {
    if (e) e.preventDefault();
    setError(null);
    setResult(null);
    const q = query.trim();
    if (!q) return;
    setLoading(true);
    try {
      // Accept direct slug or attempt slugify (simple)
      const slug = q.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
      const resp = await fetch(`/api/v1/tenants/by-slug/${encodeURIComponent(slug)}`);
      if (resp.status === 404) { setError('No temple found for that name.'); return; }
      if (!resp.ok) throw new Error('Search failed');
      const t = await resp.json();
      setResult(t);
    } catch (err: any) {
      setError(err.message || 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  function favoriteCurrent() {
    if (!result) return;
    if (favorites.some(f => f.id === result.id)) return;
    const list = [...favorites, result].slice(0, 12);
    saveFavorites(list);
  }

  function enterTenant(tenant: Tenant) {
    navigate(`/tenant/${tenant.slug}`);
  }

  function removeFavorite(id: string) {
    saveFavorites(favorites.filter(f => f.id !== id));
  }

  return (
    <div style={styles.shell}>
      <div style={styles.centerBlock}>
        <h1 style={styles.logo}>Temple</h1>
        <form onSubmit={search} style={styles.searchForm}>
          <input
            type="text"
            placeholder="Find your temple/community..."
            value={query}
            onChange={e => setQuery(e.target.value)}
            style={styles.searchInput}
          />
          <div style={styles.actions}>
            <button type="submit" style={styles.primaryBtn} disabled={loading}>{loading ? 'Searching...' : 'Search'}</button>
          </div>
        </form>
        {error && <div style={styles.error}>{error}</div>}
        {result && (
          <div style={styles.resultCard}>
            <div>
              <h2 style={{ margin: '0 0 4px' }}>{result.name}</h2>
              <p style={{ margin: 0, fontSize: 14, opacity: 0.7 }}>Slug: {result.slug}</p>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <button style={styles.secondaryBtn} onClick={favoriteCurrent}>Favorite</button>
              <button style={styles.primaryBtn} onClick={() => enterTenant(result)}>Enter</button>
              <a style={styles.linkBtn} href="/login">Login</a>
            </div>
          </div>
        )}
        {favorites.length > 0 && (
          <div style={styles.favs}>
            <h3 style={{ margin: '24px 0 12px' }}>Favorites</h3>
            <div style={styles.favGrid}>
              {favorites.map(f => (
                <div key={f.id} style={styles.favCard}>
                  <div style={{ flex: 1 }}>
                    <strong>{f.name}</strong>
                    <div style={{ fontSize: 12, opacity: 0.7 }}>{f.slug}</div>
                  </div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                    <button onClick={() => enterTenant(f)} style={styles.miniBtn}>Enter</button>
                    <a href="/login" style={styles.miniBtn}>Login</a>
                    <button onClick={() => removeFavorite(f.id)} style={styles.miniDanger}>×</button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
        <div style={styles.footerHint}>Not registered? Search your temple then click Login to create an account.</div>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  shell: { minHeight: 'calc(100vh - 70px)', display: 'flex', justifyContent: 'center', alignItems: 'flex-start', paddingTop: 60, fontFamily: 'system-ui, sans-serif', background: 'linear-gradient(135deg,#f5f7fa,#e4ecf5)' },
  centerBlock: { width: '100%', maxWidth: 720, padding: '0 24px' },
  logo: { fontSize: 64, textAlign: 'center', fontWeight: 700, letterSpacing: '-2px', background: 'linear-gradient(90deg,#2b59ff,#bb2bff)', WebkitBackgroundClip: 'text', color: 'transparent', margin: '0 0 32px' },
  searchForm: { display: 'flex', flexDirection: 'column', gap: 16 },
  searchInput: { padding: '16px 22px', fontSize: 20, borderRadius: 40, border: '1px solid #ccd3dd', outline: 'none', boxShadow: '0 2px 6px rgba(0,0,0,0.06)', transition: 'box-shadow .2s', background: '#fff' },
  actions: { display: 'flex', gap: 12, justifyContent: 'center' },
  primaryBtn: { background: '#2b59ff', color: '#fff', border: 'none', padding: '12px 28px', borderRadius: 30, cursor: 'pointer', fontSize: 16, fontWeight: 600, boxShadow: '0 4px 12px rgba(43,89,255,0.4)' },
  secondaryBtn: { background: '#fff', color: '#2b59ff', border: '1px solid #2b59ff', padding: '10px 20px', borderRadius: 24, cursor: 'pointer', fontSize: 14, fontWeight: 600 },
  linkBtn: { background: '#bb2bff', color: '#fff', padding: '10px 20px', borderRadius: 24, fontSize: 14, fontWeight: 600, textDecoration: 'none', display: 'inline-flex', alignItems: 'center' },
  error: { background: '#ffe8e8', color: '#b30000', padding: '10px 16px', borderRadius: 12, fontSize: 14 },
  resultCard: { marginTop: 20, background: '#fff', borderRadius: 24, padding: '20px 28px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 20, boxShadow: '0 6px 18px rgba(0,0,0,0.08)' },
  favs: { marginTop: 10 },
  favGrid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(180px,1fr))', gap: 16 },
  favCard: { background: '#fff', borderRadius: 20, padding: '14px 16px', display: 'flex', gap: 12, alignItems: 'stretch', boxShadow: '0 4px 12px rgba(0,0,0,0.06)', minHeight: 110 },
  miniBtn: { background: '#2b59ff', color: '#fff', borderRadius: 18, padding: '6px 12px', fontSize: 12, textDecoration: 'none', textAlign: 'center', border: 'none', cursor: 'pointer', display: 'block' },
  miniDanger: { background: '#ff4d4f', color: '#fff', borderRadius: 18, padding: '6px 12px', fontSize: 14, border: 'none', cursor: 'pointer' },
  footerHint: { marginTop: 40, fontSize: 12, textAlign: 'center', opacity: 0.6 }
};

