import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { renderHook, act } from '@testing-library/react'
import React from 'react'

// Mock axios used inside AuthContext
vi.mock('axios', () => ({
  default: {
    post: vi.fn(),
    create: vi.fn(),
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  }
}))

import axios from 'axios'
import { AuthProvider, useAuth } from '../context/AuthContext'

const mockedAxios = axios as any

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('initializes with null user when localStorage is empty', () => {
    const { result } = renderHook(() => useAuth(), {
      wrapper: AuthProvider
    })

    expect(result.current.user).toBeNull()
  })

  it('initializes with stored user from localStorage', () => {
    const storedUser = { token: 'tok', name: 'Bob', email: 'bob@test.com', isAdmin: false }
    localStorage.setItem('auth', JSON.stringify(storedUser))

    const { result } = renderHook(() => useAuth(), {
      wrapper: AuthProvider
    })

    expect(result.current.user).not.toBeNull()
    expect(result.current.user?.name).toBe('Bob')
    expect(result.current.user?.email).toBe('bob@test.com')
  })

  it('login sets user and persists to localStorage', async () => {
    const mockUser = { token: 'new-tok', name: 'Alice', email: 'alice@test.com', isAdmin: true }
    mockedAxios.post.mockResolvedValue({ data: mockUser })

    const { result } = renderHook(() => useAuth(), {
      wrapper: AuthProvider
    })

    await act(async () => {
      await result.current.login('google-id-token')
    })

    expect(result.current.user?.name).toBe('Alice')
    expect(result.current.user?.isAdmin).toBe(true)
    const persisted = JSON.parse(localStorage.getItem('auth')!)
    expect(persisted.token).toBe('new-tok')
  })

  it('login calls the correct endpoint with the Google token', async () => {
    mockedAxios.post.mockResolvedValue({
      data: { token: 't', name: 'X', email: 'x@x.com', isAdmin: false }
    })

    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })

    await act(async () => {
      await result.current.login('my-google-token')
    })

    expect(mockedAxios.post).toHaveBeenCalledWith('/api/auth/google', { idToken: 'my-google-token' })
  })

  it('logout clears user and removes from localStorage', () => {
    const storedUser = { token: 'tok', name: 'Bob', email: 'bob@test.com', isAdmin: false }
    localStorage.setItem('auth', JSON.stringify(storedUser))

    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })

    expect(result.current.user).not.toBeNull()

    act(() => {
      result.current.logout()
    })

    expect(result.current.user).toBeNull()
    expect(localStorage.getItem('auth')).toBeNull()
  })

  it('isAdmin is correctly reflected in user state', async () => {
    mockedAxios.post.mockResolvedValue({
      data: { token: 'admin-tok', name: 'Admin', email: 'admin@test.com', isAdmin: true }
    })

    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })

    await act(async () => {
      await result.current.login('admin-google-token')
    })

    expect(result.current.user?.isAdmin).toBe(true)
  })
})
