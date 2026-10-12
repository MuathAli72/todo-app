import { z } from 'zod'

export const TaskSchema = z.object({
  id: z.string(),
  title: z.string(),
  isDone: z.boolean(),
  priority: z.number().int().min(0).max(2),
  dueDate: z.string().nullable(),
  dueTime: z.string().nullable(),
})

export type Task = z.infer<typeof TaskSchema>

export const RoutineSchema = z.object({
  id: z.string(),
  title: z.string(),
  days: z.array(z.string()),
  time: z.string().nullable(),
  isToday: z.boolean(),
  isDoneToday: z.boolean(),
  missedDays: z.array(z.string()),
})

export type Routine = z.infer<typeof RoutineSchema>

export const DateInputSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/)
export const TitleInputSchema = z.string().trim().min(1, 'Please enter a title.')
