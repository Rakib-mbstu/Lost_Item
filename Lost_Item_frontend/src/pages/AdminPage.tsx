import { useState, useEffect } from 'react'
import axios from 'axios'

interface Product {
  id: number
  trackingId: string
  type: string
  brand: string
  model: string
  extraFields: Record<string, string>
  createdAt: string
}

type ProductType = 'Mobile' | 'Bike' | 'Laptop'

export default function AdminPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [type, setType] = useState<ProductType>('Mobile')
  const [fields, setFields] = useState<Record<string, string>>({})
  const [error, setError] = useState('')

  const load = async () => {
    const { data } = await axios.get('/api/admin/products')
    setProducts(data)
  }

  useEffect(() => { load() }, [])

  const set = (k: string, v: string) => setFields(f => ({ ...f, [k]: v }))

  const submit = async () => {
    setError('')
    try {
      const endpoint = `/api/admin/products/${type.toLowerCase()}`
      await axios.post(endpoint, fields)
      setFields({})
      load()
    } catch (e: any) {
      setError(e.response?.data || 'Failed to create')
    }
  }

  const remove = async (id: number) => {
    if (!confirm('Delete this product?')) return
    await axios.delete(`/api/admin/products/${id}`)
    load()
  }

  const fieldDefs: Record<ProductType, { key: string; label: string }[]> = {
    Mobile: [{ key: 'brand', label: 'Brand' }, { key: 'model', label: 'Model' }, { key: 'imei', label: 'IMEI' }],
    Bike: [{ key: 'brand', label: 'Brand' }, { key: 'model', label: 'Model' }, { key: 'frameNumber', label: 'Frame Number' }, { key: 'color', label: 'Color' }],
    Laptop: [{ key: 'brand', label: 'Brand' }, { key: 'model', label: 'Model' }, { key: 'serialNumber', label: 'Serial Number' }, { key: 'macAddress', label: 'MAC Address (optional)' }]
  }

  return (
    <div style={s.page}>
      <h1 style={s.title}>Admin — Product Management</h1>

      <div style={s.form}>
        <h3 style={{ color: '#fff', marginBottom: 16 }}>Add Product</h3>
        <div style={s.typeRow}>
          {(['Mobile', 'Bike', 'Laptop'] as ProductType[]).map(t => (
            <button key={t} style={{ ...s.typeBtn, ...(type === t ? s.typeBtnActive : {}) }} onClick={() => { setType(t); setFields({}) }}>{t}</button>
          ))}
        </div>
        {fieldDefs[type].map(f => (
          <input key={f.key} style={s.input} placeholder={f.label} value={fields[f.key] || ''} onChange={e => set(f.key, e.target.value)} />
        ))}
        {error && <p style={s.error}>{error}</p>}
        <button style={s.btn} onClick={submit}>Add {type}</button>
      </div>

      <table style={s.table}>
        <thead>
          <tr>
            {['Tracking ID', 'Type', 'Brand', 'Model', 'Extra', 'Added', 'Actions'].map(h => (
              <th key={h} style={s.th}>{h}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {products.map(p => (
            <tr key={p.id}>
              <td style={s.td}>{p.trackingId}</td>
              <td style={s.td}>{p.type}</td>
              <td style={s.td}>{p.brand}</td>
              <td style={s.td}>{p.model}</td>
              <td style={s.td}>{Object.entries(p.extraFields).map(([k, v]) => `${k}: ${v}`).join(', ')}</td>
              <td style={s.td}>{new Date(p.createdAt).toLocaleDateString()}</td>
              <td style={s.td}><button style={s.delBtn} onClick={() => remove(p.id)}>Delete</button></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

const s: Record<string, React.CSSProperties> = {
  page: { minHeight: '90vh', background: '#0f0f1a', color: '#fff', padding: 24 },
  title: { fontSize: 28, fontWeight: 700, marginBottom: 24 },
  form: { background: '#1a1a2e', padding: 24, borderRadius: 12, marginBottom: 32, display: 'flex', flexDirection: 'column', gap: 12, maxWidth: 500 },
  typeRow: { display: 'flex', gap: 8, marginBottom: 8 },
  typeBtn: { padding: '8px 16px', borderRadius: 6, border: '1px solid #333', background: 'transparent', color: '#aaa', cursor: 'pointer' },
  typeBtnActive: { background: '#e94560', color: '#fff', border: '1px solid #e94560' },
  input: { padding: '10px 14px', borderRadius: 8, border: '1px solid #333', background: '#0f0f1a', color: '#fff', fontSize: 14 },
  btn: { background: '#e94560', color: '#fff', border: 'none', padding: '10px 20px', borderRadius: 8, cursor: 'pointer', alignSelf: 'flex-start' },
  error: { color: '#e94560' },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: 14 },
  th: { textAlign: 'left', padding: '10px 12px', borderBottom: '1px solid #333', color: '#aaa' },
  td: { padding: '10px 12px', borderBottom: '1px solid #1a1a2e', color: '#ccc' },
  delBtn: { background: 'transparent', border: '1px solid #e94560', color: '#e94560', padding: '4px 10px', borderRadius: 6, cursor: 'pointer' }
}