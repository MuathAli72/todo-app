# Todo App

A to-do app with one-off **tasks** and repeating **routines**, built to practice Domain-Driven Design (DDD), Behavior-Driven Development (BDD), and Test-Driven Development (TDD), plus layered validation, a real database, and debugging.

- **Backend:** C# / .NET, ASP.NET Core minimal API
- **Frontend:** React + TypeScript (Vite)
- **Database:** PostgreSQL, through Entity Framework Core
- **Validation:** Zod (frontend), FluentValidation (API), domain rules, database constraints

## What the app does

The page opens on **today**. At the top are today's routines, with anything missed this week. Below them are your tasks. Everything is added from one box at the bottom: type a title and click Add for a task, or turn on **Repeat**, pick days, and click Add for a routine.

## Tasks

| Field | Required? | Notes |
|---|---|---|
| Title | Yes | Can't be empty or only spaces |
| Priority | Yes | Low, Medium, or High. Defaults to Medium |
| IsDone | Yes | Starts as not done |
| Due date | No | A day, with no time attached |
| Due time | No | Only allowed if there's a due date |
| Created at | Automatic | Used to keep tasks in the order they were made |

There's no description field. A to-do list is about getting things done, not explaining them, so it was left out on purpose.

**Rules** (enforced no matter how a request arrives):
1. A title can't be empty or only spaces.
2. Priority must be Low, Medium, or High. Nothing else, including numbers.
3. A time can't exist without a date.

**Actions:** add, edit title, change priority, set or remove due date and time, complete, reopen, delete.

### How due dates work

- **Date only:** "Pay the bill by Friday."
- **Date and time:** "Meeting Friday at 3pm."
- **Neither:** not every task has a deadline.
- **Time only:** the app uses the next time that clock time comes around. At 9am, picking 3pm means today at 3pm. At 11pm, picking 1am means tomorrow at 1am.

The "time only" convenience lives in the **frontend**, because "today" depends on the user's own clock, which only the user's browser knows. The backend itself always requires a date with a time, so anything calling the API directly (like Postman) has to send both.

## Routines

A routine is something you do on set days of the week, like "Gym on Mon, Wed, Fri" or "Read every day". Instead of being done once, it's done **per day**.

| Field | Required? | Notes |
|---|---|---|
| Title | Yes | Can't be empty or only spaces |
| Days | Yes | At least one day of the week |
| Time | No | Shown next to the routine. Not used for any rule yet |
| Created on | Automatic | The day the routine was made. That day counts |
| Completions | Automatic | One record per day you ticked it |

**Rules** (enforced in the domain, so Postman and the app behave the same):
1. A title can't be empty, and a routine needs at least one day.
2. You can only tick a day the routine is scheduled on.
3. You can't tick a day in the future.
4. You can't tick a day before the routine existed, or before its days were last changed.
5. You can tick a day late, but only up to **6 days back**. Older days are locked.
6. Ticking the same day twice does nothing. It's still ticked once.

**Missed days** are the scheduled days in the last 6 days (not counting today) that weren't ticked. They aren't stored anywhere. They're calculated every time from the schedule and the completions, so they can never go out of sync. Each missed day shows as a button you can click to tick it late.

**Actions:** add, edit title / days / time, tick and untick today, tick a missed day, delete.

## Design decisions

Each of these was a choice between options. The alternative is listed so the trade-off is clear.

### Backend

| We did | Because | The alternative was | Which would have cost |
|---|---|---|---|
| Made `Routine` its own aggregate, not a kind of task | A task is done once, and a routine is done per day. A routine passed around as a task would break code that expects "done" to mean done forever | `Routine` inheriting from `TodoItem` | Every task rule having special cases for routines |
| Calculated missed days instead of storing them | One source of truth: the schedule plus the completions | A "missed" record per day | A background job, and records that can disagree with the schedule |
| Limited late ticks to 6 days, in the domain | It matches the missed-days window. The rule lives in one place, so Postman can't do more than the app | Enforcing it only in the frontend | A back door: any direct API call could tick a day from months ago |
| Restarted missed-day tracking when a routine's days change | Simple and honest: we only judge days under the current schedule | Saving the full history of schedule changes | A new table, and more complex rules for every past day |
| Stored days as text in one column ("Monday,Wednesday,Friday") | Days are always read and written together, as one value | A separate table with one row per day | An extra table and join for no real benefit |
| Took "today" from the server's clock | The server runs on the user's own machine, so its clock is the user's clock | Sending "today" from the browser on every request | More moving parts. Revisit if the server ever moves to another time zone |
| Ticking is `POST` (creates a completion), unticking is `DELETE` | A tick creates a record, so it's a create, not an edit | `PATCH` on the routine | Pretending a new record is a change to an old one |
| A double tick returns success instead of an error | Two clicks or a retried request shouldn't fail. A unique index in the database backs this up | Returning an error on the second tick | Error messages for something that isn't a problem |
| One `GET /routines` returns each routine with "is it today", "done today", and missed days already worked out | The frontend shows the numbers and never recalculates the rules | The frontend calculating them | The same rule living in two places |

### Frontend

| We did | Because | The alternative was | Which would have cost |
|---|---|---|---|
| Routines show only today's, plus any with missed days, with a "Show all" button | People open a to-do app to see what to do today | Showing every routine all the time | Clutter that grows with every routine |
| Routines are added from the same box as tasks, with a Repeat option | Two similar text boxes would confuse people. Apple Reminders and Microsoft To Do also treat "repeat" as an option on a normal item, not a separate form | A separate "New routine" form | A second text box doing almost the same job |
| A short message appears when you add a routine that isn't on today | Otherwise it seems to vanish, since the page only shows today | Switching to "Show all" automatically | Changing the view without the user asking |
| Editing a routine uses a small form with Save and Cancel | Changing days restarts missed-day tracking, so it should only happen on purpose. Cancel is a real undo | Saving every change instantly, like tasks | One mis-click wiping the week's missed days |
| Deleting a routine asks first, deleting a task doesn't | A routine carries history that can't be recreated. A task takes seconds to retype | Confirming every delete | People learning to click OK without reading, so the confirm that matters stops working |
| Color is only used when it means something: green for done or low, yellow for medium or missed, red for high or delete | The priority colors stand out instead of fighting the rest of the page | A colored theme | Priority colors clashing with decoration |

## How it's built

### Three backend projects
- **TodoApp.Core:** the business rules (`TodoItem`, `TodoList`, `Routine`, `Completion`). Knows nothing about the web or the database.
- **TodoApp.Tests:** tests for those rules. They run without a database.
- **TodoApp.Api:** the web routes, validators, and database code.

Core doesn't reference the other two, so the rules can't accidentally depend on web or database details. The compiler enforces that.

### Aggregates: who guards which rules
In DDD terms, an **aggregate root** is the one object you go through to change something, so its rules can't be skipped.

- **`TodoList`** guards the tasks. Outside code asks the list to complete, rename, or delete a task. The task's own change methods are `internal`, so code outside Core can't call them, even by accident.
- **Each `Routine`** guards its own completions. A `Completion` can only be created by its routine, which checks every tick rule first.

### Saving: load, work, save
For every request, a **repository** loads the data from the database, the domain objects do their work with all their rules, and the repository saves the changes back. The rules never know a database exists. That's why the task tests never needed changing when the database was added.

`today` is passed into the routine's methods instead of being read from the clock inside them. That keeps the rules testable: a test can say "today is Monday the 12th" and check exactly what happens.

Database calls use `async`/`await`, so the server can handle other requests while it waits for the database.

### Four layers of validation

| Layer | Where | What it's for |
|---|---|---|
| Zod | Frontend | Instant feedback on titles and dates. Also checks that tasks and routines coming back from the backend have the expected shape |
| FluentValidation | API | Checks each request is readable before anything else runs: real day names, real times, dates as YYYY-MM-DD |
| Domain rules | `TodoItem`, `TodoList`, `Routine` | The real business rules. The authority |
| Constraints | PostgreSQL | Protect the stored data, even if something writes to the database without going through the backend |

The frontend check is for speed. The backend and database checks are the ones that can't be skipped.

### The API

| Method | Route | What it does |
|---|---|---|
| GET | `/tasks` | All tasks |
| POST | `/tasks` | Add a task |
| PATCH | `/tasks/{id}/title` | Rename |
| PATCH | `/tasks/{id}/priority` | Change priority (`low`, `medium`, `high`) |
| PATCH | `/tasks/{id}/due-date` | Set or remove the due date and time |
| PATCH | `/tasks/{id}/complete` and `/reopen` | Mark done or not done |
| DELETE | `/tasks/{id}` | Delete a task |
| GET | `/routines` | All routines, with today's status and missed days |
| POST | `/routines` | Add a routine |
| PATCH | `/routines/{id}/title`, `/days`, `/time` | Edit one part of a routine |
| DELETE | `/routines/{id}` | Delete a routine and its history |
| POST | `/routines/{id}/completions` | Tick a day (`{ "date": "2026-10-12" }`) |
| DELETE | `/routines/{id}/completions/{date}` | Untick a day |

### The database

**Tasks**

| Column | Type | Notes |
|---|---|---|
| Id | uuid | Primary key |
| Title | text | Not empty, not only spaces |
| IsDone | boolean | |
| Priority | integer | 0 = Low, 1 = Medium, 2 = High |
| DueDate | date | Optional |
| DueTime | time | Optional, only with a date |
| CreatedAt | timestamp with time zone | Stored in UTC |

**Routines**

| Column | Type | Notes |
|---|---|---|
| Id | uuid | Primary key |
| Title | text | Not empty, not only spaces |
| Days | text | For example "Monday,Wednesday,Friday". Can't be empty |
| Time | time | Optional |
| CreatedOn | date | |
| ScheduleStartsOn | date | Moves to today when the days change |

**Completions**

| Column | Type | Notes |
|---|---|---|
| Id | uuid | Primary key |
| RoutineId | uuid | Belongs to a routine. Deleted with it |
| Date | date | The day that was ticked |
| CompletedAt | timestamp with time zone | When it was ticked, in UTC |

A routine can only have one completion per date. A unique index on (RoutineId, Date) enforces it.

The tables are created from the C# classes using EF Core **migrations**, so anyone can recreate them with one command.

## Behavior scenarios

Written before the code. The tests are built from them.

**Tasks**
- Adding a task with a title, and with an empty title or only spaces (rejected)
- Marking a task done, and reopening it
- Changing to an allowed priority, and to one that doesn't exist (rejected)
- Editing a title, and editing it to empty or only spaces (rejected)
- Setting a due date with no time, and with a time
- Setting a time without a date (rejected)
- Removing the due date also removes the time
- Deleting a task removes only that task

**Routines**
- Creating a routine, and creating one with no days or an empty title (rejected)
- Ticking today, ticking a missed day late, and unticking a day
- The same tick arriving twice only counts once
- Ticking a day the routine isn't on, a future day, or a day before it existed (rejected)
- Ticking a day 6 days ago works, but a day 7 days ago is rejected
- A missed day shows up, today isn't missed yet, and a late tick removes it from missed
- A missed day disappears after a week
- The day a routine is created counts
- Editing the title, and editing it to empty (rejected)
- Changing days keeps old completions, doesn't make past days missed, and can't leave zero days

## Bugs found, and how

- **Rejected tasks showed up as blank tasks.** Stepping through the backend with the Visual Studio debugger showed the backend was correctly rejecting empty titles. The real problem was the frontend, which treated the error response as if it were a new task. Fixed by checking every response for success before using it.
- **A priority of "5" was accepted.** C#'s `Enum.TryParse` accepts numbers, not just names, and doesn't check the number is a real priority. Found by testing an unusual input instead of only the obvious wrong one. Fixed in the domain and in the validator, which now only accepts the words. The routine validator was written the same way from the start, so "1" isn't accepted as a day.
- **A due date without a time couldn't be saved,** and a half-filled date box could erase an existing date. The browser's combined date-and-time box reports itself as empty until both parts are filled. This led to splitting date and time into separate optional fields, and adding a Remove date button.
- **Typing a year got interrupted after one digit,** and actually saved years like 0002. The app saved on every keystroke, which redrew the box mid-typing. Fixed by saving only when the user leaves the box.
- **The date box froze the page.** An invalid year failed to save but stayed in the box, so every click elsewhere sent it again and showed another error. Fixed by checking the date first and putting the saved value back when it's invalid.
- **The API allowed more than the app.** The page only offers late ticks for the last 6 days, but the domain had no such limit, so Postman could tick a day from weeks ago. Found while writing the tick route, by comparing the agreed rule with what the domain actually checked. Fixed by moving the 6-day rule into the domain, with tests for day 6 (allowed) and day 7 (rejected).

## Known limitations, on purpose

- **No reminders or notifications.** A routine's time is shown, but nothing happens at that time yet.
- **No accounts.** There's one shared list. A multi-user version would load and save per user.
- **Changing a routine's days restarts its missed-day tracking.** Past ticks are kept, but days before the change are no longer judged.
- **A late tick can't be undone from the page.** Once a missed day is ticked, its button disappears. The history view (below) would fix this.
- **"Today" comes from the server's clock.** Fine while the server runs on the user's machine.
- **The API sends priority as a number but accepts it as a word.** It works, but sending words both ways would be tidier.
- **The task API accepts any date format C# can read.** The frontend always sends YYYY-MM-DD. The routine tick route only accepts YYYY-MM-DD.
- **Styling was done last.** Functionality was built and tested first.

## Running it

**You need:** the .NET 10 SDK, Node.js, and PostgreSQL.

**1. Point the backend at your database.** From `backend/TodoApp.Api`, store your connection string as a user secret, so the password never goes into Git:

```
dotnet user-secrets set "ConnectionStrings:TodoDb" "Host=localhost;Port=5432;Database=todoapp;Username=postgres;Password=YOUR_PASSWORD"
```

**2. Create the database tables.** In Visual Studio, open the Package Manager Console with `TodoApp.Api` as the default project and run `Update-Database`. Or from the command line:

```
dotnet tool install --global dotnet-ef
dotnet ef database update --project backend/TodoApp.Api
```

**3. Start the backend:** press F5 in Visual Studio, or run `dotnet run` in `backend/TodoApp.Api`. It listens on `http://localhost:5149`.

**4. Start the frontend:**

```
cd frontend
npm install
npm run dev
```

Then open the address it prints, usually `http://localhost:5173`.

**Tests:** Test Explorer in Visual Studio, or `dotnet test` in `backend`.

## Future ideas

- **Progress page.** Streaks, best streak, total completions, this week vs last week, and the last 30 days as squares, per routine. The 6-day late tick already works as a built-in "streak saver."
- **History view.** The last 30 days of each routine, which would also let you undo a mis-clicked late tick.
- **Schedule history.** Save every change to a routine's days, so stats and missed days stay accurate across changes. This pays back the shortcut described under "Design decisions."
- **Undo or a trash bin** for deletes, instead of a confirm step.
- **Reminders** using a routine's time.
- **Accounts,** so each person has their own list.
