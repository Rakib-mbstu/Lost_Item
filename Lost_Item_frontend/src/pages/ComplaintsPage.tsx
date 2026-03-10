import { useState, useEffect } from 'react'
import { useAuth } from '../context/AuthContext'
import api from '../helper/api'

interface Complaint {
  id: number
  productId: number
  productTrackingId: string
  userName: string
  locationStolen: string
  policeReportUrl: string
  status: string
  createdAt: string
  resolvedAt?: string
}

type ProductType = 'Mobile' | 'Bike' | 'Laptop'

export default function ComplaintsPage() {
  const { user } = useAuth()
  const [complaints, setComplaints] = useState<Complaint[]>([])
  const [showForm, setShowForm] = useState(false)

  // Common
  const [productType, setProductType] = useState<ProductType>('Mobile')
  const [brand, setBrand] = useState('')
  const [model, setModel] = useState('')
  const [location, setLocation] = useState('')
  const [file, setFile] = useState<File | null>(null)

  // Mobile
  const [imei, setImei] = useState('')

  // Bike
  const [frameNumber, setFrameNumber] = useState('')
  const [engineNumber, setEngineNumber] = useState('')

  // Laptop
  const [serialNumber, setSerialNumber] = useState('')
  const [macAddress, setMacAddress] = useState('')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  const load = async () => {
    const { data } = await api.get('/complaints')
    setComplaints(data)
  }

  useEffect(() => { load() }, [])

  const resetForm = () => {
    setBrand(''); setModel(''); setLocation(''); setFile(null)
    setImei(''); setFrameNumber(''); setEngineNumber('')
    setSerialNumber(''); setMacAddress('')
    setProductType('Mobile')
  }

  const submit = async () => {
    if (!brand || !model || !location || !file) { setError('All fields required'); return }
    if (productType === 'Mobile' && !imei) { setError('IMEI required'); return }
    if (productType === 'Bike' && (!frameNumber || !engineNumber)) { setError('Frame number and engine number required'); return }
    if (productType === 'Laptop' && (!serialNumber || !macAddress)) { setError('Serial number and MAC address required'); return }

    setSubmitting(true)
    setError('')

    const form = new FormData()
    form.append('productType', productType)
    form.append('brand', brand)
    form.append('model', model)
    form.append('locationStolen', location)
    form.append('policeReport', file)
    if (productType === 'Mobile') form.append('imei', imei)
    if (productType === 'Bike') { form.append('frameNumber', frameNumber); form.append('engineNumber', engineNumber) }
    if (productType === 'Laptop') { form.append('serialNumber', serialNumber); form.append('macAddress', macAddress) }

    try {
      await api.post('/complaints', form)
      setShowForm(false)
      resetForm()
      load()
    } catch (e: any) {
      setError(e.response?.data || 'Failed to submit')
    } finally {
      setSubmitting(false)
    }
  }

  const resolve = async (id: number) => {
    await api.patch(`/complaints/${id}/resolve`)
    load()
  }

  return (
    <div style={s.page}>
      <div style={s.header}>
        <h1 style={s.title}>Complaints</h1>
        <button style={s.btn} onClick={() => setShowForm(!showForm)}>+ New Complaint</button>
      </div>

      {showForm && (
        <div style={s.form}>
          <h3 style={{ color: '#fff', marginBottom: 16 }}>Report Stolen Product</h3>

          {/* Product Type */}
          <div style={s.row}>
            {(['Mobile', 'Bike', 'Laptop'] as ProductType[]).map(t => (
              <button key={t} style={{ ...s.typeBtn, ...(productType === t ? s.typeBtnActive : {}) }}
                onClick={() => setProductType(t)}>{t}</button>
            ))}
          </div>

          {/* Common */}
          <input style={s.input} placeholder="Brand" value={brand} onChange={e => setBrand(e.target.value)} />
          <input style={s.input} placeholder="Model" value={model} onChange={e => setModel(e.target.value)} />

          {/* Type-specific */}
          {productType === 'Mobile' && (
            <input style={s.input} placeholder="IMEI" value={imei} onChange={e => setImei(e.target.value)} />
          )}
          {productType === 'Bike' && (<>
            <input style={s.input} placeholder="Frame Number" value={frameNumber} onChange={e => setFrameNumber(e.target.value)} />
            <input style={s.input} placeholder="Engine Number" value={engineNumber} onChange={e => setEngineNumber(e.target.value)} />
          </>)}
          {productType === 'Laptop' && (<>
            <input style={s.input} placeholder="Serial Number" value={serialNumber} onChange={e => setSerialNumber(e.target.value)} />
            <input style={s.input} placeholder="MAC Address" value={macAddress} onChange={e => setMacAddress(e.target.value)} />
          </>)}

          {/* Complaint fields */}
          <input style={s.input} placeholder="Location stolen" value={location} onChange={e => setLocation(e.target.value)} />
          <input type="file" accept=".pdf,.jpg,.jpeg,.png" style={s.input} onChange={e => setFile(e.target.files?.[0] || null)} />

          {error && <p style={s.error}>{error}</p>}
          <button style={s.btn} onClick={submit} disabled={submitting}>{submitting ? 'Submitting...' : 'Submit'}</button>
        </div>
      )}

      <div style={s.list}>
        {complaints.map(c => (
          <div key={c.id} style={s.card}>
            <div style={s.cardHeader}>
              <span style={s.trackingId}>{c.productTrackingId}</span>
              <span style={{ ...s.status, background: c.status === 'Open' ? '#e94560' : '#27ae60' }}>{c.status}</span>
            </div>
            <p style={s.field}>📍 {c.locationStolen}</p>
            <p style={s.field}>👤 {c.userName}</p>
            <p style={s.date}>{new Date(c.createdAt).toLocaleDateString()}</p>
            <div style={s.actions}>
              <a href={c.policeReportUrl} target="_blank" rel="noreferrer" style={s.link}>📄 Police Report</a>
              {c.status === 'Open' && (user?.isAdmin || true) && (
                <button style={s.resolveBtn} onClick={() => resolve(c.id)}>Mark Resolved</button>
              )}
            </div>
          </div>
        ))}
        {complaints.length === 0 && <p style={{ color: '#aaa' }}>No complaints yet.</p>}
      </div>
    </div>
  )
}

const s: Record<string, React.CSSProperties> = {
  page: { minHeight: '90vh', background: '#0f0f1a', color: '#fff', padding: 24, maxWidth: 800, margin: '0 auto' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 },
  title: { fontSize: 28, fontWeight: 700 },
  btn: { background: '#e94560', color: '#fff', border: 'none', padding: '10px 20px', borderRadius: 8, cursor: 'pointer' },
  form: { background: '#1a1a2e', padding: 24, borderRadius: 12, marginBottom: 24, display: 'flex', flexDirection: 'column', gap: 12 },
  input: { padding: '10px 14px', borderRadius: 8, border: '1px solid #333', background: '#0f0f1a', color: '#fff', fontSize: 14 },
  error: { color: '#e94560' },
  list: { display: 'flex', flexDirection: 'column', gap: 12 },
  card: { background: '#1a1a2e', borderRadius: 12, padding: 20 },
  cardHeader: { display: 'flex', justifyContent: 'space-between', marginBottom: 12 },
  trackingId: { fontWeight: 700, fontSize: 16 },
  status: { padding: '4px 10px', borderRadius: 12, fontSize: 12, fontWeight: 600 },
  field: { color: '#ccc', margin: '4px 0' },
  date: { color: '#666', fontSize: 12, margin: '4px 0' },
  actions: { display: 'flex', gap: 12, marginTop: 12 },
  link: { color: '#e94560', textDecoration: 'none' },
  resolveBtn: { background: 'transparent', border: '1px solid #27ae60', color: '#27ae60', padding: '4px 12px', borderRadius: 6, cursor: 'pointer' },
  row: { display: 'flex', gap: 8 },
  typeBtn: { flex: 1, padding: '8px 0', borderRadius: 8, border: '1px solid #333', background: '#0f0f1a', color: '#aaa', cursor: 'pointer', fontSize: 14 },
  typeBtnActive: { border: '1px solid #e94560', color: '#e94560', background: '#1a0a10' },
}