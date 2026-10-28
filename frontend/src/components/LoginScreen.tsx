import { useState } from 'react'
import type { ThemeName } from '../hooks/useTheme'
import { ThemeScene } from './scenes/ThemeScene'
import './LoginScreen.css'

type Props = {
  /** The theme being shown. Picks the illustrated scene at the top. */
  theme: ThemeName
  /** Returns an error message to show, or null when it worked. */
  onLogin: (email: string, password: string) => Promise<string | null>
  /** Returns an error message to show, or null when it worked. */
  onRegister: (email: string, password: string) => Promise<string | null>
}

/**
 * The landing page: the theme's illustrated scene, then the log in / create account form.
 *
 * @remarks
 * The scene is interactive (hover and click things in it); see components/scenes.
 * One form for both: a link switches between "Log in" and "Create account".
 * The server's error message ({ error }) is shown as is, e.g. "Wrong email or password."
 */
export function LoginScreen({ theme, onLogin, onRegister }: Props) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const isRegister = mode === 'register'

  async function submit() {
    setError(null)
    // Same rule as the server, checked here first for instant feedback.
    if (isRegister && password.length < 8) {
      setError('The password needs at least 8 characters.')
      return
    }
    setBusy(true)
    const problem = await (isRegister ? onRegister : onLogin)(email.trim(), password)
    setBusy(false)
    setError(problem)
  }

  return (
    <div className="app login-screen">
      <div className="login-card">
        <ThemeScene theme={theme} />

        <div className="login-body">
          <h1 className="login-title">To-Do App</h1>
          <p className="login-pitch">Tasks and routines, one day at a time.</p>
          <h2>{isRegister ? 'Create account' : 'Log in'}</h2>

          <form
            className="login-form"
            onSubmit={(event) => {
              event.preventDefault()
              void submit()
            }}
          >
            <label>
              Email
              <input type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
            </label>
            <label>
              Password
              <input
                type="password"
                autoComplete={isRegister ? 'new-password' : 'current-password'}
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </label>

            {error && <p className="login-error" role="alert">{error}</p>}

            <button type="submit" className="primary" disabled={busy}>
              {busy ? 'Please wait...' : isRegister ? 'Create account' : 'Log in'}
            </button>
          </form>

          <button
            type="button"
            className="login-switch"
            onClick={() => {
              setMode(isRegister ? 'login' : 'register')
              setError(null)
            }}
          >
            {isRegister ? 'Already have an account? Log in' : 'No account yet? Create one'}
          </button>
        </div>
      </div>
    </div>
  )
}
