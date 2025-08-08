import { useEffect, useState } from 'react';

interface Tenant {
  id: string;
  name: string;
  slug: string;
  status?: string;
}

export default function Tenants() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // No list endpoint yet; placeholder
  }, []);

  async function createTenant() {
    setCreating(true); setError(null);
    try {
      const token = localStorage.getItem('auth_token');
      if (!token) throw new Error('Login first');
      const resp = await fetch('/api/v1/tenants', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify({ name: `UAT Tenant ${Date.now()}` }) });
      if (!resp.ok) throw new Error(await resp.text());
      const created = await resp.json();
      setTenants(t => [...t, created]);
    } catch (e:any) { setError(e.message); } finally { setCreating(false); }
  }

  return (
    <div>
      <h2>Tenants</h2>
  <button disabled={creating} onClick={createTenant}>{creating ? 'Creating...' : 'Create Tenant (after login)'}</button>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <ul>
        {tenants.map(t => <li key={t.id}>{t.name} ({t.slug})</li>)}
      </ul>
    </div>
  );
}
