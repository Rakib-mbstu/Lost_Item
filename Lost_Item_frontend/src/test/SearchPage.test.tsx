import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import React from 'react'

// Mock axios used by SearchPage
vi.mock('axios', () => ({
  default: {
    get: vi.fn(),
    create: vi.fn(),
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  }
}))

import axios from 'axios'
import SearchPage from '../pages/SearchPage'

const mockedAxios = axios as any

describe('SearchPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders search form elements', () => {
    render(<SearchPage />)
    expect(screen.getByPlaceholderText(/e\.g\. 356938035643809/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /search registry/i })).toBeInTheDocument()
  })

  it('renders device type select with all options', () => {
    render(<SearchPage />)
    const select = screen.getByRole('combobox')
    expect(select).toBeInTheDocument()
    expect(screen.getByText(/Mobile — IMEI/i)).toBeInTheDocument()
    expect(screen.getByText(/Bike — Engine \/ Frame Number/i)).toBeInTheDocument()
    expect(screen.getByText(/Laptop — MAC \/ Serial Number/i)).toBeInTheDocument()
  })

  it('shows no result initially', () => {
    render(<SearchPage />)
    expect(screen.queryByText(/REPORTED STOLEN/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/No product found/i)).not.toBeInTheDocument()
  })

  it('does not search when query is empty', async () => {
    render(<SearchPage />)
    fireEvent.click(screen.getByRole('button', { name: /search registry/i }))
    expect(mockedAxios.get).not.toHaveBeenCalled()
  })

  it('searches when Enter key is pressed', async () => {
    mockedAxios.get.mockResolvedValue({
      data: {
        productId: 1,
        trackingId: '123456789012345',
        type: 'Mobile',
        brand: 'Samsung',
        model: 'Galaxy S21',
        isStolen: true,
        openComplaints: [{ id: 1, locationStolen: 'Dhaka', createdAt: '2024-01-01T00:00:00Z' }]
      }
    })

    render(<SearchPage />)
    const input = screen.getByPlaceholderText(/e\.g\. 356938035643809/i)
    fireEvent.change(input, { target: { value: '123456789012345' } })
    fireEvent.keyDown(input, { key: 'Enter' })

    await waitFor(() => {
      expect(mockedAxios.get).toHaveBeenCalledWith('/api/search', {
        params: { trackingId: '123456789012345', type: 'Mobile' }
      })
    })
  })

  it('displays stolen banner when result isStolen is true', async () => {
    mockedAxios.get.mockResolvedValue({
      data: {
        productId: 1,
        trackingId: '111111111111111',
        type: 'Mobile',
        brand: 'Apple',
        model: 'iPhone 14',
        isStolen: true,
        openComplaints: [{ id: 1, locationStolen: 'Chittagong', createdAt: '2024-03-01T00:00:00Z' }]
      }
    })

    render(<SearchPage />)
    const input = screen.getByPlaceholderText(/e\.g\. 356938035643809/i)
    fireEvent.change(input, { target: { value: '111111111111111' } })
    fireEvent.click(screen.getByRole('button', { name: /search registry/i }))

    await waitFor(() => {
      expect(screen.getByText(/REPORTED STOLEN/i)).toBeInTheDocument()
      expect(screen.getByText('Apple iPhone 14')).toBeInTheDocument()
      expect(screen.getByText(/Chittagong/i)).toBeInTheDocument()
    })
  })

  it('shows error message when product is not found (404)', async () => {
    mockedAxios.get.mockRejectedValue({ response: { status: 404 } })

    render(<SearchPage />)
    const input = screen.getByPlaceholderText(/e\.g\. 356938035643809/i)
    fireEvent.change(input, { target: { value: 'NOTFOUND123' } })
    fireEvent.click(screen.getByRole('button', { name: /search registry/i }))

    await waitFor(() => {
      expect(screen.getByText(/No product found with that identifier/i)).toBeInTheDocument()
    })
  })

  it('shows error message on generic API error', async () => {
    mockedAxios.get.mockRejectedValue({
      response: { status: 500, data: 'Internal server error' }
    })

    render(<SearchPage />)
    const input = screen.getByPlaceholderText(/e\.g\. 356938035643809/i)
    fireEvent.change(input, { target: { value: 'ANY-ID' } })
    fireEvent.click(screen.getByRole('button', { name: /search registry/i }))

    await waitFor(() => {
      expect(screen.getByText(/Internal server error/i)).toBeInTheDocument()
    })
  })

  it('shows product info without stolen banner when isStolen is false', async () => {
    mockedAxios.get.mockResolvedValue({
      data: {
        productId: 5,
        trackingId: 'SN-LAPTOP-007',
        type: 'Laptop',
        brand: 'Dell',
        model: 'XPS 15',
        isStolen: false,
        openComplaints: []
      }
    })

    render(<SearchPage />)
    // Switch to Laptop type
    const select = screen.getByRole('combobox')
    fireEvent.change(select, { target: { value: 'Laptop-Mac/serialNumber' } })

    const input = screen.getByPlaceholderText(/e\.g\. C02XK0XHHTD6/i)
    fireEvent.change(input, { target: { value: 'SN-LAPTOP-007' } })
    fireEvent.click(screen.getByRole('button', { name: /search registry/i }))

    await waitFor(() => {
      expect(screen.getByText('Dell XPS 15')).toBeInTheDocument()
      expect(screen.queryByText(/REPORTED STOLEN/i)).not.toBeInTheDocument()
    })
  })

  it('changes placeholder text when device type changes', () => {
    render(<SearchPage />)
    const select = screen.getByRole('combobox')

    fireEvent.change(select, { target: { value: 'Bike-Engine/FrameNumber' } })
    expect(screen.getByPlaceholderText(/e\.g\. EN12345 \/ FR98765/i)).toBeInTheDocument()

    fireEvent.change(select, { target: { value: 'Laptop-Mac/serialNumber' } })
    expect(screen.getByPlaceholderText(/e\.g\. C02XK0XHHTD6/i)).toBeInTheDocument()
  })

  it('sends correct type parameter for Bike search', async () => {
    mockedAxios.get.mockResolvedValue({
      data: {
        productId: 2,
        trackingId: 'FRAME-001',
        type: 'Bike',
        brand: 'Honda',
        model: 'CB300',
        isStolen: true,
        openComplaints: [{ id: 2, locationStolen: 'Sylhet', createdAt: '2024-02-01T00:00:00Z' }]
      }
    })

    render(<SearchPage />)
    const select = screen.getByRole('combobox')
    fireEvent.change(select, { target: { value: 'Bike-Engine/FrameNumber' } })

    const input = screen.getByPlaceholderText(/EN12345/i)
    fireEvent.change(input, { target: { value: 'FRAME-001' } })
    fireEvent.click(screen.getByRole('button', { name: /search registry/i }))

    await waitFor(() => {
      expect(mockedAxios.get).toHaveBeenCalledWith('/api/search', {
        params: { trackingId: 'FRAME-001', type: 'Bike' }
      })
    })
  })

  it('trims whitespace from query before searching', async () => {
    mockedAxios.get.mockResolvedValue({
      data: {
        productId: 3,
        trackingId: '123456789012345',
        type: 'Mobile',
        brand: 'Test',
        model: 'Phone',
        isStolen: false,
        openComplaints: []
      }
    })

    render(<SearchPage />)
    const input = screen.getByPlaceholderText(/e\.g\. 356938035643809/i)
    fireEvent.change(input, { target: { value: '  123456789012345  ' } })
    fireEvent.click(screen.getByRole('button', { name: /search registry/i }))

    await waitFor(() => {
      expect(mockedAxios.get).toHaveBeenCalledWith('/api/search', {
        params: { trackingId: '123456789012345', type: 'Mobile' }
      })
    })
  })
})
