import { useEffect, useState } from 'react'
import type { HistoryDay, Routine } from '../schemas'
import { fetchHistory } from '../hooks/useRoutines'
import './RoutineHistory.css'

type Props = {
  routine: Routine
  /** Ticks (done = true) or unticks a day. Resolves to true when it worked. */
  onSetDayDone: (routine: Routine, date: string, done: boolean) => Promise<boolean>
}

/** What each status means, for the hover text and the legend. */
const StatusLabels: Record<HistoryDay['status'], string> = {
  notstarted: 'Before the routine existed',
  off: 'Not one of its days',
  done: 'Done',
  missed: 'Missed',
  pending: 'Today, not done yet',
}

/** "yyyy-MM-dd" to something readable, like "Mon 12 Oct". */
function readableDate(date: string): string {
  return new Date(`${date}T00:00`).toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
}

/**
 * A routine's last 30 days as a row of squares, with its streaks.
 *
 * @remarks
 * - Click a done day to untick it (undo a wrong tick, including a late one).
 * - Click a missed day inside the 6-day window to tick it late.
 * Loads again whenever the routine changes, which happens after every tick (the list reloads).
 */
export function RoutineHistory({ routine, onSetDayDone }: Props) {
  const [days, setDays] = useState<HistoryDay[] | null>(null)

  useEffect(() => {
    // Ignore an answer that arrives after this view closed or the routine changed again.
    let current = true
    fetchHistory(routine.id)
      .then(result => {
        if (current) {
          setDays(result)
        }
      })
      .catch(() => alert('Could not load the history.'))
    return () => {
      current = false
    }
  }, [routine])

  if (days === null) {
    return <div className="routine-history">Loading...</div>
  }

  const doneCount = days.filter(d => d.status === 'done').length

  function click(day: HistoryDay) {
    if (day.status === 'done') {
      void onSetDayDone(routine, day.date, false)
    } else if (day.canTick) {
      void onSetDayDone(routine, day.date, true)
    }
  }

  return (
    <div className="routine-history">
      <p className="history-summary">
        Streak: {routine.currentStreak} · Best: {routine.bestStreak} · Done {doneCount} of the last {days.length} days
      </p>
      <div className="history-days">
        {days.map(day => {
          const clickable = day.status === 'done' || day.canTick
          const hint = day.status === 'done' ? ' (click to undo)' : day.canTick ? ' (click to tick late)' : ''
          return (
            <button
              key={day.date}
              type="button"
              className={`history-day ${day.status}`}
              title={`${readableDate(day.date)}: ${StatusLabels[day.status]}${hint}`}
              aria-label={`${readableDate(day.date)}: ${StatusLabels[day.status]}`}
              disabled={!clickable}
              onClick={() => click(day)}
            />
          )
        })}
      </div>
      <p className="history-legend">
        <span className="history-day done" /> done <span className="history-day missed" /> missed{' '}
        <span className="history-day pending" /> today <span className="history-day off" /> day off
      </p>
    </div>
  )
}
