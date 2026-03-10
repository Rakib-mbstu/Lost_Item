
// import { useGoogleLogin } from '@react-oauth/google'
// import { useNavigate } from 'react-router-dom'
// import { useAuth } from '../context/AuthContext'
// import axios from 'axios'

// export default function LoginPage() {
//   const { login } = useAuth()
//   const navigate = useNavigate()

//   const handleGoogleLogin = useGoogleLogin({
//   flow: 'auth-code', // recommended for backend validation
//   onSuccess: async (codeResponse) => {
//     try {
//       console.log('Google auth code:', codeResponse.code);
//       await login(codeResponse.code); // send to backend to exchange for ID token
//       navigate('/');
//     } catch (e) {
//       console.error('Login failed', e);
//     }
//   },
//     onError: (err) => console.error('Google login failed', err)
//   });

//   return (
//     <div style={s.page}>
//       <div style={s.card}>
//         <h1 style={s.title}>Sign In</h1>
//         <p style={s.sub}>Sign in with Google to report stolen products or manage your complaints</p>
//         <button style={s.googleBtn} onClick={() => handleGoogleLogin()}>
//           <img src="https://www.gstatic.com/firebasejs/ui/2.0.0/images/auth/google.svg" width={20} alt="Google" />
//           Continue with Google
//         </button>
//       </div>
//     </div>
//   )
// }

const s: Record<string, React.CSSProperties> = {
  page: { minHeight: '90vh', background: '#0f0f1a', display: 'flex', alignItems: 'center', justifyContent: 'center' },
  card: { background: '#1a1a2e', padding: 40, borderRadius: 16, textAlign: 'center', maxWidth: 400, width: '100%' },
  title: { color: '#fff', fontSize: 28, marginBottom: 8 },
  sub: { color: '#aaa', marginBottom: 32, lineHeight: 1.6 },
  googleBtn: { display: 'flex', alignItems: 'center', gap: 12, padding: '12px 24px', background: '#fff', color: '#333', border: 'none', borderRadius: 8, cursor: 'pointer', fontSize: 16, fontWeight: 500, margin: '0 auto' }
}

import { GoogleLogin } from '@react-oauth/google'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  return (
    <div style={s.page}>
      <div style={s.card}>
        <h1 style={s.title}>Sign In</h1>
        <p style={s.sub}>Sign in with Google to report stolen products</p>
        <GoogleLogin
          onSuccess={async (credentialResponse) => {
            try {
              await login(credentialResponse.credential!) // ← this is the ID token
              navigate('/')
            } catch (e) {
              console.error('Login failed', e)
            }
          }}
          onError={() => console.error('Google login failed')}
        />
      </div>
    </div>
  )
}