import { useState, useLayoutEffect, useSyncExternalStore } from 'react'

/** The themes that exist. Each one changes the whole look, not only the colors. */
export type ThemeName = 'ocean' | 'aurora' | 'desert'

/** The user's choice: a theme, or "system" (Ocean on a dark device, Desert on a light one). */
export type ThemeChoice = ThemeName | 'system'

/** The localStorage key the choice is saved under. */
const storageKey = 'theme'

/**
 * Reads the saved theme choice.
 *
 * @remarks
 * Older versions saved "dark" or "light". Those become Ocean and Desert, the closest new themes.
 *
 * @returns The saved choice, or 'system' if nothing (or something unknown) is saved.
 */
function readSavedTheme(): ThemeChoice {
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved === 'ocean' || saved === 'aurora' || saved === 'desert' || saved === 'system') {
      return saved
    }
    if (saved === 'dark') {
      return 'ocean'
    }
    if (saved === 'light') {
      return 'desert'
    }
  } catch {
    // Storage can be blocked (private mode). Then we just don't remember.
  }
  return 'system'
}

/** The theme actually shown for a choice. */
function resolve(choice: ThemeChoice, systemLight: boolean): ThemeName {
  if (choice === 'system') {
    return systemLight ? 'desert' : 'ocean'
  }
  return choice
}

/** The device setting this app follows for "system". */
const lightQuery = '(prefers-color-scheme: light)'

/** Lets React listen for the device switching between light and dark. */
function subscribeToSystem(onChange: () => void) {
  const media = window.matchMedia(lightQuery)
  media.addEventListener('change', onChange)
  return () => media.removeEventListener('change', onChange)
}

/**
 * The theme choice, remembered in this browser.
 *
 * @remarks
 * It puts the actual theme on the page as `<html data-theme="ocean">` (or aurora, desert).
 * App.css reads that attribute and swaps its whole set of variables: colors, corners, shadows.
 * The login page also uses `actualTheme` to pick which illustrated scene to show.
 * "system" follows the phone or computer setting, and keeps following it if it changes.
 *
 * @returns theme (the user's choice, can be 'system'), actualTheme (what's shown) and setTheme.
 */
export function useTheme() {
  // Passing the function itself (not calling it) means it only runs on the first render.
  const [theme, setTheme] = useState<ThemeChoice>(readSavedTheme)
  // Whether the device is in light mode right now. Re-renders by itself when that changes.
  const systemLight = useSyncExternalStore(subscribeToSystem, () => window.matchMedia(lightQuery).matches)
  // Worked out on every render, not stored, so it can't get out of sync.
  const actualTheme = resolve(theme, systemLight)

  // useLayoutEffect runs before the page is painted, so there's no flash of the wrong theme.
  useLayoutEffect(() => {
    document.documentElement.dataset.theme = actualTheme
    try {
      localStorage.setItem(storageKey, theme)
    } catch {
      // Same as above: if storage is blocked, the choice lasts until the page closes.
    }
  }, [theme, actualTheme])

  return { theme, actualTheme, setTheme }
}
