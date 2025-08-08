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
  const token = localStorage.getItem('token');

  useEffect(() => {
    // No list endpoint yet; placeholder
  }, []);

  async function createTenant() {
    setCreating(true);
    setError(null);
    try {
      const resp = await fetch('/api/tenants', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...(token ? { 'Authorization': `Bearer ${token}` } : {}) },
        body: JSON.stringify({ name: `UAT Tenant ${Date.now()}` })
      });
      if (!resp.ok) throw new Error(await resp.text());
      const created = await resp.json();
      setTenants(t => [...t, created]);
    } catch (e: any) {
      setError(e.message);
    } finally {
      setCreating(false);
    }
  }

  return (
    <div>
      {token && (
        <button disabled={creating} onClick={createTenant}>{creating ? 'Creating...' : 'Create Tenant'}</button>
      )}
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <ul>
        {tenants.map(t => <li key={t.id}>{t.name} ({t.slug})</li>)}
      </ul>
    </div>
  );
}
