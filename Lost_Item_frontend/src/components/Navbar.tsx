import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)

  const handleLogout = () => {
    logout()
    navigate('/')
    setOpen(false)
  }

  const linkClass = 'text-blue-100 hover:text-white text-sm font-medium no-underline transition-colors px-1 py-1'
  const ctaClass  = 'bg-brand-accent text-brand-text text-sm font-semibold px-4 py-2 rounded-lg no-underline hover:bg-amber-400 transition-colors border-0 cursor-pointer'

  return (
    <nav className="bg-brand-primary shadow-md">
      <div className="max-w-6xl mx-auto px-4 sm:px-6">
        <div className="flex items-center justify-between h-14">
          {/* Brand */}
          <Link to="/" className="text-white font-bold text-lg no-underline flex items-center gap-2">
            🔍 <span>StolenTracker</span>
          </Link>

          {/* Desktop links */}
          <div className="hidden sm:flex items-center gap-5">
            <Link to="/" className={linkClass}>Search</Link>
            {user && <Link to="/complaints" className={linkClass}>My Complaints</Link>}
            {user?.isAdmin && <Link to="/admin" className={linkClass}>Admin</Link>}
            {user
              ? <button onClick={handleLogout} className={ctaClass}>
                  Logout ({user.name})
                </button>
              : <Link to="/login" className={ctaClass}>Sign In</Link>
            }
          </div>

          {/* Hamburger */}
          <button
            className="sm:hidden p-2 text-blue-100 hover:text-white text-xl leading-none bg-transparent border-0 cursor-pointer"
            onClick={() => setOpen(o => !o)}
            aria-label="Toggle menu"
          >
            {open ? '✕' : '☰'}
          </button>
        </div>
      </div>

      {/* Mobile drawer */}
      {open && (
        <div className="sm:hidden border-t border-blue-800 bg-brand-primary px-4 pb-4 pt-2 flex flex-col gap-1">
          <Link to="/" className="py-2.5 text-blue-100 hover:text-white text-sm font-medium no-underline block"
            onClick={() => setOpen(false)}>Search</Link>
          {user && (
            <Link to="/complaints" className="py-2.5 text-blue-100 hover:text-white text-sm font-medium no-underline block"
              onClick={() => setOpen(false)}>My Complaints</Link>
          )}
          {user?.isAdmin && (
            <Link to="/admin" className="py-2.5 text-blue-100 hover:text-white text-sm font-medium no-underline block"
              onClick={() => setOpen(false)}>Admin</Link>
          )}
          <div className="pt-2">
            {user
              ? <button onClick={handleLogout} className={`${ctaClass} w-full`}>
                  Logout ({user.name})
                </button>
              : <Link to="/login" onClick={() => setOpen(false)}
                  className={`${ctaClass} block text-center`}>Sign In</Link>
            }
          </div>
        </div>
      )}
    </nav>
  )
}
