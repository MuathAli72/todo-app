import { useState, useEffect } from 'react'
import { z } from 'zod'
import { API_URL } from '../config'
import { RoutineSchema, type Routine } from '../schemas'
import { formatDate } from '../dates'

/** What the routine edit form sends to updateRoutine. */
export type RoutineChanges = {
  title: string
  /** Day names, like "Monday". */
  days: string[]
  /** "HH:mm", or '' for no time. */
  time: string
}

/**
 * Loads all routines from the server and checks their shape.
 *
 * @remarks
 * It lives outside the hook because it doesn't need any state.
 * updateRoutine calls it again to reload after an edit.
 *
 * @returns The routines. The promise fails if the request fails or the shape is wrong;
 * the caller shows the alert.
 */
function fetchRoutines(): Promise<Routine[]> {
  return fetch(`${API_URL}/routines`)
    .then(response => {
      if (!response.ok) {
        throw new Error()
      }
      return response.json()
    })
    .then(data => {
      const result = z.array(RoutineSchema).safeParse(data)
      if (!result.success) {
        throw new Error()
      }
      return result.data
    })
}

/**
 * Holds the routines, and every server call that changes them.
 *
 * @remarks
 * The server decides isToday, isDoneToday and missedDays.
 * For a simple tick this hook updates the screen itself. After an edit it reloads,
 * because only the server can work those values out again.
 *
 * @returns The routines, plus one function per action.
 */
export function useRoutines() {
  const [routines, setRoutines] = useState<Routine[]>([])

  // Load the routines once, when the page opens.
  useEffect(() => {
    fetchRoutines()
      .then(setRoutines)
      .catch(() => alert('Could not load routines.'))
  }, [])

  /** Ticks today for a routine, or unticks it if today is already ticked. */
  function toggleToday(routine: Routine) {
    // The user's today, in their own time zone (see formatDate).
    const today = formatDate(new Date())

    // Tick = POST a completion. Untick = DELETE the completion for that date.
    const request = routine.isDoneToday
      ? fetch(`${API_URL}/routines/${routine.id}/completions/${today}`, {
          method: 'DELETE'
        })
      : fetch(`${API_URL}/routines/${routine.id}/completions`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ date: today })
        })

    request
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setRoutines(routines.map(r => {
          if (r.id === routine.id) {
            return { ...r, isDoneToday: !r.isDoneToday }
          }
          return r
        }))
      })
      .catch(err => alert(err.message))
  }

  /**
   * Ticks a missed day late. The server only allows the last 6 days.
   *
   * @param date - "yyyy-MM-dd", one of routine.missedDays.
   */
  function tickMissedDay(routine: Routine, date: string) {
    fetch(`${API_URL}/routines/${routine.id}/completions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ date: date })
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        // That day isn't missed anymore, so take it off the list.
        setRoutines(routines.map(r => {
          if (r.id === routine.id) {
            return { ...r, missedDays: r.missedDays.filter(d => d !== date) }
          }
          return r
        }))
      })
      .catch(err => alert(err.message))
  }

  /**
   * Creates a routine.
   *
   * @param days - Day names, like "Monday".
   * @param time - "HH:mm", or '' for no time.
   * @returns The routine as the server saved it, or null if it failed (the error was already shown).
   * AddBox uses the result to show the "It'll show on..." notice.
   */
  function addRoutine(title: string, days: string[], time: string): Promise<Routine | null> {
    return fetch(`${API_URL}/routines`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      // The server wants null for "no time", not an empty string.
      body: JSON.stringify({ title, days, time: time || null })
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        return response.json()
      })
      .then(data => {
        const result = RoutineSchema.safeParse(data)
        if (!result.success) {
          throw new Error('The server sent the routine in an unexpected shape.')
        }
        setRoutines([...routines, result.data])
        return result.data
      })
      .catch(err => {
        alert(err.message)
        return null
      })
  }

  /**
   * Saves an edit from the routine form.
   *
   * @remarks
   * Only the parts that changed are sent, each to its own endpoint, all at the same time.
   * Afterwards it always reloads (even after an error), because:
   * - the server works out isToday and missedDays, and changing days restarts tracking;
   * - if one request failed, the others may still have saved, so reloading shows what really saved.
   *
   * @returns true if every change saved. false if not (the error was already shown).
   */
  function updateRoutine(routine: Routine, changes: RoutineChanges): Promise<boolean> {
    const url = `${API_URL}/routines/${routine.id}`
    const headers = { 'Content-Type': 'application/json' }
    const requests: Promise<Response>[] = []

    if (changes.title !== routine.title) {
      requests.push(fetch(`${url}/title`, {
        method: 'PATCH',
        headers,
        body: JSON.stringify({ title: changes.title })
      }))
    }

    // The days changed if the count is different, or any picked day is new.
    const daysChanged =
      changes.days.length !== routine.days.length ||
      changes.days.some(d => !routine.days.includes(d))
    if (daysChanged) {
      requests.push(fetch(`${url}/days`, {
        method: 'PATCH',
        headers,
        body: JSON.stringify({ days: changes.days })
      }))
    }

    // The server sends "HH:mm:ss"; the form uses "HH:mm". Compare them the same way.
    const currentTime = routine.time ? routine.time.slice(0, 5) : ''
    if (changes.time !== currentTime) {
      requests.push(fetch(`${url}/time`, {
        method: 'PATCH',
        headers,
        body: JSON.stringify({ time: changes.time || null })
      }))
    }

    // Nothing changed, so there's nothing to send.
    if (requests.length === 0) {
      return Promise.resolve(true)
    }

    // Promise.all waits for every request. If any failed, show its message.
    return Promise.all(requests)
      .then(responses => {
        const failed = responses.find(r => !r.ok)
        if (failed) {
          return failed.json().then(err => { throw new Error(err.error) })
        }
        return true
      })
      .catch(err => {
        alert(err.message)
        return false
      })
      .finally(() => {
        fetchRoutines()
          .then(setRoutines)
          .catch(() => alert('Could not load routines.'))
      })
  }

  /** Deletes a routine and all its ticks. RoutineList asks the user to confirm first. */
  function deleteRoutine(routine: Routine) {
    fetch(`${API_URL}/routines/${routine.id}`, {
      method: 'DELETE'
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setRoutines(routines.filter(r => r.id !== routine.id))
      })
      .catch(err => alert(err.message))
  }

  return { routines, toggleToday, tickMissedDay, addRoutine, updateRoutine, deleteRoutine }
}
