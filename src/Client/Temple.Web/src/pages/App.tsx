import { Routes, Route, Link } from 'react-router-dom';
import Tenants from './Tenants';
import Home from './Home';

export default function App() {
  return (
    <div style={{ fontFamily: 'sans-serif', padding: 16 }}>
      <h1>Temple UAT</h1>
      <nav style={{ display: 'flex', gap: 12 }}>
        <Link to="/">Home</Link>
        <Link to="/tenants">Tenants</Link>
        <a href="/swagger" target="_blank" rel="noreferrer">Swagger</a>
      </nav>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/tenants" element={<Tenants />} />
      </Routes>
    </div>
  );
}
