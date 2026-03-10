import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  return (
    <nav style={styles.nav}>
      <Link to="/" style={styles.brand}>🔍 StolenTracker</Link>
      <div style={styles.links}>
        <Link to="/" style={styles.link}>Search</Link>
        {user && <Link to="/complaints" style={styles.link}>My Complaints</Link>}
        {user?.isAdmin && <Link to="/admin" style={styles.link}>Admin</Link>}
        {user
          ? <button onClick={handleLogout} style={styles.btn}>Logout ({user.name})</button>
          : <Link to="/login" style={styles.btn}>Sign In</Link>
        }
      </div>
    </nav>
  )
}

const styles: Record<string, React.CSSProperties> = {
  nav: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '12px 24px', background: '#1a1a2e', color: '#fff' },
  brand: { color: '#e94560', fontWeight: 700, fontSize: 20, textDecoration: 'none' },
  links: { display: 'flex', gap: 16, alignItems: 'center' },
  link: { color: '#ccc', textDecoration: 'none', fontSize: 14 },
  btn: { background: '#e94560', color: '#fff', border: 'none', padding: '8px 16px', borderRadius: 6, cursor: 'pointer', fontSize: 14, textDecoration: 'none' }
}