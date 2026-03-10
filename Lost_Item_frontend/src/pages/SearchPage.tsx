import { useState } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'
import { Link } from 'react-router-dom'

interface SearchResult {
  productId: number
  trackingId: string
  type: string
  brand: string
  model: string
  isStolen: boolean
  openComplaints: { id: number; locationStolen: string; createdAt: string }[]
}

export default function SearchPage() {
  const { user } = useAuth()
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<SearchResult | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const search = async () => {
    if (!query.trim()) return
    setLoading(true)
    setError('')
    setResult(null)
    try {
      const { data } = await axios.get('/api/search', { params: { trackingId: query.trim() } })
      setResult(data)
    } catch (e: any) {
      setError(e.response?.data || 'No product found')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={s.page}>
      <div style={s.hero}>
        <h1 style={s.title}>Check if a product is stolen</h1>
        <p style={s.subtitle}>Enter IMEI, Frame Number, or Serial Number</p>
        <div style={s.searchRow}>
          <input
            style={s.input}
            value={query}
            onChange={e => setQuery(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && search()}
            placeholder="e.g. 356938035643809"
          />
          <button style={s.btn} onClick={search} disabled={loading}>
            {loading ? 'Searching...' : 'Search'}
          </button>
        </div>
        {error && <p style={s.error}>{error}</p>}
      </div>

      {result && (
        <div style={s.card}>
          <div style={{ ...s.badge, background: result.isStolen ? '#e94560' : '#27ae60' }}>
            {result.isStolen ? '⚠️ REPORTED STOLEN' : '✅ NOT REPORTED STOLEN'}
          </div>
          <h2>{result.brand} {result.model}</h2>
          <p><strong>Type:</strong> {result.type}</p>
          <p><strong>Tracking ID:</strong> {result.trackingId}</p>

          {result.isStolen && result.openComplaints.length > 0 && (
            <div>
              <h3>Open Complaints</h3>
              {result.openComplaints.map(c => (
                <div key={c.id} style={s.complaintItem}>
                  <p>📍 {c.locationStolen}</p>
                  <p style={s.date}>{new Date(c.createdAt).toLocaleDateString()}</p>
                </div>
              ))}
            </div>
          )}

          {user && (
            <Link to="/complaints" style={s.reportBtn}>
              + Report this product stolen
            </Link>
          )}
          {!user && (
            <p style={s.hint}><Link to="/login">Sign in</Link> to report a stolen product</p>
          )}
        </div>
      )}
    </div>
  )
}

const s: Record<string, React.CSSProperties> = {
  page: { minHeight: '90vh', background: '#0f0f1a', color: '#fff', padding: 24 },
  hero: { maxWidth: 600, margin: '60px auto 40px', textAlign: 'center' },
  title: { fontSize: 32, fontWeight: 700, marginBottom: 8 },
  subtitle: { color: '#aaa', marginBottom: 24 },
  searchRow: { display: 'flex', gap: 8 },
  input: { flex: 1, padding: '12px 16px', borderRadius: 8, border: '1px solid #333', background: '#1a1a2e', color: '#fff', fontSize: 16 },
  btn: { padding: '12px 24px', background: '#e94560', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer', fontWeight: 600 },
  error: { color: '#e94560', marginTop: 12 },
  card: { maxWidth: 600, margin: '0 auto', background: '#1a1a2e', borderRadius: 12, padding: 24 },
  badge: { display: 'inline-block', padding: '6px 14px', borderRadius: 20, fontWeight: 700, marginBottom: 16, fontSize: 14 },
  complaintItem: { background: '#0f0f1a', borderRadius: 8, padding: 12, marginBottom: 8 },
  date: { color: '#aaa', fontSize: 12 },
  reportBtn: { display: 'inline-block', marginTop: 16, padding: '10px 20px', background: '#e94560', color: '#fff', borderRadius: 8, textDecoration: 'none' },
  hint: { marginTop: 12, color: '#aaa', fontSize: 14 }
}