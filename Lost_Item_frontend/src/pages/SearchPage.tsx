import { useState } from 'react'
import axios from 'axios'

interface SearchResult {
  productId: number
  trackingId: string
  type: string
  brand: string
  model: string
  isStolen: boolean
  openComplaints: { id: number; locationStolen: string; createdAt: string }[]
}

function identifierLabel(type: string): string {
  switch (type) {
    case 'Mobile': return 'IMEI'
    case 'Bike':   return 'Frame No.'
    case 'Laptop': return 'Serial No.'
    default:       return 'ID'
  }
}

export default function SearchPage() {
  const [selectedType, setSelectedType] = useState('Mobile-IMEI')
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<SearchResult | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const typeToEnum: Record<string, string> = {
    'Mobile-IMEI': 'Mobile',
    'Bike-Engine/FrameNumber': 'Bike',
    'Laptop-Mac/serialNumber': 'Laptop'
  }

  const placeholderByType: Record<string, string> = {
    'Mobile-IMEI': 'e.g. 356938035643809',
    'Bike-Engine/FrameNumber': 'e.g. EN12345 / FR98765',
    'Laptop-Mac/serialNumber': 'e.g. C02XK0XHHTD6'
  }

  const search = async () => {
    if (!query.trim()) return
    setLoading(true)
    setError('')
    setResult(null)
    try {
      const { data } = await axios.get('/api/search', {
        params: { trackingId: query.trim(), type: typeToEnum[selectedType] }
      })
      setResult(data)
    } catch (e: any) {
      const status = (e.response?.status as number) | 0
      if (status === 404) {
        setError('No product found with that identifier')
      } else {
        setError(e.response?.data || 'Search failed')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={s.page}>
      <div style={s.hero}>
        <h1 style={s.title}>Check if a product is stolen</h1>
        <p style={s.subtitle}>Select type, then enter the identifier</p>
        <div style={s.searchRow}>
          <select
            style={s.select}
            value={selectedType}
            onChange={e => setSelectedType(e.target.value)}
          >
            <option value="Mobile-IMEI">Mobile - IMEI</option>
            <option value="Bike-Engine/FrameNumber">Bike - Engine/Frame Number</option>
            <option value="Laptop-Mac/serialNumber">Laptop - Mac/Serial Number</option>
          </select>
          <input
            style={s.input}
            value={query}
            onChange={e => setQuery(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && search()}
            placeholder={placeholderByType[selectedType]}
          />
          <button style={s.btn} onClick={search} disabled={loading}>
            {loading ? 'Searching...' : 'Search'}
          </button>
        </div>
        {error && <p style={s.error}>{error}</p>}
      </div>

      {result && (
        <div style={s.card}>
          {result.isStolen && (
            <div style={{ ...s.badge, background: '#e94560' }}>
              ⚠️ REPORTED STOLEN
            </div>
          )}
          <div style={s.productHeader}>
            <h2 style={s.productTitle}>{result.brand} {result.model}</h2>
            <span style={s.typeChip}>{result.type}</span>
          </div>
          <p style={s.idRow}>
            <span style={s.idLabel}>{identifierLabel(result.type)}:</span> {result.trackingId}
          </p>

          {result.isStolen && result.openComplaints.length > 0 && (
            <div style={{ marginTop: 12 }}>
              <h3 style={s.complaintsTitle}>Stolen Reports</h3>
              {result.openComplaints.map(c => (
                <div key={c.id} style={s.complaintItem}>
                  <p style={s.itemLocation}>📍 {c.locationStolen}</p>
                  <p style={s.date}>Reported: {new Date(c.createdAt).toLocaleDateString()}</p>
                </div>
              ))}
            </div>
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
  searchRow: { display: 'flex', gap: 12, flexDirection: 'column' },
  select: { width: '100%', padding: '12px 36px 12px 12px', borderRadius: 8, border: '1px solid #333', background: '#1a1a2e', color: '#fff', fontSize: 14 },
  input: { width: '100%', padding: '12px 16px', borderRadius: 8, border: '1px solid #333', background: '#1a1a2e', color: '#fff', fontSize: 16 },
  btn: { width: '100%', padding: '12px 24px', background: '#e94560', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer', fontWeight: 600 },
  error: { color: '#e94560', marginTop: 12 },
  card:          { maxWidth: 600, margin: '0 auto', background: '#1a1a2e', borderRadius: 12, padding: 24 },
  badge:         { display: 'inline-block', padding: '6px 14px', borderRadius: 20, fontWeight: 700, marginBottom: 16, fontSize: 14 },
  productHeader: { display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 },
  productTitle:  { margin: 0, fontSize: 22, fontWeight: 700 },
  typeChip:      { fontSize: 12, color: '#aaa', background: '#0f0f1a', padding: '3px 10px', borderRadius: 10, whiteSpace: 'nowrap' },
  idRow:          { color: '#ccc', fontSize: 13, margin: '0 0 4px' },
  idLabel:        { color: '#666', fontSize: 12 },
  complaintsTitle:{ fontSize: 15, fontWeight: 600, margin: '0 0 8px' },
  complaintItem:  { background: '#0f0f1a', borderRadius: 8, padding: 12, marginBottom: 8 },
  itemLocation:   { margin: '0 0 4px', fontSize: 14 },
  date:           { color: '#aaa', fontSize: 12, margin: 0 }
}