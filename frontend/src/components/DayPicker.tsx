import { dayNames } from '../dates'

function toggleInList(list: string[], item: string): string[] {
  if (list.includes(item)) {
    return list.filter(x => x !== item)
  }
  return [...list, item]
}

type Props = {
  selected: string[]
  onChange: (days: string[]) => void
}

// The 7 day buttons. Used by the add box and the routine edit form.
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
