import { useState, useCallback, useRef } from 'react'

type ToastType = 'success' | 'error' | 'info'

interface ToastItem {
  id: number
  message: string
  type: ToastType
}

const TOAST_STYLE: Record<ToastType, string> = {
  success: 'border-l-brand-success bg-green-50 text-green-800',
  error:   'border-l-brand-danger bg-red-50 text-red-800',
  info:    'border-l-brand-primary bg-brand-subtle text-brand-primary',
}

// --- Rendered component ---

export function ToastContainer({ toasts }: { toasts: ToastItem[] }) {
  if (toasts.length === 0) return null
  return (
    <div className="fixed bottom-6 right-4 sm:right-6 flex flex-col gap-2 z-50">
      {toasts.map(t => (
        <div key={t.id}
          className={`border-l-4 shadow-card-md text-sm px-4 py-3 rounded-lg max-w-xs sm:max-w-sm font-medium ${TOAST_STYLE[t.type]}`}>
          {t.message}
        </div>
      ))}
    </div>
  )
}

// --- Hook ---

export function useToast() {
  const [toasts, setToasts] = useState<ToastItem[]>([])
  const nextId = useRef(0)

  const showToast = useCallback((message: string, type: ToastType = 'info') => {
    const id = nextId.current++
    setToasts(prev => [...prev, { id, message, type }])
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 3500)
  }, [])

  return { toasts, showToast }
}
