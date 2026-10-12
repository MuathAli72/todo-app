import { useState, useEffect } from 'react'
import { DateInputSchema, TitleInputSchema, type Task } from '../schemas'
import { nextDateFor } from '../dates'

const priorityClasses = ['priority-low', 'priority-medium', 'priority-high']

type Props = {
  tasks: Task[]
  onRename: (task: Task, title: string) => Promise<boolean>
  onToggle: (task: Task) => void
  onChangePriority: (task: Task, priority: number) => void
  onChangeDueDate: (task: Task, date: string | null, time: string | null) => void
  onDelete: (task: Task) => void
}

// The Tasks section. Holds only screen state: what's open and what's being typed.
export function TaskList({ tasks, onRename, onToggle, onChangePriority, onChangeDueDate, onDelete }: Props) {
  const [expandedTaskId, setExpandedTaskId] = useState<string | null>(null)
  const [editingTaskId, setEditingTaskId] = useState<string | null>(null)
  const [editingTitle, setEditingTitle] = useState('')

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
    const check = TitleInputSchema.safeParse(editingTitle)
    if (!check.success) {
      setEditingTaskId(null)
      alert(check.error.issues[0].message)
      return
    }
    onRename(task, editingTitle).then(ok => {
      if (ok) {
        setEditingTaskId(null)
      }
    })
  }

  return (
    <section className="tasks">
      <h2>Tasks</h2>
      <ul className="task-list">
        {tasks.map(task => (
          <li
            key={task.id}
            className={`task ${priorityClasses[task.priority]} ${task.isDone ? 'is-done' : ''}`}
          >
            <div className="task-row">
              <input
                type="checkbox"
                checked={task.isDone}
                onChange={() => onToggle(task)}
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
              <button className="danger" onClick={() => onDelete(task)}>Delete</button>
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
                  onChange={e => onChangePriority(task, Number(e.target.value))}
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
                    onChangeDueDate(task, date, task.dueTime)
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
                      if (task.dueDate) onChangeDueDate(task, task.dueDate, null)
                      return
                    }
                    onChangeDueDate(task, task.dueDate ?? nextDateFor(time), time)
                  }}
                />
                {task.dueDate && (
                  <button onClick={() => onChangeDueDate(task, null, null)}>Remove date</button>
                )}
              </div>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
