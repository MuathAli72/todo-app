import { useState, useEffect } from 'react'

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
    <div>
      <h1>My Todo App</h1>
      <ul>
        {tasks.map(task => (
          <li key={task.id}>
            <input
              type="checkbox"
              checked={task.isDone}
              onChange={() => toggleTask(task)}
            />
            {task.dueDate && <span> — due {task.dueDate.slice(0, 16).replace('T', ' ')}</span>}
            {task.title}
            <button onClick={() => deleteTask(task)}>Delete</button>
            <button onClick={() => { setExpandedTaskId(expandedTaskId === task.id ? null : task.id) }}>
              More options
            </button>


            {expandedTaskId === task.id && (
              <div>
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
      <input
        value={newTitle}
        onChange={e => setNewTitle(e.target.value)}
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
  )
}

export default App