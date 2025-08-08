import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const navigate = useNavigate();

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
  const resp = await fetch('/api/v1/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      if (!resp.ok) {
        throw new Error(`Login failed: ${resp.status}`);
      }
      const json = await resp.json();
      setToken(json.accessToken);
      localStorage.setItem('auth_token', json.accessToken);
      // Redirect to home page after successful login
      navigate('/');
    } catch (err: any) {
      setError(err.message || 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  async function register() {
    setError(null);
    setLoading(true);
    try {
      const resp = await fetch('/api/v1/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      if (!resp.ok) throw new Error(`Register failed: ${resp.status}`);
      // After register, trigger login
      await submit(new Event('submit') as any);
    } catch (err: any) {
      setError(err.message || 'Unknown error');
      setLoading(false);
    }
  }

  async function guestAccess() {
    setError(null);
    setLoading(true);
    try {
      const resp = await fetch('/api/v1/auth/guest', { method: 'POST' });
      if (!resp.ok) throw new Error('Guest access failed');
      const json = await resp.json();
      setToken(json.accessToken);
      localStorage.setItem('auth_token', json.accessToken);
      // Redirect to home page after successful guest access
      navigate('/');
    } catch (e: any) {
      setError(e.message || 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={styles.shell}>
      <div style={styles.card}>
        <h1 style={styles.logo}>Temple</h1>
        <h2 style={styles.subtitle}>Access your community</h2>
        <form onSubmit={submit} style={styles.form}>
          <div style={styles.field}>
            <input placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} type="email" required style={styles.input} />
          </div>
          <div style={styles.field}>
            <input placeholder="Password" value={password} onChange={e => setPassword(e.target.value)} type="password" required style={styles.input} />
          </div>
          <div style={styles.buttons}>
            <button type="submit" disabled={loading} style={styles.primary}>{loading ? 'Working...' : 'Login'}</button>
            <button type="button" onClick={register} disabled={loading} style={styles.secondary}>Register</button>
            <button type="button" onClick={guestAccess} disabled={loading} style={styles.ghost}>Guest</button>
          </div>
        </form>
        {error && <div style={styles.error}>{error}</div>}
        {token && <div style={styles.tokenBox}><strong>Token issued</strong><div style={styles.token}>{token}</div></div>}
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  shell: { minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'center', background: 'linear-gradient(135deg,#eef2f7,#dfe7f1)' },
  card: { width: '100%', maxWidth: 420, background: '#fff', padding: '40px 46px 48px', borderRadius: 32, boxShadow: '0 10px 40px -8px rgba(0,0,0,0.12)', fontFamily: 'system-ui,sans-serif' },
  logo: { margin: '0 0 4px', fontSize: 42, textAlign: 'center', background: 'linear-gradient(90deg,#2b59ff,#bb2bff)', WebkitBackgroundClip: 'text', color: 'transparent', letterSpacing: '-1px' },
  subtitle: { margin: '0 0 32px', fontWeight: 500, fontSize: 18, textAlign: 'center', color: '#445064' },
  form: { display: 'flex', flexDirection: 'column', gap: 18 },
  field: { display: 'flex', flexDirection: 'column', gap: 6 },
  input: { border: '1px solid #c9d2df', borderRadius: 14, padding: '14px 16px', fontSize: 15, outline: 'none', background: '#f9fbfd' },
  buttons: { display: 'flex', gap: 12, marginTop: 4, flexWrap: 'wrap' },
  primary: { flex: 1, background: '#2b59ff', color: '#fff', border: 'none', padding: '14px 0', borderRadius: 14, fontSize: 15, fontWeight: 600, cursor: 'pointer', boxShadow: '0 6px 16px -4px rgba(43,89,255,.5)' },
  secondary: { flex: 1, background: '#fff', color: '#2b59ff', border: '1px solid #2b59ff', padding: '14px 0', borderRadius: 14, fontSize: 15, fontWeight: 600, cursor: 'pointer' },
  ghost: { flex: 1, background: '#f0f4ff', color: '#2b59ff', border: '1px dashed #2b59ff', padding: '14px 0', borderRadius: 14, fontSize: 15, fontWeight: 600, cursor: 'pointer' },
  error: { background: '#ffe8e8', color: '#b30000', padding: '10px 14px', borderRadius: 12, fontSize: 13, marginTop: 16 },
  tokenBox: { marginTop: 28, fontSize: 12, color: '#223' },
  token: { marginTop: 8, maxHeight: 160, overflow: 'auto', background: '#f5f8fc', fontFamily: 'monospace', fontSize: 11, padding: '10px 12px', borderRadius: 10 }
};
