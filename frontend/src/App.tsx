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

const DateInputSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/)

function nextDateFor(time: string): string {
  const [hours, minutes] = time.split(':').map(Number)
  const target = new Date()
  target.setHours(hours, minutes, 0, 0)

  if (target < new Date()) {
    target.setDate(target.getDate() + 1)
  }

  const year = target.getFullYear()
  const month = String(target.getMonth() + 1).padStart(2, '0')
  const day = String(target.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function App() {
  const [tasks, setTasks] = useState<Task[]>([])
  const [newTitle, setNewTitle] = useState('')
  const [expandedTaskId, setExpandedTaskId] = useState<string | null>(null)
  const [editingTaskId, setEditingTaskId] = useState<string | null>(null)
  const [editingTitle, setEditingTitle] = useState('')
  const priorityNames = ['low', 'medium', 'high']
  const priorityClasses = ['priority-low', 'priority-medium', 'priority-high']

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

  function saveTitle(task: Task) {
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
          placeholder="Add a task..."
        />
        <button onClick={() => {
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
        }}>
          Add
        </button>
      </div>
    </div>
  )
}

export default App