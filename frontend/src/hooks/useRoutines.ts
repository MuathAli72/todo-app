import { useState, useEffect } from 'react'
import { z } from 'zod'
import { API_URL } from '../config'
import { RoutineSchema, type Routine } from '../schemas'
import { formatDate } from '../dates'

export type RoutineChanges = {
  title: string
  days: string[]
  time: string
}

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

// The routines, and every server call that changes them.
export function useRoutines() {
  const [routines, setRoutines] = useState<Routine[]>([])

  useEffect(() => {
    fetchRoutines()
      .then(setRoutines)
      .catch(() => alert('Could not load routines.'))
  }, [])

  function toggleToday(routine: Routine) {
    const today = formatDate(new Date())

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
        setRoutines(routines.map(r => {
          if (r.id === routine.id) {
            return { ...r, missedDays: r.missedDays.filter(d => d !== date) }
          }
          return r
        }))
      })
      .catch(err => alert(err.message))
  }

  function addRoutine(title: string, days: string[], time: string): Promise<Routine | null> {
    return fetch(`${API_URL}/routines`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
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

  // Sends only what changed, then reloads, because the server works out
  // "is it today" and "missed days" after a change.
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

    const currentTime = routine.time ? routine.time.slice(0, 5) : ''
    if (changes.time !== currentTime) {
      requests.push(fetch(`${url}/time`, {
        method: 'PATCH',
        headers,
        body: JSON.stringify({ time: changes.time || null })
      }))
    }

    if (requests.length === 0) {
      return Promise.resolve(true)
    }

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
