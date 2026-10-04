# Todo App — Proof of Concept

A simple to-do list app, built as a learning exercise applying Domain-Driven Design (DDD), Behavior-Driven Development (BDD), and Test-Driven Development (TDD) — meant to be simple rather than fully ready to be shipped, since the goal was to show good judgment about *when* to use these patterns, not to use every pattern available.

Backend: C# (.NET). Frontend: React + TypeScript (no framework ceremony beyond that). No database — tasks live in memory while the server runs.

## What a Task is

Every task has:
- **Title** — required, can't be empty
- **Due date** — optional, includes a specific time and date
- **Priority** — Low, Medium, or High (defaults to Medium)
- **IsDone** — whether it's completed


Two rules are enforced, no matter how a request arrives (UI, or directly against the API):
1. A task's title can't be empty.
2. Priority must be exactly Low, Medium, or High which is translated to numbers in API and backend (0-1-2) — nothing else is accepted.

Actions: create, edit title, edit priority, edit due date, complete, reopen (un-complete), delete.

## What the app actually does (the scenarios)

These were written before any code, and the tests are built directly from them:

- Adding a task with a title
- Adding a task with an empty title (rejected)
- Marking a task done
- Reopening a done task
- Rejecting a priority that doesn't exist
- Changing to an allowed priority
- Deleting a task
- Changing a task's due date

## How it's structured, and why

**`TodoList` is the only way to change a task.** It's what DDD calls an Aggregate Root — the single gatekeeper for everything inside it. You can't reach into a task and change it directly from outside; every change goes through `TodoList`, and the task's own "complete," "reopen," etc. methods are locked (`internal`) so nothing outside the core project can call them directly, even by accident.

**Storage is swappable.** `TodoList` only exposes a handful of methods (add, complete, delete, etc.) — nothing outside it knows or cares that the tasks are just sitting in memory right now. If this needed a real database later, only the inside of `TodoList` would change. The API routes and the entire frontend would stay exactly as they are.

**The API never trusts the caller.** Even though the UI only offers valid priority choices, the backend still checks every request itself. The UI can't reliably prevent someone from calling the API directly (a different frontend, a tool like Postman, a mistake in a future version of the UI) — so the rule has to live on the backend regardless of what the UI allows.

## What was deliberately left out, and why

- **Reminders / notifications** — a real, separate feature (background checks, browser permissions) that wasn't needed to prove the core idea works.
- **Login / accounts** — there's no multi-user concept at all, so there's one shared list for everyone using the app.
- **A real database** — tasks are stored in memory and are lost when the server restarts. Kept this way on purpose, to avoid extra setup that wasn't needed for a proof of concept.
- **Strict date format checking** — the due-date field accepts any format C#'s built-in date parser understands, rather than enforcing one exact format.


## Real corrections made along the way

- **Titles needed to be editable.** The original plan allowed editing priority and due date after a task was created, but not the title itself — an inconsistency, not a deliberate choice. Fixed by adding the same create/edit/validate pattern already used for the other fields.

## Running it

Two terminals, both need to stay open at the same time:

**Backend:**
```
cd backend/TodoApp.Api
dotnet run
```

**Frontend:**
```
cd frontend
npm run dev
```


