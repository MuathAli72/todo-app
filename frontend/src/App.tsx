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

  useEffect(() => {
    fetch('http://localhost:5149/tasks')
      .then(response => response.json())
      .then(data => setTasks(data))
  }, [])

  return (
    <div>
      <h1>My Todo App</h1>
      <ul>
        {tasks.map(task => (
          <li key={task.id}>{task.title}</li>
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