import { useState, useEffect } from 'react'
import './App.css'

function App() {

  type Task = {
    id: string
    title: string
    isDone: boolean
    priority: number
    dueDate: string | null
  }

  const [tasks, setTasks] = useState<Task[]>([])
  const [newTitle, setNewTitle] = useState('')
  const [expandedTaskId, setExpandedTaskId] = useState<string | null>(null)
  const priorityNames = ['low', 'medium', 'high']
  const priorityClasses = ['priority-low', 'priority-medium', 'priority-high']
  const [editingTaskId, setEditingTaskId] = useState<string | null>(null)
  const [editingTitle, setEditingTitle] = useState('')


  function saveTitle(task: Task) {
    fetch(`http://localhost:5149/tasks/${task.id}/title`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title: editingTitle })
    })
      .then(() => {
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, title: editingTitle }
          }
          return t
        }))
        setEditingTaskId(null)
      })
  }
  function changePriority(task: Task, newPriority: number) {
    fetch(`http://localhost:5149/tasks/${task.id}/priority`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ priority: priorityNames[newPriority] })
    })
      .then(() => {
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, priority: newPriority }
          }
          return t
        }))
      })
  }

  useEffect(() => {
    fetch('http://localhost:5149/tasks')
      .then(response => response.json())
      .then(data => setTasks(data))
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

  function toggleTask(task: Task) {
    const action = task.isDone ? 'reopen' : 'complete'

    fetch(`http://localhost:5149/tasks/${task.id}/${action}`, {
      method: 'PATCH'
    })
      .then(() => {
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, isDone: !t.isDone }
          }
          return t
        }))
      })
  }

  function deleteTask(task: Task) {
    fetch(`http://localhost:5149/tasks/${task.id}`, {
      method: 'DELETE'
    })
      .then(() => {
        setTasks(tasks.filter(t => t.id !== task.id))
      })
  }

  function changeDueDate(task: Task, newDate: string) {
    fetch(`http://localhost:5149/tasks/${task.id}/due-date`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dueDate: newDate })
    })
      .then(() => {
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, dueDate: newDate }
          }
          return t
        }))
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
                  due {task.dueDate.slice(0, 16).replace('T', ' ')}
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
                  type="datetime-local"
                  value={task.dueDate ? task.dueDate.slice(0, 16) : ''}
                  onChange={e => changeDueDate(task, e.target.value)}
                />
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
            .then(response => response.json())
            .then(newTask => {
              setTasks([...tasks, newTask])
              setNewTitle('')
            })
        }}>
          Add
        </button>
      </div>
    </div>
  )
}

export default App