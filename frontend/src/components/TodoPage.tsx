import { todayHeading } from '../dates'
import { useTasks } from '../hooks/useTasks'
import { useRoutines } from '../hooks/useRoutines'
import type { useTheme } from '../hooks/useTheme'
import { RoutineList } from './RoutineList'
import { TaskList } from './TaskList'
import { AddBox } from './AddBox'
import { Settings } from './Settings'

/** Whatever theme values useTheme works with (dark, light, system). */
type Theme = ReturnType<typeof useTheme>['theme']

type Props = {
  email: string
  theme: Theme
  onThemeChange: (theme: Theme) => void
  onLogout: () => void
}

/**
 * The to-do page for a logged-in user (what App used to show).
 *
 * @remarks
 * It's its own component so the task and routine hooks only start loading
 * once someone is logged in.
 */
export function TodoPage({ email, theme, onThemeChange, onLogout }: Props) {
  const { tasks, addTask, renameTask, toggleTask, changePriority, changeDueDate, deleteTask } = useTasks()
  const { routines, addRoutine, toggleToday, tickMissedDay, updateRoutine, deleteRoutine } = useRoutines()

  return (
    <div className="app">
      <header className="app-header">
        <h1>{todayHeading()}</h1>
        <div className="header-actions">
          <Settings theme={theme} onThemeChange={onThemeChange} />
          {/* The email shows on hover, so it's there when needed without cluttering the page. */}
          <button type="button" className="logout-button" title={`Logged in as ${email}`} onClick={onLogout}>
            Log out
          </button>
        </div>
      </header>

      <RoutineList
        routines={routines}
        onToggleToday={toggleToday}
        onTickMissedDay={tickMissedDay}
        onUpdate={updateRoutine}
        onDelete={deleteRoutine}
      />

      <TaskList
        tasks={tasks}
        onRename={renameTask}
        onToggle={toggleTask}
        onChangePriority={changePriority}
        onChangeDueDate={changeDueDate}
        onDelete={deleteTask}
      />

      <AddBox onAddTask={addTask} onAddRoutine={addRoutine} />
    </div>
  )
}
