import { useState, useEffect } from 'react'
import { TitleInputSchema, type Routine } from '../schemas'
import { dayNames } from '../dates'
import { DayPicker } from './DayPicker'

type Props = {
  onAddTask: (title: string) => Promise<boolean>
  onAddRoutine: (title: string, days: string[], time: string) => Promise<Routine | null>
}

// The add box at the bottom: a task, or a routine when Repeat is on.
export function AddBox({ onAddTask, onAddRoutine }: Props) {
  const [newTitle, setNewTitle] = useState('')
  const [repeat, setRepeat] = useState(false)
  const [newDays, setNewDays] = useState<string[]>([])
  const [newTime, setNewTime] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    if (notice === '') {
      return
    }
    const timer = setTimeout(() => setNotice(''), 4000)
    return () => clearTimeout(timer)
  }, [notice])

  function add() {
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

      {notice && <p className="notice" role="status">{notice}</p>}
    </>
  )
}
