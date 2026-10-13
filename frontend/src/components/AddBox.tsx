import { useState, useEffect } from 'react'
import { TitleInputSchema, type Routine } from '../schemas'
import { dayNames } from '../dates'
import { DayPicker } from './DayPicker'

type Props = {
  /** Adds a task. Resolves to true when it saved. */
  onAddTask: (title: string) => Promise<boolean>
  /** Adds a routine. Resolves to the saved routine, or null when it failed. */
  onAddRoutine: (title: string, days: string[], time: string) => Promise<Routine | null>
}

/**
 * The add box at the bottom. It adds a task, or a routine when Repeat is on.
 *
 * @remarks
 * It only holds what's being typed. The input is cleared only after the save worked,
 * so a failed save doesn't lose the user's text.
 */
export function AddBox({ onAddTask, onAddRoutine }: Props) {
  const [newTitle, setNewTitle] = useState('')
  const [repeat, setRepeat] = useState(false)
  const [newDays, setNewDays] = useState<string[]>([])
  const [newTime, setNewTime] = useState('')
  const [notice, setNotice] = useState('')

  // Hide the notice after 4 seconds.
  // The cleanup cancels the old timer if a new notice comes first.
  useEffect(() => {
    if (notice === '') {
      return
    }
    const timer = setTimeout(() => setNotice(''), 4000)
    return () => clearTimeout(timer)
  }, [notice])

  /** Checks the title, then adds a task or a routine. */
  function add() {
    // Same title rule as the server, checked here so the user gets the message right away.
    const check = TitleInputSchema.safeParse(newTitle)
    if (!check.success) {
      alert(check.error.issues[0].message)
      return
    }
    if (repeat) {
      addRoutine()
    } else {
      onAddTask(newTitle).then(ok => {
        if (ok) {
          setNewTitle('')
        }
      })
    }
  }

  /**
   * Adds a routine.
   *
   * @remarks
   * A routine that isn't on today won't appear in the list,
   * so we show a notice saying which days it will show on.
   */
  function addRoutine() {
    if (newDays.length === 0) {
      alert('Pick at least one day.')
      return
    }
    onAddRoutine(newTitle, newDays, newTime).then(created => {
      if (!created) {
        return
      }
      if (!created.isToday) {
        // Put the days in week order and shorten them: "Mon, Wed".
        const days = dayNames
          .filter(d => created.days.includes(d))
          .map(d => d.slice(0, 3))
          .join(', ')
        setNotice(`${created.title} saved. It'll show on ${days}.`)
      }
      setNewTitle('')
      setNewDays([])
      setNewTime('')
      setRepeat(false)
    })
  }

  return (
    <>
      <div className="add-row">
        <input
          value={newTitle}
          onChange={e => setNewTitle(e.target.value)}
          placeholder={repeat ? 'Add a routine...' : 'Add a task...'}
        />
        <button
          className={repeat ? 'repeat-button selected' : 'repeat-button'}
          onClick={() => setRepeat(!repeat)}
        >
          Repeat
        </button>
        <button className="primary" onClick={add}>
          Add
        </button>
      </div>

      {repeat && (
        <div className="repeat-options">
          <DayPicker selected={newDays} onChange={setNewDays} />
          <input
            type="time"
            value={newTime}
            onChange={e => setNewTime(e.target.value)}
          />
        </div>
      )}

      {/* role="status" makes screen readers read the notice out. */}
      {notice && <p className="notice" role="status">{notice}</p>}
    </>
  )
}
