import { useState, useLayoutEffect } from 'react'

export type ThemeChoice = 'dark' | 'light' | 'system'

const storageKey = 'theme'

function readSavedTheme(): ThemeChoice {
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved === 'dark' || saved === 'light' || saved === 'system') {
      return saved
    }
  } catch {
    // Storage can be blocked (private mode). Then we just don't remember.
  }
  return 'system'
}

// The theme choice, remembered in this browser.
// "system" follows the phone or computer setting, and keeps following it if it changes.
export function useTheme() {
  const [theme, setTheme] = useState<ThemeChoice>(readSavedTheme)

  // useLayoutEffect runs before the page is painted, so there's no flash of the wrong theme.
  useLayoutEffect(() => {
    const systemLight = window.matchMedia('(prefers-color-scheme: light)')

    function apply() {
      const actual = theme === 'system' ? (systemLight.matches ? 'light' : 'dark') : theme
      document.documentElement.dataset.theme = actual
    }

    apply()

    try {
      localStorage.setItem(storageKey, theme)
    } catch {
      // Same as above: if storage is blocked, the choice lasts until the page closes.
    }

    if (theme !== 'system') {
      return
    }
    systemLight.addEventListener('change', apply)
    return () => systemLight.removeEventListener('change', apply)
  }, [theme])

  return { theme, setTheme }
}
