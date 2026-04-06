import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { GoogleOAuthProvider } from '@react-oauth/google'
import { AuthProvider, useAuth } from './context/AuthContext'
import Navbar from './components/Navbar'
import Footer from './components/Footer'
import SearchPage from './pages/SearchPage'
import LoginPage from './pages/LoginPage'
import MyComplaintsPage from './pages/MyComplaintsPage'
import NewComplaintPage from './pages/NewComplaintPage'
import AdminPage from './pages/AdminPage'

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID

function ProtectedRoute({ children, adminOnly = false }: { children: JSX.Element, adminOnly?: boolean }) {
  const { user } = useAuth()
  if (!user) return <Navigate to="/login" />
  if (adminOnly && !user.isAdmin) return <Navigate to="/" />
  return children
}

export default function App() {
  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <AuthProvider>
        <BrowserRouter>
          <div className="flex flex-col min-h-screen">
            <Navbar />
            <main className="flex-1">
              <Routes>
                <Route path="/" element={<SearchPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/complaints" element={
                  <ProtectedRoute><MyComplaintsPage /></ProtectedRoute>
                } />
                <Route path="/complaints/new" element={
                  <ProtectedRoute><NewComplaintPage /></ProtectedRoute>
                } />
                <Route path="/admin" element={
                  <ProtectedRoute adminOnly><AdminPage /></ProtectedRoute>
                } />
              </Routes>
            </main>
            <Footer />
          </div>
        </BrowserRouter>
      </AuthProvider>
    </GoogleOAuthProvider>
  )
}