import { z } from 'zod'

/**
 * The shape of a task, as the server sends it.
 *
 * @remarks
 * Every server reply is checked against this. If the server ever sends
 * something different, it fails loudly here instead of breaking the screen later.
 */
export const TaskSchema = z.object({
  id: z.string(),
  title: z.string(),
  isDone: z.boolean(),
  /** 0 = low, 1 = medium, 2 = high. */
  priority: z.number().int().min(0).max(2),
  /** "yyyy-MM-dd", or null when there's no due date. */
  dueDate: z.string().nullable(),
  /** "HH:mm:ss", or null. A time only exists together with a date. */
  dueTime: z.string().nullable(),
})

/** A task. The type is built from the schema, so the two can't drift apart. */
export type Task = z.infer<typeof TaskSchema>

/**
 * The shape of a routine, as the server sends it.
 *
 * @remarks
 * isToday, isDoneToday and missedDays aren't stored anywhere.
 * The server works them out on every request.
 */
export const RoutineSchema = z.object({
  id: z.string(),
  title: z.string(),
  /** Day names, like "Monday". */
  days: z.array(z.string()),
  /** "HH:mm:ss", or null when the routine has no time. */
  time: z.string().nullable(),
  /** True when today is one of the routine's days. */
  isToday: z.boolean(),
  /** True when today has been ticked. */
  isDoneToday: z.boolean(),
  /** Scheduled days from the last 6 days that weren't ticked ("yyyy-MM-dd"). They can still be ticked late. */
  missedDays: z.array(z.string()),
})

/** A routine. The type is built from the schema, like Task. */
export type Routine = z.infer<typeof RoutineSchema>

/** A date the user typed. It must look like "yyyy-MM-dd". */
export const DateInputSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/)

/** A title the user typed. It can't be empty or only spaces. Same message as the server. */
export const TitleInputSchema = z.string().trim().min(1, 'Please enter a title.')
