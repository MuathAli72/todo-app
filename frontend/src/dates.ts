/**
 * Day names in JavaScript's order: getDay() gives 0 for Sunday.
 * Same spelling the server uses, so they can be sent as they are.
 */
export const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

/**
 * Turns a Date into "yyyy-MM-dd", using the user's own clock.
 *
 * @remarks
 * We don't use toISOString(), because it converts to UTC first.
 * In Riyadh, between midnight and 3 am, that would give yesterday's date.
 */
export function formatDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

/**
 * A short label for a date, like "Mon 12 Oct". Used on the missed-day buttons.
 *
 * @param date - A date as "yyyy-MM-dd".
 */
export function dayLabel(date: string): string {
  // Built from parts on purpose: new Date("2026-10-12") reads the text as UTC,
  // which shows the wrong day in some time zones.
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString('en-GB', {
    weekday: 'short', day: 'numeric', month: 'short'
  })
}

/**
 * The next date a clock time happens: today if it hasn't passed yet, otherwise tomorrow.
 *
 * @remarks
 * Used when a task gets a time but no date, because the server needs a date with every time.
 *
 * @param time - A time as "HH:mm".
 * @returns A date as "yyyy-MM-dd".
 */
export function nextDateFor(time: string): string {
  const [hours, minutes] = time.split(':').map(Number)
  const target = new Date()
  target.setHours(hours, minutes, 0, 0)

  if (target < new Date()) {
    target.setDate(target.getDate() + 1)
  }

  return formatDate(target)
}

/** Today's date for the page heading, like "Tuesday 13 October". */
export function todayHeading(): string {
  return new Date().toLocaleDateString('en-GB', {
    weekday: 'long', day: 'numeric', month: 'long'
  })
}
