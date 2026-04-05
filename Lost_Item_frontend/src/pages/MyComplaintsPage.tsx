import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import api from '../helper/api'

interface Complaint {
  id: number
  productTrackingId: string
  productBrand: string
  productModel: string
  productType: string
  locationStolen: string
  policeReportUrl: string
  status: 'Pending' | 'Approved' | 'Rejected' | 'Resolved'
  createdAt: string
  reviewedAt?: string
  resolvedAt?: string
}

function identifierLabel(type: string): string {
  switch (type) {
    case 'Mobile': return 'IMEI'
    case 'Bike':   return 'Frame No.'
    case 'Laptop': return 'Serial No.'
    default:       return 'ID'
  }
}

const STATUS_META: Record<Complaint['status'], { label: string; color: string; note: string }> = {
  Pending:  { label: 'Pending Review', color: '#f39c12', note: 'Your complaint is waiting for admin review.' },
  Approved: { label: 'Approved',       color: '#e94560', note: 'Approved — visible in the public stolen registry.' },
  Rejected: { label: 'Rejected',       color: '#888',    note: 'An admin has dismissed this complaint.' },
  Resolved: { label: 'Resolved',       color: '#27ae60', note: 'Marked as resolved — item recovered.' },
}

export default function MyComplaintsPage() {
  const [complaints, setComplaints] = useState<Complaint[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    api.get('/complaints')
      .then(r => setComplaints(r.data))
      .catch(() => setError('Failed to load complaints'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div style={s.page}>
      <div style={s.header}>
        <h1 style={s.title}>My Complaints</h1>
        <Link to="/complaints/new" style={s.newBtn}>+ File New Complaint</Link>
      </div>

      {loading && <p style={s.hint}>Loading…</p>}
      {error && <p style={s.error}>{error}</p>}

      {!loading && complaints.length === 0 && (
        <div style={s.empty}>
          <p>You haven't filed any complaints yet.</p>
          <Link to="/complaints/new" style={s.newBtn}>File your first complaint</Link>
        </div>
      )}

      <div style={s.list}>
        {complaints.map(c => {
          const meta = STATUS_META[c.status] ?? STATUS_META.Pending
          return (
            <div key={c.id} style={s.card}>
              <div style={s.cardTop}>
                <div>
                  <span style={s.product}>{c.productBrand} {c.productModel}</span>
                  <span style={s.type}>{c.productType}</span>
                </div>
                <span style={{ ...s.badge, background: meta.color }}>{meta.label}</span>
              </div>

              <p style={s.idRow}><span style={s.idLabel}>{identifierLabel(c.productType)}:</span> {c.productTrackingId}</p>
              <p style={s.field}>📍 {c.locationStolen}</p>
              <p style={{ ...s.note, color: meta.color }}>{meta.note}</p>

              <div style={s.timeline}>
                <span style={s.ts}>Filed: {new Date(c.createdAt).toLocaleDateString()}</span>
                {c.reviewedAt && <span style={s.ts}>Reviewed: {new Date(c.reviewedAt).toLocaleDateString()}</span>}
                {c.resolvedAt && <span style={s.ts}>Resolved: {new Date(c.resolvedAt).toLocaleDateString()}</span>}
              </div>

              <a href={c.policeReportUrl} target="_blank" rel="noreferrer" style={s.link}>📄 Police Report</a>
            </div>
          )
        })}
      </div>
    </div>
  )
}

const s: Record<string, React.CSSProperties> = {
  page:       { minHeight: '90vh', background: '#0f0f1a', color: '#fff', padding: 24, maxWidth: 800, margin: '0 auto' },
  header:     { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 28 },
  title:      { fontSize: 28, fontWeight: 700, margin: 0 },
  newBtn:     { background: '#e94560', color: '#fff', border: 'none', padding: '10px 20px', borderRadius: 8, cursor: 'pointer', textDecoration: 'none', fontSize: 14, fontWeight: 600 },
  hint:       { color: '#aaa' },
  error:      { color: '#e94560' },
  empty:      { textAlign: 'center', padding: '60px 0', color: '#aaa', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16 },
  list:       { display: 'flex', flexDirection: 'column', gap: 14 },
  card:       { background: '#1a1a2e', borderRadius: 12, padding: 20 },
  cardTop:    { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 10, gap: 12 },
  product:    { fontWeight: 700, fontSize: 16, marginRight: 8 },
  type:       { fontSize: 12, color: '#aaa', background: '#0f0f1a', padding: '2px 8px', borderRadius: 10 },
  badge:      { flexShrink: 0, padding: '4px 12px', borderRadius: 12, fontSize: 12, fontWeight: 700, color: '#fff' },
  idRow:      { color: '#ccc', fontSize: 13, margin: '4px 0' },
  idLabel:    { color: '#666', fontSize: 12 },
  field:      { color: '#ccc', margin: '4px 0', fontSize: 14 },
  note:       { fontSize: 12, margin: '8px 0 4px', fontStyle: 'italic' },
  timeline:   { display: 'flex', gap: 16, marginTop: 10, flexWrap: 'wrap' },
  ts:         { fontSize: 11, color: '#666' },
  link:       { display: 'inline-block', marginTop: 10, color: '#e94560', textDecoration: 'none', fontSize: 13 },
}
