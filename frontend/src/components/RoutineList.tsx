import { useState } from 'react'
import { TitleInputSchema, type Routine } from '../schemas'
import { dayNames, dayLabel } from '../dates'
import type { RoutineChanges } from '../hooks/useRoutines'
import { DayPicker } from './DayPicker'
import { RoutineHistory } from './RoutineHistory'
import { CalendarLink } from './CalendarLink'

type Props = {
  routines: Routine[]
  /** Ticks or unticks today. */
  onToggleToday: (routine: Routine) => void
  /** Ticks a missed day late. date is "yyyy-MM-dd". */
  onTickMissedDay: (routine: Routine, date: string) => void
  /** Ticks (done = true) or unticks any day, from the history view. Resolves to true when it worked. */
  onSetDayDone: (routine: Routine, date: string, done: boolean) => Promise<boolean>
  /** Saves an edit. Resolves to true when everything saved. */
  onUpdate: (routine: Routine, changes: RoutineChanges) => Promise<boolean>
  /** Deletes a routine and its history. */
  onDelete: (routine: Routine) => void
}

/**
 * The Routines section.
 *
 * @remarks
 * By default it only shows routines that matter today: on today, or with missed days.
 * "Show all" lists every routine. "History" opens a routine's last 30 days,
 * and "Calendar" shows the link for adding routines to a phone calendar.
 * It only holds screen state (Show all, and the edit form). The routines come from useRoutines.
 */
export function RoutineList({ routines, onToggleToday, onTickMissedDay, onSetDayDone, onUpdate, onDelete }: Props) {
  const [showAll, setShowAll] = useState(false)
  const [showCalendar, setShowCalendar] = useState(false)
  const [historyRoutineId, setHistoryRoutineId] = useState<string | null>(null)
  const [editingRoutineId, setEditingRoutineId] = useState<string | null>(null)
  const [editTitle, setEditTitle] = useState('')
  const [editDays, setEditDays] = useState<string[]>([])
  const [editTime, setEditTime] = useState('')

  const todayName = dayNames[new Date().getDay()]

  // Worked out from routines on every render, not stored, so it can't get out of sync.
  const visibleRoutines = routines.filter(r =>
    showAll || r.isToday || r.missedDays.length > 0
  )

  /** Opens the edit form, filled with the routine's current values. */
  function startEdit(routine: Routine) {
    setEditingRoutineId(routine.id)
    setEditTitle(routine.title)
    setEditDays(routine.days)
    // The time input wants "HH:mm"; the server sends "HH:mm:ss".
    setEditTime(routine.time ? routine.time.slice(0, 5) : '')
  }

  /** Checks the form, then saves. The form only closes if the save worked. */
  function saveEdit(routine: Routine) {
    const check = TitleInputSchema.safeParse(editTitle)
    if (!check.success) {
      alert(check.error.issues[0].message)
      return
    }
    if (editDays.length === 0) {
      alert('Pick at least one day.')
      return
    }
    onUpdate(routine, { title: editTitle, days: editDays, time: editTime }).then(ok => {
      if (ok) {
        setEditingRoutineId(null)
      }
    })
  }

  /**
   * Asks before deleting.
   *
   * @remarks
   * Routines ask first because deleting one also deletes its tick history.
   * Tasks don't ask: there's nothing extra to lose.
   */
  function confirmDelete(routine: Routine) {
    if (window.confirm(`Delete ${routine.title}? This also deletes its history.`)) {
      onDelete(routine)
    }
  }

  return (
    <section className="routines">
      <div className="routines-header">
        <h2>Routines</h2>
        <div className="routines-header-actions">
          <button className="quiet" onClick={() => setShowCalendar(!showCalendar)}>
            {showCalendar ? 'Hide calendar' : 'Calendar'}
          </button>
          <button className="quiet" onClick={() => setShowAll(!showAll)}>
            {showAll ? 'Show today' : 'Show all'}
          </button>
        </div>
      </div>

      {showCalendar && <CalendarLink />}

      {visibleRoutines.length === 0 && (
        <p className="routines-empty">No routines today.</p>
      )}

      <ul className="task-list">
        {visibleRoutines.map(routine => (
          <li key={routine.id} className="task">
            {editingRoutineId === routine.id ? (
              <div className="routine-edit">
                <input
                  value={editTitle}
                  onChange={e => setEditTitle(e.target.value)}
                  autoFocus
                />
                <div className="repeat-options">
                  <DayPicker selected={editDays} onChange={setEditDays} />
                  <input
                    type="time"
                    value={editTime}
                    onChange={e => setEditTime(e.target.value)}
                  />
                </div>
                <div className="routine-edit-actions">
                  <button className="primary" onClick={() => saveEdit(routine)}>Save</button>
                  <button onClick={() => setEditingRoutineId(null)}>Cancel</button>
                </div>
              </div>
            ) : (
              <>
                <div className="task-row">
                  {/* Only today can be ticked here. Other days get an empty space so the titles line up. */}
                  {routine.isToday ? (
                    <input
                      type="checkbox"
                      checked={routine.isDoneToday}
                      onChange={() => onToggleToday(routine)}
                    />
                  ) : (
                    <span className="checkbox-spacer" aria-hidden="true" />
                  )}
                  <span className="task-title">{routine.title}</span>
                  {/* The streak: scheduled days done in a row. Hover shows the best ever. */}
                  {routine.currentStreak > 0 && (
                    <span className="streak" title={`Best streak: ${routine.bestStreak}`}>
                      🔥 {routine.currentStreak}
                    </span>
                  )}
                  {routine.time && (
                    <span className="task-due">{routine.time.slice(0, 5)}</span>
                  )}
                  {/* The week strip: one letter per day. Its days are lit, today is marked. */}
                  <span className="week-strip" aria-label={`Repeats on ${routine.days.join(', ')}`}>
                    {dayNames.map(day => (
                      <span
                        key={day}
                        className={[
                          'week-day',
                          routine.days.includes(day) ? 'on' : '',
                          day === todayName ? 'today' : '',
                          day === todayName && routine.isDoneToday ? 'done' : ''
                        ].join(' ')}
                      >
                        {day[0]}
                      </span>
                    ))}
                  </span>
                  <button onClick={() => setHistoryRoutineId(historyRoutineId === routine.id ? null : routine.id)}>
                    History
                  </button>
                  <button onClick={() => startEdit(routine)}>Edit</button>
                  <button className="danger" onClick={() => confirmDelete(routine)}>Delete</button>
                </div>
                {/* Missed days from the last 6 days. Clicking one ticks it late. */}
                {routine.missedDays.length > 0 && (
                  <div className="missed-days">
                    Missed:
                    {routine.missedDays.map(date => (
                      <button key={date} onClick={() => onTickMissedDay(routine, date)}>
                        {dayLabel(date)}
                      </button>
                    ))}
                  </div>
                )}
                {historyRoutineId === routine.id && (
                  <RoutineHistory routine={routine} onSetDayDone={onSetDayDone} />
                )}
              </>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
