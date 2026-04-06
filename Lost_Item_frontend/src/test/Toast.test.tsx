import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useToast, ToastContainer } from '../components/Toast'
import { render, screen } from '@testing-library/react'
import React from 'react'

describe('useToast', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('starts with empty toasts', () => {
    const { result } = renderHook(() => useToast())
    expect(result.current.toasts).toHaveLength(0)
  })

  it('adds a toast when showToast is called', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.showToast('Hello world', 'success')
    })

    expect(result.current.toasts).toHaveLength(1)
    expect(result.current.toasts[0].message).toBe('Hello world')
    expect(result.current.toasts[0].type).toBe('success')
  })

  it('removes the toast after 3500ms', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.showToast('Temporary', 'info')
    })

    expect(result.current.toasts).toHaveLength(1)

    act(() => {
      vi.advanceTimersByTime(3500)
    })

    expect(result.current.toasts).toHaveLength(0)
  })

  it('defaults to info type when no type provided', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.showToast('Default type')
    })

    expect(result.current.toasts[0].type).toBe('info')
  })

  it('can show multiple toasts at once', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.showToast('First', 'success')
      result.current.showToast('Second', 'error')
      result.current.showToast('Third', 'info')
    })

    expect(result.current.toasts).toHaveLength(3)
  })

  it('assigns unique ids to each toast', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.showToast('Toast A', 'success')
      result.current.showToast('Toast B', 'error')
    })

    const ids = result.current.toasts.map(t => t.id)
    expect(new Set(ids).size).toBe(2)
  })
})

describe('ToastContainer', () => {
  it('renders nothing when toasts array is empty', () => {
    const { container } = render(<ToastContainer toasts={[]} />)
    expect(container.firstChild).toBeNull()
  })

  it('renders success toast with correct message', () => {
    const toasts = [{ id: 1, message: 'Saved!', type: 'success' as const }]
    render(<ToastContainer toasts={toasts} />)
    expect(screen.getByText('Saved!')).toBeInTheDocument()
  })

  it('renders error toast with correct message', () => {
    const toasts = [{ id: 2, message: 'Something went wrong', type: 'error' as const }]
    render(<ToastContainer toasts={toasts} />)
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('renders multiple toasts', () => {
    const toasts = [
      { id: 1, message: 'First', type: 'success' as const },
      { id: 2, message: 'Second', type: 'error' as const },
    ]
    render(<ToastContainer toasts={toasts} />)
    expect(screen.getByText('First')).toBeInTheDocument()
    expect(screen.getByText('Second')).toBeInTheDocument()
  })
})
