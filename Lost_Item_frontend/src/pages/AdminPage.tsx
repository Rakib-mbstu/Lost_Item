import { useState, useEffect, useCallback } from 'react'
import api from '../helper/api'
import { ToastContainer, useToast } from '../components/Toast'

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

const STATUS_BADGE: Record<string, string> = {
  Pending:  'bg-amber-100 text-amber-800 border border-amber-200',
  Approved: 'bg-red-100 text-red-700 border border-red-200',
  Resolved: 'bg-green-100 text-green-700 border border-green-200',
  Rejected: 'bg-gray-100 text-gray-500 border border-gray-200',
}

const FILTER_ACTIVE: Record<string, string> = {
  Pending:  'bg-amber-100 text-amber-800 border-amber-300',
  Approved: 'bg-red-100 text-red-700 border-red-300',
  Resolved: 'bg-green-100 text-green-700 border-green-300',
  Rejected: 'bg-gray-200 text-gray-600 border-gray-300',
}

const STAT_STYLE: Record<string, { value: string; bg: string }> = {
  Total:    { value: 'text-brand-primary', bg: 'bg-brand-subtle' },
  Pending:  { value: 'text-amber-600',     bg: 'bg-amber-50'     },
  Approved: { value: 'text-brand-danger',  bg: 'bg-red-50'       },
  Resolved: { value: 'text-brand-success', bg: 'bg-green-50'     },
  Rejected: { value: 'text-gray-400',      bg: 'bg-gray-50'      },
  Users:    { value: 'text-purple-600',    bg: 'bg-purple-50'    },
}

// --- Stats row ---

function StatsRow({ complaints, users, loading }: {
  complaints: Complaint[]; users: User[]; loading: boolean
}) {
  const stats = [
    { label: 'Total',    value: complaints.length },
    { label: 'Pending',  value: complaints.filter(c => c.status === 'Pending').length },
    { label: 'Approved', value: complaints.filter(c => c.status === 'Approved').length },
    { label: 'Resolved', value: complaints.filter(c => c.status === 'Resolved').length },
    { label: 'Rejected', value: complaints.filter(c => c.status === 'Rejected').length },
    { label: 'Users',    value: users.length },
  ]

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3 mb-6">
      {stats.map(({ label, value }) => {
        const st = STAT_STYLE[label]
        return (
          <div key={label} className={`rounded-xl border border-brand-border shadow-card px-4 py-4 flex flex-col gap-1 ${st.bg}`}>
            <span className={`text-3xl font-bold leading-none ${st.value}`}>
              {loading ? '—' : value}
            </span>
            <span className="text-xs text-brand-muted uppercase tracking-widest font-medium">{label}</span>
          </div>
        )
      })}
    </div>
  )
}

// --- Main component ---

export default function AdminPage() {
  const [tab, setTab] = useState<'complaints' | 'users'>('complaints')
  const [complaints, setComplaints] = useState<Complaint[]>([])
  const [users, setUsers]           = useState<User[]>([])
  const [loadingComplaints, setLoadingComplaints] = useState(false)
  const [loadingUsers, setLoadingUsers]           = useState(false)
  const [complaintsError, setComplaintsError] = useState('')
  const [usersError, setUsersError]           = useState('')
  const { toasts, showToast } = useToast()

  const loadComplaints = useCallback(async () => {
    setLoadingComplaints(true); setComplaintsError('')
    try { const { data } = await api.get('/complaints'); setComplaints(data) }
    catch { setComplaintsError('Failed to load complaints') }
    finally { setLoadingComplaints(false) }
  }, [])

  const loadUsers = useCallback(async () => {
    setLoadingUsers(true); setUsersError('')
    try { const { data } = await api.get('/auth/users'); setUsers(data) }
    catch { setUsersError('Failed to load users') }
    finally { setLoadingUsers(false) }
  }, [])

  useEffect(() => { loadComplaints(); loadUsers() }, [loadComplaints, loadUsers])

  return (
    <div className="min-h-screen bg-brand-bg pb-12">
      <ToastContainer toasts={toasts} />

      {/* Header */}
      <div className="bg-brand-primary py-8 px-4">
        <div className="max-w-5xl mx-auto">
          <h1 className="text-white text-2xl sm:text-3xl font-bold m-0">Admin Dashboard</h1>
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-4 mt-6">
        <StatsRow complaints={complaints} users={users} loading={loadingComplaints || loadingUsers} />

        {/* Tabs */}
        <div className="flex gap-2 mb-6 border-b border-brand-border pb-0">
          {(['complaints', 'users'] as const).map(t => (
            <button key={t} onClick={() => setTab(t)}
              className={`px-5 py-2.5 text-sm font-medium capitalize cursor-pointer border-0 transition-colors rounded-t-lg
                ${tab === t
                  ? 'bg-brand-primary text-white'
                  : 'bg-transparent text-brand-muted hover:text-brand-text'}`}
            >{t}</button>
          ))}
        </div>

        {tab === 'complaints'
          ? <ComplaintsTab complaints={complaints} loading={loadingComplaints}
              error={complaintsError} reload={loadComplaints} showToast={showToast} />
          : <UsersTab users={users} loading={loadingUsers}
              error={usersError} reload={loadUsers} showToast={showToast} />
        }
      </div>
    </div>
  )
}

// --- Complaints tab ---

function ComplaintsTab({ complaints, loading, error, reload, showToast }: {
  complaints: Complaint[]; loading: boolean; error: string
  reload: () => void; showToast: (msg: string, type?: 'success' | 'error' | 'info') => void
}) {
  const [filter, setFilter] = useState<StatusFilter>('Pending')

  const action = async (id: number, endpoint: 'approve' | 'reject' | 'resolve') => {
    try {
      await api.patch(`/complaints/${id}/${endpoint}`)
      reload()
      showToast(`Complaint ${endpoint}d`, 'success')
    } catch (e: any) {
      showToast(e.response?.data || `Failed to ${endpoint}`, 'error')
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
      {/* Filter pills */}
      <div className="flex flex-wrap gap-2 mb-5">
        {(['Pending', 'Approved', 'Resolved', 'Rejected'] as StatusFilter[]).map(f => (
          <button key={f} onClick={() => setFilter(f)}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full border text-xs font-medium cursor-pointer transition-colors
              ${filter === f ? FILTER_ACTIVE[f] : 'border-brand-border text-brand-muted bg-white hover:border-gray-400 hover:text-brand-text'}`}
          >
            {f}
            {counts[f] > 0 && (
              <span className={`text-xs font-bold px-1.5 py-0.5 rounded-full leading-none
                ${filter === f ? 'bg-white/60' : 'bg-gray-100 text-brand-muted'}`}>
                {counts[f]}
              </span>
            )}
          </button>
        ))}
      </div>

      {loading && <p className="text-brand-muted py-5 text-sm">Loading…</p>}
      {error   && <div className="bg-red-50 border border-red-200 rounded-lg p-3 text-red-700 text-sm mb-4">{error}</div>}

      <div className="flex flex-col gap-3">
        {visible.length === 0 && !loading && (
          <div className="text-center py-12 text-brand-muted text-sm bg-brand-card rounded-xl border border-brand-border">
            No {filter.toLowerCase()} complaints.
          </div>
        )}
        {visible.map(c => (
          <div key={c.id} className="bg-brand-card rounded-xl border border-brand-border shadow-card overflow-hidden">
            {/* Card header */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 px-4 sm:px-5 py-3 border-b border-brand-border bg-gray-50">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="text-brand-text font-bold text-sm">{c.productBrand} {c.productModel}</span>
                <span className="text-xs text-brand-muted bg-white border border-brand-border px-2 py-0.5 rounded-full">{c.productType}</span>
                <span className={`text-xs font-semibold px-2.5 py-0.5 rounded-full ${STATUS_BADGE[c.status]}`}>{c.status}</span>
              </div>
              <span className="text-brand-muted text-xs shrink-0">{new Date(c.createdAt).toLocaleDateString()}</span>
            </div>

            <div className="px-4 sm:px-5 py-4">
              <p className="text-brand-muted text-xs mb-1">
                {identifierLabel(c.productType)}:
                <code className="text-brand-primary font-mono text-xs font-medium ml-1">{c.productTrackingId}</code>
              </p>
              <p className="text-brand-text text-sm mb-1">👤 {c.userName} <span className="text-brand-muted text-xs">{c.userEmail}</span></p>
              <p className="text-brand-text text-sm mb-2">📍 {c.locationStolen}</p>
              {(c.reviewedAt || c.resolvedAt) && (
                <div className="flex flex-wrap gap-3 text-xs text-brand-muted mb-2">
                  {c.reviewedAt && <span>Reviewed: {new Date(c.reviewedAt).toLocaleDateString()}</span>}
                  {c.resolvedAt && <span>Resolved: {new Date(c.resolvedAt).toLocaleDateString()}</span>}
                </div>
              )}

              <div className="flex flex-wrap gap-2 mt-3 pt-3 border-t border-brand-border">
                <a href={c.policeReportUrl} target="_blank" rel="noreferrer"
                  className="text-brand-primary text-xs font-medium no-underline hover:underline">
                  📄 Police Report
                </a>
                {c.status === 'Pending' && <>
                  <button onClick={() => action(c.id, 'approve')}
                    className="border border-green-300 text-green-700 bg-green-50 text-xs px-3 py-1 rounded-md cursor-pointer hover:bg-green-100 transition-colors font-medium">
                    ✓ Approve
                  </button>
                  <button onClick={() => action(c.id, 'reject')}
                    className="border border-gray-300 text-gray-500 bg-gray-50 text-xs px-3 py-1 rounded-md cursor-pointer hover:bg-gray-100 transition-colors font-medium">
                    ✕ Reject
                  </button>
                </>}
                {c.status === 'Approved' && (
                  <button onClick={() => action(c.id, 'resolve')}
                    className="border border-blue-200 text-brand-primary bg-brand-subtle text-xs px-3 py-1 rounded-md cursor-pointer hover:bg-blue-100 transition-colors font-medium">
                    Mark Resolved
                  </button>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

// --- Users tab ---

function UsersTab({ users, loading, error, reload, showToast }: {
  users: User[]; loading: boolean; error: string
  reload: () => void; showToast: (msg: string, type?: 'success' | 'error' | 'info') => void
}) {
  const toggleAdmin = async (user: User) => {
    try {
      await api.patch(`/auth/users/${user.id}/make-admin`, { isAdmin: !user.isAdmin })
      reload()
      showToast(`${user.name} is ${!user.isAdmin ? 'now an admin' : 'no longer an admin'}`, 'success')
    } catch (e: any) {
      showToast(e.response?.data || 'Failed to update user', 'error')
    }
  }

  return (
    <div>
      {loading && <p className="text-brand-muted py-5 text-sm">Loading…</p>}
      {error   && <div className="bg-red-50 border border-red-200 rounded-lg p-3 text-red-700 text-sm mb-4">{error}</div>}
      <div className="bg-brand-card rounded-xl border border-brand-border shadow-card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm border-collapse">
            <thead>
              <tr className="bg-gray-50 border-b border-brand-border">
                {['Name', 'Email', 'Joined', 'Role', ''].map(h => (
                  <th key={h}
                    className={`text-left px-4 py-3 text-xs font-semibold text-brand-muted uppercase tracking-wide
                      ${h === 'Joined' ? 'hidden sm:table-cell' : ''}`}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-brand-border">
              {users.map(u => (
                <tr key={u.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 text-brand-text font-medium text-sm">{u.name}</td>
                  <td className="px-4 py-3 text-brand-muted text-xs">{u.email}</td>
                  <td className="px-4 py-3 text-brand-muted text-xs hidden sm:table-cell">
                    {new Date(u.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3">
                    {u.isAdmin
                      ? <span className="inline-flex items-center gap-1 text-xs font-semibold text-brand-primary bg-brand-subtle border border-blue-200 px-2 py-0.5 rounded-full">Admin</span>
                      : <span className="text-xs text-brand-muted">User</span>
                    }
                  </td>
                  <td className="px-4 py-3">
                    <button onClick={() => toggleAdmin(u)}
                      className={`border text-xs px-3 py-1 rounded-md cursor-pointer font-medium transition-colors
                        ${u.isAdmin
                          ? 'border-gray-300 text-gray-500 hover:bg-gray-50'
                          : 'border-blue-200 text-brand-primary bg-brand-subtle hover:bg-blue-100'}`}>
                      {u.isAdmin ? 'Revoke Admin' : 'Make Admin'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
