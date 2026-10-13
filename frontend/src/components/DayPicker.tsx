import { dayNames } from '../dates'

/**
 * Adds the item to the list, or removes it if it's already there.
 *
 * @returns A new list. The original is never changed, because React only
 * notices a change when it gets a new array.
 */
function toggleInList(list: string[], item: string): string[] {
  if (list.includes(item)) {
    return list.filter(x => x !== item)
  }
  return [...list, item]
}

type Props = {
  /** Day names that are switched on, like "Monday". */
  selected: string[]
  /** Called with the new list when a day is clicked. */
  onChange: (days: string[]) => void
}

/**
 * The 7 day buttons. Used by the add box and the routine edit form.
 *
 * @remarks
 * It holds no state of its own. The parent owns the selected days,
 * which is why both forms can reuse it.
 */
export function DayPicker({ selected, onChange }: Props) {
  return (
    <>
      {dayNames.map(day => (
        <button
          key={day}
          className={selected.includes(day) ? 'day-button selected' : 'day-button'}
          onClick={() => onChange(toggleInList(selected, day))}
        >
          {day.slice(0, 3)}
        </button>
      ))}
    </>
  )
}
