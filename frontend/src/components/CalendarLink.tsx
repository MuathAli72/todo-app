import { useEffect, useState } from 'react'
import { apiFetch } from '../api'
import { API_URL } from '../config'
import { CalendarLinkSchema } from '../schemas'

/** The full feed address for a token. Works when API_URL is a full address (laptop) or "/api" (live). */
function feedUrl(token: string): string {
  return new URL(`${API_URL}/calendar/${token}.ics`, window.location.origin).href
}

/** Asks the server for the link (GET), or for a brand-new one (POST reset). */
function requestLink(reset: boolean): Promise<string> {
  const request = reset
    ? apiFetch('/account/calendar-link/reset', { method: 'POST' })
    : apiFetch('/account/calendar-link')
  return request
    .then(response => {
      if (!response.ok) {
        throw new Error()
      }
      return response.json()
    })
    .then(data => feedUrl(CalendarLinkSchema.parse(data).token))
}

/**
 * Reminders: the user's secret link for their phone or computer calendar.
 *
 * @remarks
 * The calendar app checks the link every few hours, so new routines show up by themselves.
 * Routines with a time get a reminder at that time. The link is the only key, so it can be reset.
 */
export function CalendarLink() {
  const [url, setUrl] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    requestLink(false)
      .then(setUrl)
      .catch(() => alert('Could not get your calendar link.'))
  }, [])

  function copy() {
    if (url) {
      void navigator.clipboard.writeText(url).then(() => setCopied(true))
    }
  }

  function reset() {
    if (window.confirm('Make a new link? The old one stops working, so calendars using it stop updating.')) {
      requestLink(true)
        .then(newUrl => {
          setUrl(newUrl)
          setCopied(false)
        })
        .catch(() => alert('Could not make a new link.'))
    }
  }

  if (url === null) {
    return <div className="calendar-link">Loading...</div>
  }

  return (
    <div className="calendar-link">
      <p>
        Add your routines to your phone or computer calendar, with a reminder at each routine's time.
        Subscribe to this link (Google Calendar: Other calendars → From URL. iPhone: Settings → Calendar →
        Accounts → Add Subscribed Calendar).
      </p>
      <input readOnly value={url} onFocus={e => e.target.select()} aria-label="Calendar link" />
      <div className="calendar-link-actions">
        <button onClick={copy}>{copied ? 'Copied' : 'Copy link'}</button>
        <a href={url.replace(/^https?:/, 'webcal:')}>Open in calendar app</a>
        <button className="danger" onClick={reset}>New link</button>
      </div>
      <p className="calendar-link-note">Keep it private: anyone with the link can see your routine names and times.</p>
    </div>
  )
}
