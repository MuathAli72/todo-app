import { API_URL } from './config'

/** The user's time zone, like "Asia/Riyadh". The browser knows it from the device settings. */
const TimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone

/**
 * The name of the browser event sent when the server answers 401 (not logged in).
 * useAuth listens for it and shows the login screen, e.g. when the login cookie has expired.
 */
export const UNAUTHORIZED_EVENT = 'todo:unauthorized'

/**
 * fetch, plus what every request to our server needs.
 *
 * @remarks
 * - Adds the server address, so callers only write the path ("/tasks").
 * - Sends the user's time zone in the X-Time-Zone header, so the server knows
 *   what "today" is for this user, not for the server's own clock.
 * - Marks bodies as JSON, because every body we send is JSON.
 * - Sends the login cookie with every request (credentials: 'include').
 * - On a 401, tells the rest of the app that the user is logged out.
 *
 * @param path - The path on the server, like "/tasks" or "/routines/123/title".
 * @param options - The usual fetch options: method, body, and so on.
 */
export async function apiFetch(path: string, options: RequestInit = {}): Promise<Response> {
  const headers = new Headers(options.headers)
  headers.set('X-Time-Zone', TimeZone)
  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  // Copy the caller's options, then replace headers with ours.
  const response = await fetch(`${API_URL}${path}`, { ...options, headers, credentials: 'include' })
  if (response.status === 401) {
    window.dispatchEvent(new Event(UNAUTHORIZED_EVENT))
  }
  return response
}
