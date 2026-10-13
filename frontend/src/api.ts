import { API_URL } from './config'

/** The user's time zone, like "Asia/Riyadh". The browser knows it from the device settings. */
const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone

/**
 * fetch, plus what every request to our server needs.
 *
 * @remarks
 * - Adds the server address, so callers only write the path ("/tasks").
 * - Sends the user's time zone in the X-Time-Zone header, so the server knows
 *   what "today" is for this user, not for the server's own clock.
 * - Marks bodies as JSON, because every body we send is JSON.
 *
 * @param path - The path on the server, like "/tasks" or "/routines/123/title".
 * @param options - The usual fetch options: method, body, and so on.
 */
export function apiFetch(path: string, options: RequestInit = {}): Promise<Response> {
  const headers = new Headers(options.headers)
  headers.set('X-Time-Zone', timeZone)
  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  // Copy the caller's options, then replace headers with ours.
  return fetch(`${API_URL}${path}`, { ...options, headers })
}
