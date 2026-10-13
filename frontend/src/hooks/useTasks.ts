import { useState, useEffect } from 'react'
import { z } from 'zod'
import { API_URL } from '../config'
import { TaskSchema, type Task } from '../schemas'

/** The server's name for each priority. The position matches Task.priority (0, 1, 2). */
const priorityNames = ['low', 'medium', 'high']

/**
 * Holds the tasks, and every server call that changes them.
 *
 * @remarks
 * Every change goes to the server first. The screen only changes once the server
 * says OK, so it never shows something that wasn't saved.
 * If the server says no, its error message is shown in an alert.
 *
 * @returns The tasks, plus one function per action.
 */
export function useTasks() {
  const [tasks, setTasks] = useState<Task[]>([])

  // Load the tasks once, when the page opens. The empty [] means "run one time".
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

  /**
   * Adds a task.
   *
   * @param title - The title the user typed.
   * @returns true if it was saved. false if not (the error was already shown).
   * AddBox uses this to decide whether to clear the input.
   */
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
        // The server sends back the saved task (with its new id), so we check it and use it.
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

  /**
   * Renames a task.
   *
   * @returns true if it was saved. false if not (the error was already shown).
   */
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

  /**
   * Changes a task's priority.
   *
   * @param newPriority - 0 = low, 1 = medium, 2 = high.
   */
  function changePriority(task: Task, newPriority: number) {
    fetch(`${API_URL}/tasks/${task.id}/priority`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      // The server wants the name ("high"), not the number.
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

  /** Ticks a task, or unticks it if it's already done. */
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

  /**
   * Sets or removes a task's due date and time.
   *
   * @param date - "yyyy-MM-dd", or null to remove the due date.
   * @param time - "HH:mm", or null for no time. A time needs a date.
   */
  function changeDueDate(task: Task, date: string | null, time: string | null) {
    fetch(`${API_URL}/tasks/${task.id}/due-date`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      // The server reads an empty string as "remove it".
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

  /** Deletes a task straight away. Unlike routines, there's no confirm: a task has no history to lose. */
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
