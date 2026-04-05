import { useState, useEffect } from 'react'
import api from '../helper/api'

// --- Types ---

interface Complaint {
  id: number
  productTrackingId: string
  productBrand: string
  productModel: string
  productType: string
  userName: string
  userEmail: string
  locationStolen: string
  policeReportUrl: string
  status: 'Pending' | 'Approved' | 'Resolved' | 'Rejected'
  createdAt: string
  reviewedAt?: string
  resolvedAt?: string
}

interface User {
  id: number
  name: string
  email: string
  isAdmin: boolean
  createdAt: string
}

type StatusFilter = 'Pending' | 'Approved' | 'Resolved' | 'Rejected'

// --- Helpers ---

function identifierLabel(type: string): string {
  switch (type) {
    case 'Mobile': return 'IMEI'
    case 'Bike':   return 'Frame No.'
    case 'Laptop': return 'Serial No.'
    default:       return 'ID'
  }
}

function statusColor(status: string): string {
  switch (status) {
    case 'Approved': return '#e94560'
    case 'Resolved': return '#27ae60'
    case 'Rejected': return '#888'
    default:         return '#f39c12'
  }
}

// --- Main component ---

export default function AdminPage() {
  const [tab, setTab] = useState<'complaints' | 'users'>('complaints')

  return (
    <div style={s.page}>
      <h1 style={s.title}>Admin Dashboard</h1>
      <div style={s.tabs}>
        <button style={{ ...s.tab, ...(tab === 'complaints' ? s.tabActive : {}) }}
          onClick={() => setTab('complaints')}>Complaints</button>
        <button style={{ ...s.tab, ...(tab === 'users' ? s.tabActive : {}) }}
          onClick={() => setTab('users')}>Users</button>
      </div>
      {tab === 'complaints' ? <ComplaintsTab /> : <UsersTab />}
    </div>
  )
}

// --- Complaints tab ---

function ComplaintsTab() {
  const [complaints, setComplaints] = useState<Complaint[]>([])
  const [filter, setFilter] = useState<StatusFilter>('Pending')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const load = async () => {
    setLoading(true)
    setError('')
    try {
      const { data } = await api.get('/complaints')
      setComplaints(data)
    } catch {
      setError('Failed to load complaints')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const action = async (id: number, endpoint: 'approve' | 'reject' | 'resolve') => {
    try {
      await api.patch(`/complaints/${id}/${endpoint}`)
      load()
    } catch (e: any) {
      alert(e.response?.data || `Failed to ${endpoint}`)
    }
  }

  const visible = complaints.filter(c => c.status === filter)
  const counts: Record<StatusFilter, number> = {
    Pending:  complaints.filter(c => c.status === 'Pending').length,
    Approved: complaints.filter(c => c.status === 'Approved').length,
    Resolved: complaints.filter(c => c.status === 'Resolved').length,
    Rejected: complaints.filter(c => c.status === 'Rejected').length,
  }

  return (
    <div>
      <div style={s.filterRow}>
        {(['Pending', 'Approved', 'Resolved', 'Rejected'] as StatusFilter[]).map(f => (
          <button key={f}
            style={{ ...s.filterBtn, ...(filter === f ? { borderColor: statusColor(f), color: statusColor(f) } : {}) }}
            onClick={() => setFilter(f)}>
            {f} {counts[f] > 0 && <span style={{ ...s.badge, background: statusColor(f) }}>{counts[f]}</span>}
          </button>
        ))}
      </div>

      {loading && <p style={s.hint}>Loading…</p>}
      {error && <p style={s.error}>{error}</p>}

      <div style={s.list}>
        {visible.length === 0 && !loading && <p style={s.hint}>No {filter.toLowerCase()} complaints.</p>}
        {visible.map(c => (
          <div key={c.id} style={s.card}>
            <div style={s.cardTop}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                <span style={s.trackingId}>{c.productBrand} {c.productModel}</span>
                <span style={s.typeChip}>{c.productType}</span>
                <span style={{ ...s.statusBadge, background: statusColor(c.status) }}>{c.status}</span>
              </div>
              <span style={s.date}>{new Date(c.createdAt).toLocaleDateString()}</span>
            </div>
            <p style={s.field}><span style={s.idLabel}>{identifierLabel(c.productType)}:</span> <code style={s.code}>{c.productTrackingId}</code></p>
            <p style={s.field}>👤 {c.userName} <span style={s.email}>{c.userEmail}</span></p>
            <p style={s.field}>📍 {c.locationStolen}</p>
            {c.reviewedAt && <p style={s.meta}>Reviewed: {new Date(c.reviewedAt).toLocaleDateString()}</p>}
            {c.resolvedAt && <p style={s.meta}>Resolved: {new Date(c.resolvedAt).toLocaleDateString()}</p>}
            <div style={s.actions}>
              <a href={c.policeReportUrl} target="_blank" rel="noreferrer" style={s.link}>📄 Police Report</a>
              {c.status === 'Pending' && <>
                <button style={{ ...s.actionBtn, borderColor: '#27ae60', color: '#27ae60' }}
                  onClick={() => action(c.id, 'approve')}>Approve</button>
                <button style={{ ...s.actionBtn, borderColor: '#888', color: '#888' }}
                  onClick={() => action(c.id, 'reject')}>Reject</button>
              </>}
              {c.status === 'Approved' && (
                <button style={{ ...s.actionBtn, borderColor: '#e94560', color: '#e94560' }}
                  onClick={() => action(c.id, 'resolve')}>Mark Resolved</button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

// --- Users tab ---

function UsersTab() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const load = async () => {
    setLoading(true)
    setError('')
    try {
      const { data } = await api.get('/auth/users')
      setUsers(data)
    } catch {
      setError('Failed to load users')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const toggleAdmin = async (user: User) => {
    try {
      await api.patch(`/auth/users/${user.id}/make-admin`, { isAdmin: !user.isAdmin })
      load()
    } catch (e: any) {
      alert(e.response?.data || 'Failed to update user')
    }
  }

  return (
    <div>
      {loading && <p style={s.hint}>Loading…</p>}
      {error && <p style={s.error}>{error}</p>}
      <table style={s.table}>
        <thead>
          <tr>
            {['Name', 'Email', 'Joined', 'Admin', ''].map(h => (
              <th key={h} style={s.th}>{h}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {users.map(u => (
            <tr key={u.id}>
              <td style={s.td}>{u.name}</td>
              <td style={s.td}>{u.email}</td>
              <td style={s.td}>{new Date(u.createdAt).toLocaleDateString()}</td>
              <td style={s.td}>
                <span style={{ color: u.isAdmin ? '#27ae60' : '#888' }}>
                  {u.isAdmin ? 'Yes' : 'No'}
                </span>
              </td>
              <td style={s.td}>
                <button style={{ ...s.actionBtn, borderColor: u.isAdmin ? '#888' : '#e94560', color: u.isAdmin ? '#888' : '#e94560' }}
                  onClick={() => toggleAdmin(u)}>
                  {u.isAdmin ? 'Revoke Admin' : 'Make Admin'}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

// --- Styles ---

const s: Record<string, React.CSSProperties> = {
  page:        { minHeight: '90vh', background: '#0f0f1a', color: '#fff', padding: 24, maxWidth: 900, margin: '0 auto' },
  title:       { fontSize: 28, fontWeight: 700, marginBottom: 20 },
  tabs:        { display: 'flex', gap: 8, marginBottom: 24, borderBottom: '1px solid #333', paddingBottom: 12 },
  tab:         { padding: '8px 20px', borderRadius: 8, border: '1px solid #333', background: 'transparent', color: '#aaa', cursor: 'pointer', fontSize: 14 },
  tabActive:   { background: '#e94560', color: '#fff', border: '1px solid #e94560' },
  filterRow:   { display: 'flex', gap: 8, marginBottom: 20, flexWrap: 'wrap' },
  filterBtn:   { padding: '6px 14px', borderRadius: 20, border: '1px solid #444', background: 'transparent', color: '#aaa', cursor: 'pointer', fontSize: 13, display: 'flex', alignItems: 'center', gap: 6 },
  badge:       { display: 'inline-block', padding: '1px 7px', borderRadius: 10, fontSize: 11, fontWeight: 700, color: '#fff' },
  list:        { display: 'flex', flexDirection: 'column', gap: 12 },
  card:        { background: '#1a1a2e', borderRadius: 12, padding: 18 },
  cardTop:     { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 10 },
  trackingId:  { fontWeight: 700, fontSize: 15 },
  typeChip:    { fontSize: 11, color: '#aaa', background: '#0f0f1a', padding: '2px 8px', borderRadius: 10 },
  statusBadge: { display: 'inline-block', padding: '3px 10px', borderRadius: 12, fontSize: 11, fontWeight: 700, color: '#fff' },
  date:        { color: '#666', fontSize: 12 },
  field:       { color: '#ccc', margin: '3px 0', fontSize: 14 },
  meta:        { color: '#666', fontSize: 12, margin: '2px 0' },
  actions:     { display: 'flex', gap: 10, marginTop: 12, flexWrap: 'wrap' },
  link:        { color: '#e94560', textDecoration: 'none', fontSize: 13 },
  actionBtn:   { background: 'transparent', border: '1px solid', padding: '4px 12px', borderRadius: 6, cursor: 'pointer', fontSize: 13 },
  idLabel:     { color: '#666', fontSize: 12 },
  code:        { fontFamily: 'monospace', fontSize: 12, color: '#e94560' },
  email:       { color: '#888', fontSize: 12 },
  hint:        { color: '#aaa', padding: '20px 0' },
  error:       { color: '#e94560' },
  table:       { width: '100%', borderCollapse: 'collapse', fontSize: 14 },
  th:          { textAlign: 'left', padding: '10px 12px', borderBottom: '1px solid #333', color: '#aaa' },
  td:          { padding: '10px 12px', borderBottom: '1px solid #1a1a2e', color: '#ccc' },
}
