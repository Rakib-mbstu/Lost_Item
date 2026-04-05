import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../helper/api'

type ProductType = 'Mobile' | 'Bike' | 'Laptop'

export default function NewComplaintPage() {
  const navigate = useNavigate()

  const [productType, setProductType] = useState<ProductType>('Mobile')
  const [brand, setBrand] = useState('')
  const [model, setModel] = useState('')
  const [location, setLocation] = useState('')
  const [file, setFile] = useState<File | null>(null)

  const [imei, setImei] = useState('')
  const [frameNumber, setFrameNumber] = useState('')
  const [engineNumber, setEngineNumber] = useState('')
  const [serialNumber, setSerialNumber] = useState('')
  const [macAddress, setMacAddress] = useState('')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    if (!brand || !model || !location || !file) { setError('All fields required'); return }
    if (productType === 'Mobile' && !imei) { setError('IMEI required'); return }
    if (productType === 'Bike' && (!frameNumber || !engineNumber)) { setError('Frame number and engine number required'); return }
    if (productType === 'Laptop' && !serialNumber) { setError('Serial number required'); return }

    setSubmitting(true)
    setError('')

    const form = new FormData()
    form.append('productType', productType)
    form.append('brand', brand)
    form.append('model', model)
    form.append('locationStolen', location)
    form.append('policeReport', file)
    if (productType === 'Mobile') form.append('imei', imei)
    if (productType === 'Bike') {
      form.append('frameNumber', frameNumber)
      form.append('engineNumber', engineNumber)
    }
    if (productType === 'Laptop') {
      form.append('serialNumber', serialNumber)
      if (macAddress) form.append('macAddress', macAddress)
    }

    try {
      await api.post('/complaints', form)
      navigate('/complaints')
    } catch (e: any) {
      setError(e.response?.data || 'Failed to submit')
      setSubmitting(false)
    }
  }

  return (
    <div style={s.page}>
      <h1 style={s.title}>Report a Stolen Product</h1>
      <p style={s.sub}>After submission your complaint will be reviewed by an admin before appearing in the public registry.</p>

      <div style={s.form}>
        {/* Product type selector */}
        <label style={s.label}>Product Type</label>
        <div style={s.typeRow}>
          {(['Mobile', 'Bike', 'Laptop'] as ProductType[]).map(t => (
            <button key={t}
              style={{ ...s.typeBtn, ...(productType === t ? s.typeBtnActive : {}) }}
              onClick={() => setProductType(t)}>{t}</button>
          ))}
        </div>

        {/* Common fields */}
        <label style={s.label}>Brand</label>
        <input style={s.input} placeholder="e.g. Samsung" value={brand} onChange={e => setBrand(e.target.value)} />

        <label style={s.label}>Model</label>
        <input style={s.input} placeholder="e.g. Galaxy S21" value={model} onChange={e => setModel(e.target.value)} />

        {/* Type-specific identifiers */}
        {productType === 'Mobile' && (
          <>
            <label style={s.label}>IMEI</label>
            <input style={s.input} placeholder="15-digit IMEI number" value={imei} onChange={e => setImei(e.target.value)} />
          </>
        )}
        {productType === 'Bike' && (
          <>
            <label style={s.label}>Frame Number</label>
            <input style={s.input} placeholder="Frame number" value={frameNumber} onChange={e => setFrameNumber(e.target.value)} />
            <label style={s.label}>Engine Number</label>
            <input style={s.input} placeholder="Engine number" value={engineNumber} onChange={e => setEngineNumber(e.target.value)} />
          </>
        )}
        {productType === 'Laptop' && (
          <>
            <label style={s.label}>Serial Number</label>
            <input style={s.input} placeholder="Serial number" value={serialNumber} onChange={e => setSerialNumber(e.target.value)} />
            <label style={s.label}>MAC Address <span style={s.optional}>(optional)</span></label>
            <input style={s.input} placeholder="e.g. 00:1A:2B:3C:4D:5E" value={macAddress} onChange={e => setMacAddress(e.target.value)} />
          </>
        )}

        {/* Complaint fields */}
        <label style={s.label}>Location Stolen</label>
        <input style={s.input} placeholder="City, area, or address" value={location} onChange={e => setLocation(e.target.value)} />

        <label style={s.label}>Police Report <span style={s.hint}>(PDF, JPG, or PNG — max 10 MB)</span></label>
        <input type="file" accept=".pdf,.jpg,.jpeg,.png" style={s.input}
          onChange={e => setFile(e.target.files?.[0] || null)} />

        {error && <p style={s.error}>{error}</p>}

        <button style={s.btn} onClick={submit} disabled={submitting}>
          {submitting ? 'Submitting…' : 'Submit Complaint'}
        </button>
      </div>
    </div>
  )
}

const s: Record<string, React.CSSProperties> = {
  page:         { minHeight: '90vh', background: '#0f0f1a', color: '#fff', padding: 24, maxWidth: 640, margin: '0 auto' },
  title:        { fontSize: 28, fontWeight: 700, marginBottom: 8 },
  sub:          { color: '#aaa', fontSize: 14, marginBottom: 28 },
  form:         { background: '#1a1a2e', padding: 28, borderRadius: 12, display: 'flex', flexDirection: 'column', gap: 6 },
  label:        { color: '#ccc', fontSize: 13, fontWeight: 600, marginTop: 8 },
  optional:     { color: '#666', fontWeight: 400 },
  hint:         { color: '#666', fontWeight: 400, fontSize: 12 },
  typeRow:      { display: 'flex', gap: 8, marginBottom: 4 },
  typeBtn:      { flex: 1, padding: '9px 0', borderRadius: 8, border: '1px solid #333', background: '#0f0f1a', color: '#aaa', cursor: 'pointer', fontSize: 14 },
  typeBtnActive:{ border: '1px solid #e94560', color: '#e94560', background: '#1a0a10' },
  input:        { padding: '10px 14px', borderRadius: 8, border: '1px solid #333', background: '#0f0f1a', color: '#fff', fontSize: 14 },
  error:        { color: '#e94560', marginTop: 4 },
  btn:          { marginTop: 12, background: '#e94560', color: '#fff', border: 'none', padding: '12px 0', borderRadius: 8, cursor: 'pointer', fontWeight: 600, fontSize: 15 },
}
