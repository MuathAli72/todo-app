import { useState, useLayoutEffect } from 'react'

/** The user's theme choice. "system" means follow the phone or computer setting. */
export type ThemeChoice = 'dark' | 'light' | 'system'

/** The localStorage key the choice is saved under. */
const storageKey = 'theme'

/**
 * Reads the saved theme choice.
 *
 * @returns The saved choice, or 'system' if nothing (or something unknown) is saved.
 */
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

/**
 * The theme choice, remembered in this browser.
 *
 * @remarks
 * It puts the actual theme ("dark" or "light") on the page as `<html data-theme="...">`.
 * App.css reads that attribute and swaps its color variables.
 * "system" follows the phone or computer setting, and keeps following it if it changes.
 *
 * @returns theme (the user's choice, can be 'system') and setTheme to change it.
 */
export function useTheme() {
  // Passing the function itself (not calling it) means it only runs on the first render.
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
    // Only "system" needs to listen for the device switching between light and dark.
    // The cleanup removes the listener when the choice changes.
    systemLight.addEventListener('change', apply)
    return () => systemLight.removeEventListener('change', apply)
  }, [theme])

  return { theme, setTheme }
}
