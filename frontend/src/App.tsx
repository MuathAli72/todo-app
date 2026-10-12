import './App.css'
import { todayHeading } from './dates'
import { useTasks } from './hooks/useTasks'
import { useRoutines } from './hooks/useRoutines'
import { RoutineList } from './components/RoutineList'
import { TaskList } from './components/TaskList'
import { AddBox } from './components/AddBox'

// App only connects things: the hooks hold the data, the components draw it.
function App() {
  const { tasks, addTask, renameTask, toggleTask, changePriority, changeDueDate, deleteTask } = useTasks()
  const { routines, addRoutine, toggleToday, tickMissedDay, updateRoutine, deleteRoutine } = useRoutines()

  return (
    <div className="app">
      <h1>{todayHeading()}</h1>

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
