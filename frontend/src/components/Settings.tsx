import { useState } from 'react'
import type { ThemeChoice } from '../hooks/useTheme'

/** The three theme buttons, in the order they're shown. */
const themeOptions: { value: ThemeChoice, label: string }[] = [
  { value: 'dark', label: 'Dark' },
  { value: 'light', label: 'Light' },
  { value: 'system', label: 'System' },
]

type Props = {
  /** The current choice. Its button is shown as selected. */
  theme: ThemeChoice
  /** Called when the user picks a theme. */
  onThemeChange: (theme: ThemeChoice) => void
}

/**
 * The Settings button in the top corner, with a small panel.
 * For now the panel only holds the theme choice.
 */
export function Settings({ theme, onThemeChange }: Props) {
  const [open, setOpen] = useState(false)

  return (
    <div className="settings">
      {/* aria-expanded tells screen readers whether the panel is open. */}
      <button className="quiet" onClick={() => setOpen(!open)} aria-expanded={open}>
        Settings
      </button>

      {open && (
        <div className="settings-panel">
          <span className="settings-label">Theme</span>
          <div className="segmented">
            {themeOptions.map(option => (
              <button
                key={option.value}
                className={theme === option.value ? 'selected' : ''}
                onClick={() => onThemeChange(option.value)}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
