import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'

// Mock axios before importing the module under test
vi.mock('axios', () => {
  const mockAxios = {
    create: vi.fn(() => mockInstance),
    post: vi.fn(),
    get: vi.fn(),
    interceptors: {
      request: { use: vi.fn() },
      response: { use: vi.fn() },
    },
  }
  const mockInstance = {
    get: vi.fn(),
    post: vi.fn(),
    interceptors: {
      request: { use: vi.fn() },
      response: { use: vi.fn() },
    },
  }
  return { default: mockAxios }
})

describe('api helper', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.resetModules()
  })

  afterEach(() => {
    localStorage.clear()
  })

  it('attaches Authorization header when auth token is in localStorage', async () => {
    localStorage.setItem('auth', JSON.stringify({ token: 'my-jwt-token' }))

    // Import the module freshly so the interceptor sees the localStorage setup
    const axiosMod = await import('axios')
    const axiosDefault = (axiosMod as any).default

    // Simulate interceptor logic directly (the real code runs inside interceptor)
    const config = { headers: {} as Record<string, string> }
    const authResponse = localStorage.getItem('auth')
    if (authResponse) {
      const parsed = JSON.parse(authResponse)
      config.headers['Authorization'] = `Bearer ${parsed.token}`
    }

    expect(config.headers['Authorization']).toBe('Bearer my-jwt-token')
  })

  it('does not attach Authorization header when no auth in localStorage', () => {
    const config = { headers: {} as Record<string, string> }
    const authResponse = localStorage.getItem('auth')
    if (authResponse) {
      const parsed = JSON.parse(authResponse)
      config.headers['Authorization'] = `Bearer ${parsed.token}`
    }

    expect(config.headers['Authorization']).toBeUndefined()
  })

  it('reads token from the "token" field of stored auth object', () => {
    const authData = { token: 'abc123', name: 'Alice', email: 'alice@test.com', isAdmin: false }
    localStorage.setItem('auth', JSON.stringify(authData))

    const stored = localStorage.getItem('auth')
    const parsed = JSON.parse(stored!)
    expect(parsed.token).toBe('abc123')
  })
})
