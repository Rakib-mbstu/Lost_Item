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

interface ComplaintUpdate {
  id: number
  message: string
  userName: string
  createdAt: string
}

function identifierLabel(type: string): string {
  switch (type) {
    case 'Mobile': return 'IMEI'
    case 'Bike':   return 'Frame No.'
    case 'Laptop': return 'Serial No.'
    default:       return 'ID'
  }
}

const STATUS_META: Record<Complaint['status'], {
  label: string; note: string
  badge: string; dot: string
}> = {
  Pending:  {
    label: 'Pending Review',
    note:  'Your complaint is waiting for admin review.',
    badge: 'bg-amber-100 text-amber-800 border border-amber-200',
    dot:   'bg-brand-accent',
  },
  Approved: {
    label: 'Approved',
    note:  'Approved — visible in the public stolen registry.',
    badge: 'bg-red-100 text-red-700 border border-red-200',
    dot:   'bg-brand-danger',
  },
  Rejected: {
    label: 'Rejected',
    note:  'An admin has dismissed this complaint.',
    badge: 'bg-gray-100 text-gray-600 border border-gray-200',
    dot:   'bg-gray-400',
  },
  Resolved: {
    label: 'Resolved',
    note:  'Marked as resolved — item recovered.',
    badge: 'bg-green-100 text-green-700 border border-green-200',
    dot:   'bg-brand-success',
  },
}

function ComplaintCard({ c }: { c: Complaint }) {
  const meta = STATUS_META[c.status] ?? STATUS_META.Pending
  const canUpdate = c.status === 'Pending' || c.status === 'Approved'

  const [expanded, setExpanded]   = useState(false)
  const [updates, setUpdates]     = useState<ComplaintUpdate[]>([])
  const [loadingUp, setLoadingUp] = useState(false)
  const [upError, setUpError]     = useState('')
  const [message, setMessage]     = useState('')
  const [posting, setPosting]     = useState(false)
  const [postError, setPostError] = useState('')

  const fetchUpdates = async () => {
    setLoadingUp(true); setUpError('')
    try {
      const { data } = await api.get(`/complaints/${c.id}/updates`)
      setUpdates(data)
    } catch {
      setUpError('Failed to load updates')
    } finally {
      setLoadingUp(false)
    }
  }

  const handleToggle = () => {
    if (!expanded) fetchUpdates()
    setExpanded(prev => !prev)
  }

  const handlePost = async () => {
    if (!message.trim()) return
    setPosting(true); setPostError('')
    try {
      await api.post(`/complaints/${c.id}/updates`, { message })
      setMessage('')
      fetchUpdates()
    } catch (e: any) {
      setPostError(e.response?.data || 'Failed to post update')
    } finally {
      setPosting(false)
    }
  }

  return (
    <div className="bg-brand-card rounded-xl shadow-card border border-brand-border overflow-hidden">
      {/* Card top */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 p-4 sm:p-5 border-b border-brand-border">
        <div className="flex items-center gap-2 flex-wrap">
          <div className={`w-2 h-2 rounded-full shrink-0 ${meta.dot}`} />
          <span className="text-brand-text font-bold text-base">{c.productBrand} {c.productModel}</span>
          <span className="text-xs text-brand-muted bg-gray-100 border border-brand-border px-2 py-0.5 rounded-full">
            {c.productType}
          </span>
        </div>
        <span className={`shrink-0 self-start sm:self-auto text-xs font-semibold px-3 py-1 rounded-full ${meta.badge}`}>
          {meta.label}
        </span>
      </div>

      <div className="p-4 sm:p-5">
        <p className="text-brand-muted text-xs mb-1">
          {identifierLabel(c.productType)}:
          <span className="text-brand-text font-mono font-medium ml-1">{c.productTrackingId}</span>
        </p>
        <p className="text-brand-text text-sm mb-2">📍 {c.locationStolen}</p>
        <p className="text-xs italic text-brand-muted mb-3">{meta.note}</p>

        <div className="flex flex-wrap gap-4 text-xs text-brand-muted border-t border-brand-border pt-3 mb-3">
          <span>Filed: {new Date(c.createdAt).toLocaleDateString()}</span>
          {c.reviewedAt && <span>Reviewed: {new Date(c.reviewedAt).toLocaleDateString()}</span>}
          {c.resolvedAt && <span>Resolved: {new Date(c.resolvedAt).toLocaleDateString()}</span>}
        </div>

        <div className="flex items-center gap-4">
          <a href={c.policeReportUrl} target="_blank" rel="noreferrer"
            className="inline-flex items-center gap-1.5 text-brand-primary text-xs font-medium no-underline hover:underline">
            📄 View Police Report
          </a>
          <button
            onClick={handleToggle}
            className="text-xs text-brand-primary font-medium hover:underline cursor-pointer border-0 bg-transparent p-0"
          >
            {expanded ? '▲ Hide Updates' : '▼ Updates'}
          </button>
        </div>

        {/* Expandable updates section */}
        {expanded && (
          <div className="border-t border-brand-border mt-3 pt-3">
            {loadingUp && <p className="text-brand-muted text-xs">Loading…</p>}
            {upError   && <p className="text-red-600 text-xs">{upError}</p>}

            {updates.length === 0 && !loadingUp && (
              <p className="text-brand-muted text-xs italic mb-3">No updates yet.</p>
            )}

            {updates.length > 0 && (
              <div className="flex flex-col gap-2 mb-3">
                {updates.map(u => (
                  <div key={u.id} className="bg-gray-50 border border-brand-border rounded-lg px-3 py-2">
                    <p className="text-brand-text text-sm">{u.message}</p>
                    <p className="text-brand-muted text-xs mt-1">
                      {u.userName} · {new Date(u.createdAt).toLocaleString()}
                    </p>
                  </div>
                ))}
              </div>
            )}

            {canUpdate && (
              <div className="flex flex-col gap-2">
                <textarea
                  value={message}
                  onChange={e => setMessage(e.target.value)}
                  maxLength={500}
                  rows={3}
                  placeholder="Write an update…"
                  className="w-full border border-brand-border rounded-lg px-3 py-2 text-sm resize-none focus:outline-none focus:border-brand-primary"
                />
                <div className="flex items-center justify-between">
                  <span className="text-brand-muted text-xs">{message.length}/500</span>
                  <button
                    onClick={handlePost}
                    disabled={posting || !message.trim()}
                    className="bg-brand-primary text-white text-xs font-semibold px-4 py-1.5 rounded-lg disabled:opacity-50 cursor-pointer hover:bg-blue-900 transition-colors"
                  >
                    {posting ? 'Posting…' : 'Post Update'}
                  </button>
                </div>
                {postError && <p className="text-red-600 text-xs">{postError}</p>}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

export default function MyComplaintsPage() {
  const [complaints, setComplaints] = useState<Complaint[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState('')

  useEffect(() => {
    api.get('/complaints/mine')
      .then(r => setComplaints(r.data))
      .catch(() => setError('Failed to load complaints'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div className="min-h-screen bg-brand-bg pb-12">
      {/* Header */}
      <div className="bg-brand-primary py-8 px-4">
        <div className="max-w-2xl mx-auto flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <h1 className="text-white text-2xl sm:text-3xl font-bold m-0">My Complaints</h1>
          <Link to="/complaints/new"
            className="bg-brand-accent text-brand-text text-sm font-semibold px-5 py-2.5 rounded-lg no-underline hover:bg-amber-400 transition-colors text-center">
            + File New Complaint
          </Link>
        </div>
      </div>

      <div className="max-w-2xl mx-auto px-4 mt-6">
        {loading && (
          <div className="text-center py-12 text-brand-muted">Loading…</div>
        )}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700 text-sm">{error}</div>
        )}

        {!loading && complaints.length === 0 && (
          <div className="text-center py-16 flex flex-col items-center gap-4">
            <div className="text-5xl">📋</div>
            <p className="text-brand-muted font-medium">No complaints filed yet.</p>
            <Link to="/complaints/new"
              className="bg-brand-primary text-white text-sm font-semibold px-6 py-2.5 rounded-lg no-underline hover:bg-blue-900 transition-colors">
              File your first complaint
            </Link>
          </div>
        )}

        <div className="flex flex-col gap-4">
          {complaints.map(c => <ComplaintCard key={c.id} c={c} />)}
        </div>
      </div>
    </div>
  )
}
