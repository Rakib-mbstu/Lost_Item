import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../helper/api'

type ProductType = 'Mobile' | 'Bike' | 'Laptop'

const inputClass = 'w-full px-4 py-2.5 rounded-lg border border-brand-border bg-white text-brand-text text-sm focus:outline-none focus:border-brand-primary focus:ring-1 focus:ring-brand-primary placeholder-brand-muted'
const labelClass = 'block text-brand-text text-xs font-semibold uppercase tracking-wide mt-4 mb-1'

export default function NewComplaintPage() {
  const navigate = useNavigate()

  const [productType, setProductType] = useState<ProductType>('Mobile')
  const [brand, setBrand]             = useState('')
  const [model, setModel]             = useState('')
  const [location, setLocation]       = useState('')
  const [file, setFile]               = useState<File | null>(null)

  const [imei, setImei]               = useState('')
  const [frameNumber, setFrameNumber] = useState('')
  const [engineNumber, setEngineNumber] = useState('')
  const [serialNumber, setSerialNumber] = useState('')
  const [macAddress, setMacAddress]   = useState('')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError]           = useState('')
  const [warning, setWarning]       = useState('')

  const submit = async () => {
    if (!brand || !model || !location || !file) { setError('All fields required'); return }
    if (productType === 'Mobile' && !imei) { setError('IMEI required'); return }
    if (productType === 'Bike' && (!frameNumber || !engineNumber)) { setError('Frame number and engine number required'); return }
    if (productType === 'Laptop' && !serialNumber) { setError('Serial number required'); return }

    setSubmitting(true)
    setError('')
    setWarning('')

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
      if (e.response?.status === 409) {
        setWarning(e.response.data)
      } else {
        setError(e.response?.data || 'Failed to submit')
      }
      setSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen bg-brand-bg pb-12">
      {/* Page header */}
      <div className="bg-brand-primary py-8 px-4">
        <div className="max-w-lg mx-auto">
          <h1 className="text-white text-2xl sm:text-3xl font-bold mb-1">Report a Stolen Product</h1>
          <p className="text-blue-200 text-sm">
            Your complaint will be reviewed by an admin before appearing in the public registry.
          </p>
        </div>
      </div>

      {/* Form */}
      <div className="max-w-lg mx-auto px-4 -mt-4">
        <div className="bg-brand-card rounded-2xl shadow-card-md border border-brand-border p-5 sm:p-7">

          {/* Product type */}
          <label className={labelClass}>Product Type</label>
          <div className="flex flex-wrap gap-2 mb-2">
            {(['Mobile', 'Bike', 'Laptop'] as ProductType[]).map(t => (
              <button key={t} onClick={() => setProductType(t)}
                className={`flex-1 min-w-[80px] py-2 rounded-lg border text-sm font-medium cursor-pointer transition-all
                  ${productType === t
                    ? 'border-brand-primary bg-brand-subtle text-brand-primary font-semibold'
                    : 'border-brand-border text-brand-muted bg-white hover:border-gray-400'}`}
              >{t}</button>
            ))}
          </div>

          {/* Common fields */}
          <label className={labelClass}>Brand</label>
          <input className={inputClass} placeholder="e.g. Samsung" maxLength={100}
            value={brand} onChange={e => setBrand(e.target.value)} />

          <label className={labelClass}>Model</label>
          <input className={inputClass} placeholder="e.g. Galaxy S21" maxLength={100}
            value={model} onChange={e => setModel(e.target.value)} />

          {/* Type-specific */}
          {productType === 'Mobile' && <>
            <label className={labelClass}>IMEI</label>
            <input className={inputClass} placeholder="15-digit IMEI number" maxLength={20}
              value={imei} onChange={e => setImei(e.target.value)} />
          </>}

          {productType === 'Bike' && <>
            <label className={labelClass}>Frame Number</label>
            <input className={inputClass} placeholder="Frame number" maxLength={50}
              value={frameNumber} onChange={e => setFrameNumber(e.target.value)} />
            <label className={labelClass}>Engine Number</label>
            <input className={inputClass} placeholder="Engine number" maxLength={50}
              value={engineNumber} onChange={e => setEngineNumber(e.target.value)} />
          </>}

          {productType === 'Laptop' && <>
            <label className={labelClass}>Serial Number</label>
            <input className={inputClass} placeholder="Serial number" maxLength={100}
              value={serialNumber} onChange={e => setSerialNumber(e.target.value)} />
            <label className={labelClass}>
              MAC Address <span className="text-brand-muted normal-case font-normal">(optional)</span>
            </label>
            <input className={inputClass} placeholder="e.g. 00:1A:2B:3C:4D:5E" maxLength={17}
              value={macAddress} onChange={e => setMacAddress(e.target.value)} />
          </>}

          {/* Complaint fields */}
          <label className={labelClass}>Location Stolen</label>
          <input className={inputClass} placeholder="City, area, or address" maxLength={200}
            value={location} onChange={e => setLocation(e.target.value)} />

          <label className={labelClass}>
            Police Report
            <span className="text-brand-muted normal-case font-normal ml-1">(PDF, JPG, or PNG — max 10 MB)</span>
          </label>
          <div className="mt-1 border border-brand-border rounded-lg p-2 bg-gray-50">
            <input type="file" accept=".pdf,.jpg,.jpeg,.png"
              className="w-full text-sm text-brand-muted file:mr-3 file:py-1.5 file:px-3 file:rounded-md file:border-0 file:text-xs file:font-semibold file:bg-brand-primary file:text-white hover:file:bg-blue-900 cursor-pointer"
              onChange={e => setFile(e.target.files?.[0] || null)} />
          </div>

          {warning && (
            <div className="mt-4 flex items-start gap-2 bg-amber-50 border border-amber-200 rounded-lg p-3">
              <span className="text-brand-accent text-base leading-none mt-0.5">⚠</span>
              <p className="text-amber-800 text-xs">{warning}</p>
            </div>
          )}
          {error && (
            <div className="mt-4 flex items-start gap-2 bg-red-50 border border-red-200 rounded-lg p-3">
              <span className="text-brand-danger text-base leading-none mt-0.5">✕</span>
              <p className="text-red-700 text-xs">{error}</p>
            </div>
          )}

          <button
            className="mt-6 w-full bg-brand-primary text-white font-semibold py-3 rounded-lg hover:bg-blue-900 transition-colors disabled:opacity-50 border-0 cursor-pointer text-sm"
            onClick={submit}
            disabled={submitting}
          >
            {submitting ? 'Submitting…' : 'Submit Complaint'}
          </button>
        </div>
      </div>
    </div>
  )
}
