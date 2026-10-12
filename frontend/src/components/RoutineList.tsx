import { useState } from 'react'
import { TitleInputSchema, type Routine } from '../schemas'
import { dayNames, dayLabel } from '../dates'
import type { RoutineChanges } from '../hooks/useRoutines'
import { DayPicker } from './DayPicker'

type Props = {
  routines: Routine[]
  onToggleToday: (routine: Routine) => void
  onTickMissedDay: (routine: Routine, date: string) => void
  onUpdate: (routine: Routine, changes: RoutineChanges) => Promise<boolean>
  onDelete: (routine: Routine) => void
}

// The Routines section. Holds only screen state: Show all, and the edit form.
export function RoutineList({ routines, onToggleToday, onTickMissedDay, onUpdate, onDelete }: Props) {
  const [showAll, setShowAll] = useState(false)
  const [editingRoutineId, setEditingRoutineId] = useState<string | null>(null)
  const [editTitle, setEditTitle] = useState('')
  const [editDays, setEditDays] = useState<string[]>([])
  const [editTime, setEditTime] = useState('')

  const todayName = dayNames[new Date().getDay()]

  const visibleRoutines = routines.filter(r =>
    showAll || r.isToday || r.missedDays.length > 0
  )

  function startEdit(routine: Routine) {
    setEditingRoutineId(routine.id)
    setEditTitle(routine.title)
    setEditDays(routine.days)
    setEditTime(routine.time ? routine.time.slice(0, 5) : '')
  }

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

  function confirmDelete(routine: Routine) {
    if (window.confirm(`Delete ${routine.title}? This also deletes its history.`)) {
      onDelete(routine)
    }
  }

  return (
    <section className="routines">
      <div className="routines-header">
        <h2>Routines</h2>
        <button className="quiet" onClick={() => setShowAll(!showAll)}>
          {showAll ? 'Show today' : 'Show all'}
        </button>
      </div>

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
                  {routine.time && (
                    <span className="task-due">{routine.time.slice(0, 5)}</span>
                  )}
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
                  <button onClick={() => startEdit(routine)}>Edit</button>
                  <button className="danger" onClick={() => confirmDelete(routine)}>Delete</button>
                </div>
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
              </>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
