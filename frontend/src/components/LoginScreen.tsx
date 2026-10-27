import { useState } from 'react'
import './LoginScreen.css'

type Props = {
  /** Returns an error message to show, or null when it worked. */
  onLogin: (email: string, password: string) => Promise<string | null>
  /** Returns an error message to show, or null when it worked. */
  onRegister: (email: string, password: string) => Promise<string | null>
}

/**
 * The log in / create account form, shown when nobody is logged in.
 *
 * @remarks
 * One form for both: a link switches between "Log in" and "Create account".
 * The server's error message ({ error }) is shown as is, e.g. "Wrong email or password."
 */
export function LoginScreen({ onLogin, onRegister }: Props) {
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
      <h1>{isRegister ? 'Create account' : 'Log in'}</h1>

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

        <button type="submit" disabled={busy}>
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
  )
}
