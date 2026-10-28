import './App.css'
import { useAuth } from './hooks/useAuth'
import { useTheme } from './hooks/useTheme'
import { LoginScreen } from './components/LoginScreen'
import { TodoPage } from './components/TodoPage'

/**
 * The whole page.
 *
 * @remarks
 * App only decides WHAT to show: nothing while checking the login, the login screen
 * when nobody is logged in (with the current theme's scene), or the to-do page.
 * The theme lives here so both screens follow it.
 */
function App() {
  const { email, checking, login, register, logout } = useAuth()
  const { theme, actualTheme, setTheme } = useTheme()

  if (checking) {
    return null
  }
  if (!email) {
    return <LoginScreen theme={actualTheme} onLogin={login} onRegister={register} />
  }
  return <TodoPage email={email} theme={theme} onThemeChange={setTheme} onLogout={() => void logout()} />
}

export default App
