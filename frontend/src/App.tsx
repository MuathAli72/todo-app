import { useState, useEffect } from 'react'
import './App.css'
import { z } from 'zod'

const TaskSchema = z.object({
  id: z.string(),
  title: z.string(),
  isDone: z.boolean(),
  priority: z.number().int().min(0).max(2),
  dueDate: z.string().nullable(),
  dueTime: z.string().nullable(),
})

type Task = z.infer<typeof TaskSchema>

const RoutineSchema = z.object({
  id: z.string(),
  title: z.string(),
  days: z.array(z.string()),
  time: z.string().nullable(),
  isToday: z.boolean(),
  isDoneToday: z.boolean(),
  missedDays: z.array(z.string()),
})

type Routine = z.infer<typeof RoutineSchema>

const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

const DateInputSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/)
const TitleInputSchema = z.string().trim().min(1, 'Please enter a title.')

function formatDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function dayLabel(date: string): string {
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString('en-GB', {
    weekday: 'short', day: 'numeric', month: 'short'
  })
}

function nextDateFor(time: string): string {
  const [hours, minutes] = time.split(':').map(Number)
  const target = new Date()
  target.setHours(hours, minutes, 0, 0)

  if (target < new Date()) {
    target.setDate(target.getDate() + 1)
  }

  return formatDate(target)
}

function App() {
  const [tasks, setTasks] = useState<Task[]>([])
  const [newTitle, setNewTitle] = useState('')
  const [expandedTaskId, setExpandedTaskId] = useState<string | null>(null)
  const [editingTaskId, setEditingTaskId] = useState<string | null>(null)
  const [editingTitle, setEditingTitle] = useState('')
  const priorityNames = ['low', 'medium', 'high']
  const priorityClasses = ['priority-low', 'priority-medium', 'priority-high']

  const [routines, setRoutines] = useState<Routine[]>([])
  const [showAll, setShowAll] = useState(false)
  const [repeat, setRepeat] = useState(false)
  const [newDays, setNewDays] = useState<string[]>([])
  const [newTime, setNewTime] = useState('')
  const [notice, setNotice] = useState('')

  const visibleRoutines = routines.filter(r =>
    showAll || r.isToday || r.missedDays.length > 0
  )

  function toggleDay(day: string) {
    if (newDays.includes(day)) {
      setNewDays(newDays.filter(d => d !== day))
    } else {
      setNewDays([...newDays, day])
    }
  }

  useEffect(() => {
    fetch('http://localhost:5149/routines')
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
        setRoutines(result.data)
      })
      .catch(() => alert('Could not load routines.'))
  }, [])

  useEffect(() => {
    fetch('http://localhost:5149/tasks')
      .then(response => response.json())
      .then(data => {
        const result = z.array(TaskSchema).safeParse(data)
        if (!result.success) {
          console.error(result.error)
          alert('The server sent tasks in an unexpected shape.')
          return
        }
        setTasks(result.data)
      })
  }, [])

  useEffect(() => {
    function handleClickOutside() {
      setExpandedTaskId(null)
    }

    document.addEventListener('click', handleClickOutside)

    return () => {
      document.removeEventListener('click', handleClickOutside)
    }
  }, [])

  useEffect(() => {
    if (notice === '') {
      return
    }
    const timer = setTimeout(() => setNotice(''), 4000)
    return () => clearTimeout(timer)
  }, [notice])

  function saveTitle(task: Task) {
    const check = TitleInputSchema.safeParse(editingTitle)
    if (!check.success) {
      setEditingTaskId(null)
      alert(check.error.issues[0].message)
      return
    }
    fetch(`http://localhost:5149/tasks/${task.id}/title`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title: editingTitle })
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, title: editingTitle }
          }
          return t
        }))
        setEditingTaskId(null)
      })
      .catch(err => {
        alert(err.message)
      })
  }

  function changePriority(task: Task, newPriority: number) {
    fetch(`http://localhost:5149/tasks/${task.id}/priority`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ priority: priorityNames[newPriority] })
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, priority: newPriority }
          }
          return t
        }))
      })
      .catch(err => {
        alert(err.message)
      })
  }

  function toggleTask(task: Task) {
    const action = task.isDone ? 'reopen' : 'complete'

    fetch(`http://localhost:5149/tasks/${task.id}/${action}`, {
      method: 'PATCH'
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, isDone: !t.isDone }
          }
          return t
        }))
      })
      .catch(err => {
        alert(err.message)
      })
  }

  function toggleRoutineToday(routine: Routine) {
    const today = formatDate(new Date())

    const request = routine.isDoneToday
      ? fetch(`http://localhost:5149/routines/${routine.id}/completions/${today}`, {
          method: 'DELETE'
        })
      : fetch(`http://localhost:5149/routines/${routine.id}/completions`, {
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
    fetch(`http://localhost:5149/routines/${routine.id}/completions`, {
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

  function addTask() {
    const check = TitleInputSchema.safeParse(newTitle)
    if (!check.success) {
      alert(check.error.issues[0].message)
      return
    }
    fetch('http://localhost:5149/tasks', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title: newTitle })
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        return response.json()
      })
      .then(newTask => {
        setTasks([...tasks, newTask])
        setNewTitle('')
      })
      .catch(err => {
        alert(err.message)
      })
  }

  function addRoutine() {
    const check = TitleInputSchema.safeParse(newTitle)
    if (!check.success) {
      alert(check.error.issues[0].message)
      return
    }
    if (newDays.length === 0) {
      alert('Pick at least one day.')
      return
    }
    fetch('http://localhost:5149/routines', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title: newTitle, days: newDays, time: newTime || null })
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
        if (!result.data.isToday) {
          const days = dayNames
            .filter(d => result.data.days.includes(d))
            .map(d => d.slice(0, 3))
            .join(', ')
          setNotice(`${result.data.title} saved. It'll show on ${days}.`)
        }
        setNewTitle('')
        setNewDays([])
        setNewTime('')
        setRepeat(false)
      })
      .catch(err => alert(err.message))
  }

  function deleteRoutine(routine: Routine) {
    if (!window.confirm(`Delete ${routine.title}? This also deletes its history.`)) {
      return
    }
    fetch(`http://localhost:5149/routines/${routine.id}`, {
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

  function deleteTask(task: Task) {
    fetch(`http://localhost:5149/tasks/${task.id}`, {
      method: 'DELETE'
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setTasks(tasks.filter(t => t.id !== task.id))
      })
      .catch(err => {
        alert(err.message)
      })
  }

  function changeDueDate(task: Task, date: string | null, time: string | null) {
    fetch(`http://localhost:5149/tasks/${task.id}/due-date`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dueDate: date ?? '', dueTime: time ?? '' })
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, dueDate: date, dueTime: time }
          }
          return t
        }))
      })
      .catch(err => {
        alert(err.message)
      })
  }

  return (
    <div className="app">
      <h1>My Todo App</h1>

      <section className="routines">
        <div className="routines-header">
          <h2>Routines</h2>
          <button onClick={() => setShowAll(!showAll)}>
            {showAll ? 'Show today' : 'Show all'}
          </button>
        </div>

        {visibleRoutines.length === 0 && (
          <p className="routines-empty">No routines today.</p>
        )}

        <ul className="task-list">
          {visibleRoutines.map(routine => (
            <li key={routine.id} className="task">
              <div className="task-row">
                {routine.isToday && (
                  <input
                    type="checkbox"
                    checked={routine.isDoneToday}
                    onChange={() => toggleRoutineToday(routine)}
                  />
                )}
                <span className="task-title">{routine.title}</span>
                {routine.time && (
                  <span className="task-due">{routine.time.slice(0, 5)}</span>
                )}
                <button onClick={() => deleteRoutine(routine)}>Delete</button>
              </div>
              {routine.missedDays.length > 0 && (
                <div className="missed-days">
                  Missed:
                  {routine.missedDays.map(date => (
                    <button key={date} onClick={() => tickMissedDay(routine, date)}>
                      {dayLabel(date)}
                    </button>
                  ))}
                </div>
              )}
            </li>
          ))}
        </ul>
      </section>

      <h2>Tasks</h2>
      <ul className="task-list">
        {tasks.map(task => (
          <li key={task.id} className={`task ${priorityClasses[task.priority]}`}>
            <div className="task-row">
              <input
                type="checkbox"
                checked={task.isDone}
                onChange={() => toggleTask(task)}
              />
              {editingTaskId === task.id ? (
                <input
                  value={editingTitle}
                  onChange={e => setEditingTitle(e.target.value)}
                  onBlur={() => saveTitle(task)}
                  autoFocus
                />
              ) : (
                <span
                  className={`task-title ${task.isDone ? 'done' : ''}`}
                  onClick={() => {
                    setEditingTaskId(task.id)
                    setEditingTitle(task.title)
                  }}
                >
                  {task.title}
                </span>
              )}
              {task.dueDate && (
                <span className="task-due">
                  due {task.dueDate}{task.dueTime && ` ${task.dueTime.slice(0, 5)}`}
                </span>
              )}
              <button onClick={() => deleteTask(task)}>Delete</button>
              <button onClick={e => {
                e.stopPropagation()
                setExpandedTaskId(expandedTaskId === task.id ? null : task.id)
              }}>
                More options
              </button>
            </div>

            {expandedTaskId === task.id && (
              <div className="task-options" onClick={e => e.stopPropagation()}>
                <select
                  value={task.priority}
                  onChange={e => changePriority(task, Number(e.target.value))}
                >
                  <option value={0}>Low</option>
                  <option value={1}>Medium</option>
                  <option value={2}>High</option>
                </select>
                <input
                  key={`date-${task.dueDate ?? 'none'}`}
                  type="date"
                  max="9999-12-31"
                  defaultValue={task.dueDate ?? ''}
                  onBlur={e => {
                    const date = e.target.value
                    if (date === '' || date === task.dueDate) return
                    if (!DateInputSchema.safeParse(date).success) {
                      e.target.value = task.dueDate ?? ''
                      alert('Please enter a real date.')
                      return
                    }
                    changeDueDate(task, date, task.dueTime)
                  }}
                />
                <input
                  key={`time-${task.dueTime ?? 'none'}`}
                  type="time"
                  defaultValue={task.dueTime ? task.dueTime.slice(0, 5) : ''}
                  onBlur={e => {
                    const time = e.target.value
                    const current = task.dueTime ? task.dueTime.slice(0, 5) : ''
                    if (time === current) return
                    if (time === '') {
                      if (task.dueDate) changeDueDate(task, task.dueDate, null)
                      return
                    }
                    changeDueDate(task, task.dueDate ?? nextDateFor(time), time)
                  }}
                />
                {task.dueDate && (
                  <button onClick={() => changeDueDate(task, null, null)}>Remove date</button>
                )}
              </div>
            )}
          </li>
        ))}
      </ul>

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
        <button onClick={() => (repeat ? addRoutine() : addTask())}>
          Add
        </button>
      </div>

      {repeat && (
        <div className="repeat-options">
          {dayNames.map(day => (
            <button
              key={day}
              className={newDays.includes(day) ? 'day-button selected' : 'day-button'}
              onClick={() => toggleDay(day)}
            >
              {day.slice(0, 3)}
            </button>
          ))}
          <input
            type="time"
            value={newTime}
            onChange={e => setNewTime(e.target.value)}
          />
        </div>
      )}

      {notice && <p className="notice">{notice}</p>}
    </div>
  )
}

export default App