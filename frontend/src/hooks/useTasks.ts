import { useState, useEffect } from 'react'
import { z } from 'zod'
import { API_URL } from '../config'
import { TaskSchema, type Task } from '../schemas'

const priorityNames = ['low', 'medium', 'high']

// The tasks, and every server call that changes them.
export function useTasks() {
  const [tasks, setTasks] = useState<Task[]>([])

  useEffect(() => {
    fetch(`${API_URL}/tasks`)
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

  function addTask(title: string): Promise<boolean> {
    return fetch(`${API_URL}/tasks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title })
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        return response.json()
      })
      .then(data => {
        const result = TaskSchema.safeParse(data)
        if (!result.success) {
          throw new Error('The server sent the task in an unexpected shape.')
        }
        setTasks([...tasks, result.data])
        return true
      })
      .catch(err => {
        alert(err.message)
        return false
      })
  }

  function renameTask(task: Task, title: string): Promise<boolean> {
    return fetch(`${API_URL}/tasks/${task.id}/title`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title })
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setTasks(tasks.map(t => {
          if (t.id === task.id) {
            return { ...t, title }
          }
          return t
        }))
        return true
      })
      .catch(err => {
        alert(err.message)
        return false
      })
  }

  function changePriority(task: Task, newPriority: number) {
    fetch(`${API_URL}/tasks/${task.id}/priority`, {
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
      .catch(err => alert(err.message))
  }

  function toggleTask(task: Task) {
    const action = task.isDone ? 'reopen' : 'complete'

    fetch(`${API_URL}/tasks/${task.id}/${action}`, {
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
      .catch(err => alert(err.message))
  }

  function changeDueDate(task: Task, date: string | null, time: string | null) {
    fetch(`${API_URL}/tasks/${task.id}/due-date`, {
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
      .catch(err => alert(err.message))
  }

  function deleteTask(task: Task) {
    fetch(`${API_URL}/tasks/${task.id}`, {
      method: 'DELETE'
    })
      .then(response => {
        if (!response.ok) {
          return response.json().then(err => { throw new Error(err.error) })
        }
        setTasks(tasks.filter(t => t.id !== task.id))
      })
      .catch(err => alert(err.message))
  }

  return { tasks, addTask, renameTask, changePriority, toggleTask, changeDueDate, deleteTask }
}
