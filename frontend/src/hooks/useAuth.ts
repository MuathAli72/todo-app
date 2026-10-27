import { useEffect, useState } from 'react'
import { apiFetch, UNAUTHORIZED_EVENT } from '../api'
import { ErrorSchema, MeSchema } from '../schemas'

/**
 * Who is logged in, plus log in, register and log out.
 *
 * @remarks
 * The login itself is a cookie the browser keeps and sends automatically (see apiFetch).
 * This hook only tracks WHO is logged in, so the page knows what to show.
 * - `checking` is true while it asks the server "am I logged in?" when the page opens.
 * - `email` is null when nobody is logged in.
 * login and register return an error message to show, or null when they worked.
 */
export function useAuth() {
  const [email, setEmail] = useState<string | null>(null)
  const [checking, setChecking] = useState(true)

  useEffect(() => {
    // When the page opens: is there still a valid login cookie?
    apiFetch('/account/me')
      .then(async (response) => (response.ok ? MeSchema.parse(await response.json()).email : null))
      .catch(() => null)
      .then((current) => {
        setEmail(current)
        setChecking(false)
      })

    // Any 401 later on (e.g. the cookie expired) sends the user back to the login screen.
    const onUnauthorized = () => setEmail(null)
    window.addEventListener(UNAUTHORIZED_EVENT, onUnauthorized)
    return () => window.removeEventListener(UNAUTHORIZED_EVENT, onUnauthorized)
  }, [])

  async function send(path: string, newEmail: string, password: string): Promise<string | null> {
    try {
      const response = await apiFetch(path, {
        method: 'POST',
        body: JSON.stringify({ email: newEmail, password }),
      })
      if (response.ok) {
        setEmail(newEmail)
        return null
      }
      const body = ErrorSchema.safeParse(await response.json().catch(() => null))
      return body.success ? body.data.error : 'Something went wrong. Please try again.'
    } catch {
      return "Can't reach the server. Check your connection and try again."
    }
  }

  const login = (newEmail: string, password: string) => send('/account/login', newEmail, password)
  const register = (newEmail: string, password: string) => send('/account/register', newEmail, password)

  async function logout() {
    await apiFetch('/account/logout', { method: 'POST' }).catch(() => undefined)
    setEmail(null)
  }

  return { email, checking, login, register, logout }
}
