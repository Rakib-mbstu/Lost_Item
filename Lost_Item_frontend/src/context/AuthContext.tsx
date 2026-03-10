import { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import axios from 'axios'

interface AuthUser {
  token: string
  name: string
  email: string
  isAdmin: boolean
}

interface AuthContextType {
  user: AuthUser | null
  login: (googleToken: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextType>(null!)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const stored = localStorage.getItem('auth')
    return stored ? JSON.parse(stored) : null
  })

  const login = async (googleToken: string) => {
    const { data } = await axios.post('/api/auth/google', { idToken: googleToken })
    localStorage.setItem('auth', JSON.stringify(data))
    setUser(data)
  }

  const logout = () => {
    localStorage.removeItem('auth')
    setUser(null)
  }

  // useEffect(() => {
  //   if (user?.token) {
  //     axios.defaults.headers.common['Authorization'] = `Bearer ${user.token}`
  //   } else {
  //     delete axios.defaults.headers.common['Authorization']
  //   }
  // }, [user])

  return <AuthContext.Provider value={{ user, login, logout }}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)