import './App.css'
import { todayHeading } from './dates'
import { useTasks } from './hooks/useTasks'
import { useRoutines } from './hooks/useRoutines'
import { useTheme } from './hooks/useTheme'
import { RoutineList } from './components/RoutineList'
import { TaskList } from './components/TaskList'
import { AddBox } from './components/AddBox'
import { Settings } from './components/Settings'

/**
 * The whole page.
 *
 * @remarks
 * App only connects things: the hooks hold the data, the components draw it.
 * Each component gets the data it shows plus `onX` functions to call when the user does X.
 */
function App() {
  const { tasks, addTask, renameTask, toggleTask, changePriority, changeDueDate, deleteTask } = useTasks()
  const { routines, addRoutine, toggleToday, tickMissedDay, updateRoutine, deleteRoutine } = useRoutines()
  const { theme, setTheme } = useTheme()

  return (
    <div className="app">
      <header className="app-header">
        <h1>{todayHeading()}</h1>
        <Settings theme={theme} onThemeChange={setTheme} />
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

export default App
