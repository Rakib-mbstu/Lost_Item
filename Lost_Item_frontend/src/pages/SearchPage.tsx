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

const typeToEnum: Record<string, string> = {
  'Mobile-IMEI':             'Mobile',
  'Bike-Engine/FrameNumber': 'Bike',
  'Laptop-Mac/serialNumber': 'Laptop',
}

const placeholderByType: Record<string, string> = {
  'Mobile-IMEI':             'e.g. 356938035643809',
  'Bike-Engine/FrameNumber': 'e.g. EN12345 / FR98765',
  'Laptop-Mac/serialNumber': 'e.g. C02XK0XHHTD6',
}

export default function SearchPage() {
  const [selectedType, setSelectedType] = useState('Mobile-IMEI')
  const [query, setQuery]   = useState('')
  const [result, setResult] = useState<SearchResult | null>(null)
  const [error, setError]   = useState('')
  const [loading, setLoading] = useState(false)

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
      if ((e.response?.status as number) === 404) {
        setError('No product found with that identifier')
      } else {
        setError(e.response?.data || 'No Result found')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-brand-bg pb-16">
      {/* Hero */}
      <div className="bg-brand-primary py-12 sm:py-16 px-4 text-center">
        <h1 className="text-white text-2xl sm:text-4xl font-bold mb-3">
          Check if a device is stolen
        </h1>
        <p className="text-blue-200 text-sm sm:text-base max-w-md mx-auto">
          Search our public registry by IMEI, frame number, or serial number
        </p>
      </div>

      {/* Search card */}
      <div className="max-w-xl mx-auto px-4 -mt-6">
        <div className="bg-brand-card rounded-2xl shadow-card-md border border-brand-border p-5 sm:p-6">
          <div className="flex flex-col gap-3">
            <select
              className="w-full px-3 py-3 rounded-lg border border-brand-border bg-brand-bg text-brand-text text-sm focus:outline-none focus:border-brand-primary focus:ring-1 focus:ring-brand-primary"
              value={selectedType}
              onChange={e => setSelectedType(e.target.value)}
            >
              <option value="Mobile-IMEI">Mobile — IMEI</option>
              <option value="Bike-Engine/FrameNumber">Bike — Engine / Frame Number</option>
              <option value="Laptop-Mac/serialNumber">Laptop — MAC / Serial Number</option>
            </select>

            <input
              className="w-full px-4 py-3 rounded-lg border border-brand-border bg-brand-bg text-brand-text text-sm focus:outline-none focus:border-brand-primary focus:ring-1 focus:ring-brand-primary placeholder-brand-muted"
              value={query}
              onChange={e => setQuery(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && search()}
              placeholder={placeholderByType[selectedType]}
              maxLength={100}
            />

            <button
              className="w-full py-3 bg-brand-primary text-white font-semibold rounded-lg hover:bg-blue-900 transition-colors disabled:opacity-50 border-0 cursor-pointer text-sm"
              onClick={search}
              disabled={loading}
            >
              {loading ? 'Searching…' : 'Search Registry'}
            </button>
          </div>

          {error && (
            <p className="mt-4 text-sm text-brand-muted flex items-center gap-2">
              <span className="text-brand-accent">⚠</span> {error}
            </p>
          )}
        </div>

        {/* Result card */}
        {result && (
          <div className={`mt-4 bg-brand-card rounded-2xl border shadow-card-md overflow-hidden
            ${result.isStolen ? 'border-brand-danger' : 'border-brand-border'}`}>

            {/* Stolen banner */}
            {result.isStolen && (
              <div className="bg-brand-danger px-5 py-3 flex items-center gap-2">
                <span className="text-white font-bold text-sm tracking-wide">⚠️ REPORTED STOLEN</span>
              </div>
            )}

            <div className="p-5">
              <div className="flex items-center gap-2 flex-wrap mb-3">
                <h2 className="text-brand-text text-xl font-bold m-0">{result.brand} {result.model}</h2>
                <span className="text-xs text-brand-muted bg-gray-100 border border-brand-border px-2.5 py-0.5 rounded-full font-medium">
                  {result.type}
                </span>
              </div>

              <p className="text-brand-muted text-xs mb-3">
                {identifierLabel(result.type)}: <span className="text-brand-text font-mono font-medium">{result.trackingId}</span>
              </p>

              {result.isStolen && result.openComplaints.length > 0 && (
                <div className="mt-4 border-t border-brand-border pt-4">
                  <p className="text-xs font-semibold text-brand-text uppercase tracking-wide mb-2">
                    {result.openComplaints.length} Stolen Report{result.openComplaints.length > 1 ? 's' : ''}
                  </p>
                  {result.openComplaints.map(c => (
                    <div key={c.id} className="bg-red-50 border border-red-100 rounded-lg p-3 mb-2">
                      <p className="text-brand-text text-sm mb-0.5">📍 {c.locationStolen}</p>
                      <p className="text-brand-muted text-xs">Reported: {new Date(c.createdAt).toLocaleDateString()}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
