import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const resp = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      if (!resp.ok) throw new Error(await resp.text());
      const data = await resp.json();
      localStorage.setItem('token', data.accessToken);
      navigate('/tenants');
    } catch (e: any) {
      setError(e.message);
    }
  }

  return (
    <form onSubmit={submit}>
      <h2>Login</h2>
      <div>
        <label>
          Email
          <input value={email} onChange={e => setEmail(e.target.value)} />
        </label>
      </div>
      <div>
        <label>
          Password
          <input type="password" value={password} onChange={e => setPassword(e.target.value)} />
        </label>
      </div>
      <button type="submit">Login</button>
      {error && <p style={{ color: 'red' }}>{error}</p>}
    </form>
  );
}
