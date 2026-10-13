import { useState, useEffect } from 'react'
import { DateInputSchema, TitleInputSchema, type Task } from '../schemas'
import { nextDateFor } from '../dates'

/** The CSS class for each priority (position = Task.priority). It colors the row's left edge. */
const priorityClasses = ['priority-low', 'priority-medium', 'priority-high']

type Props = {
  tasks: Task[]
  /** Renames a task. Resolves to true when it saved. */
  onRename: (task: Task, title: string) => Promise<boolean>
  /** Ticks or unticks a task. */
  onToggle: (task: Task) => void
  /** Changes the priority: 0 = low, 1 = medium, 2 = high. */
  onChangePriority: (task: Task, priority: number) => void
  /** Sets the due date and time. null for both removes them. */
  onChangeDueDate: (task: Task, date: string | null, time: string | null) => void
  /** Deletes a task. */
  onDelete: (task: Task) => void
}

/**
 * The Tasks section.
 *
 * @remarks
 * It only holds screen state: which task's options are open, and which title is being edited.
 * The tasks themselves come from useTasks, through props.
 */
export function TaskList({ tasks, onRename, onToggle, onChangePriority, onChangeDueDate, onDelete }: Props) {
  const [expandedTaskId, setExpandedTaskId] = useState<string | null>(null)
  const [editingTaskId, setEditingTaskId] = useState<string | null>(null)
  const [editingTitle, setEditingTitle] = useState('')

  // A click anywhere else on the page closes the options panel.
  // The panel and its button call stopPropagation, so clicks on them don't count as "anywhere else".
  useEffect(() => {
    function handleClickOutside() {
      setExpandedTaskId(null)
    }

    document.addEventListener('click', handleClickOutside)

    // Remove the listener when the list goes away.
    return () => {
      document.removeEventListener('click', handleClickOutside)
    }
  }, [])

  /**
   * Saves the edited title when the input loses focus.
   * An empty title cancels the edit and says why.
   */
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
              {/* Clicking the title turns it into an input. Leaving the input saves it. */}
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
                {/*
                  The date and time inputs use defaultValue, so React doesn't control them.
                  The key changes when the saved value changes, which makes React rebuild
                  the input so it shows the new value.
                  They save on blur (leaving the input), so half-typed dates aren't sent.
                  max stops years longer than 4 digits.
                */}
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
                {/* A time with no date gets the next date that time happens (see nextDateFor). */}
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
