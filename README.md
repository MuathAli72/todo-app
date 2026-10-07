# Todo App

A to-do list app built to practice Domain-Driven Design (DDD), Behavior-Driven Development (BDD), and Test-Driven Development (TDD), plus layered validation, a real database, and debugging.

- **Backend:** C# / .NET, ASP.NET Core minimal API
- **Frontend:** React + TypeScript (Vite)
- **Database:** PostgreSQL, through Entity Framework Core
- **Validation:** Zod (frontend), FluentValidation (API), domain rules, database constraints

## What a task is

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

## How due dates work

- **Date only:** "Pay the bill by Friday."
- **Date and time:** "Meeting Friday at 3pm."
- **Neither:** not every task has a deadline.
- **Time only:** the app uses the next time that clock time comes around. At 9am, picking 3pm means today at 3pm. At 11pm, picking 1am means tomorrow at 1am.

The "time only" convenience lives in the **frontend**, because "today" depends on the user's own clock, which only the user's browser knows. The server could be in another time zone. The backend itself always requires a date with a time, so anything calling the API directly (like Postman) has to send both.

## Behavior scenarios

Written before the code. The tests are built from them.

- Adding a task with a title
- Adding a task with an empty title, or only spaces, is rejected
- Marking a task done, and reopening it
- Changing to an allowed priority
- Changing to a priority that doesn't exist is rejected
- Editing a title, and editing it to empty or only spaces is rejected
- Setting a due date with no time, and with a time
- Setting a time without a date is rejected
- Removing the due date also removes the time
- Deleting a task removes only that task

## How it's built

### Three backend projects
- **TodoApp.Core:** the business rules (`TodoItem`, `TodoList`). Knows nothing about the web or the database.
- **TodoApp.Tests:** tests for those rules. They run without a database.
- **TodoApp.Api:** the web routes, validators, and database code.

Core doesn't reference the other two, so the rules can't accidentally depend on web or database details. The compiler enforces that.

### `TodoList` is the only way to change a task
In DDD terms, it's the **aggregate root**. Outside code asks the list to complete, rename, or delete a task. The task's own change methods are `internal`, so code outside the Core project can't call them, even by accident.

### Saving: load, work, save
For every request, a **repository** loads the tasks from the database into a `TodoList`, the list does its work with all its rules, and the repository saves the changes back. The rules never know a database exists. That's why the tests never needed changing when the database was added.

Database calls use `async`/`await`, so the server can handle other requests while it waits for the database.

### Four layers of validation

| Layer | Where | What it's for |
|---|---|---|
| Zod | Frontend | Instant feedback on titles and dates. Also checks that tasks coming back from the backend have the expected shape |
| FluentValidation | API | Checks each request is readable before anything else runs |
| Domain rules | `TodoItem` / `TodoList` | The real business rules. The authority |
| CHECK constraints | PostgreSQL | Protect the stored data, even if something writes to the database without going through the backend |

The frontend check is for speed. The backend and database checks are the ones that can't be skipped.

### The database table

One table, `Tasks`:

| Column | Type | Notes |
|---|---|---|
| Id | uuid | Primary key |
| Title | text | Not empty, not only spaces |
| IsDone | boolean | |
| Priority | integer | 0 = Low, 1 = Medium, 2 = High |
| DueDate | date | Optional |
| DueTime | time | Optional, only with a date |
| CreatedAt | timestamp with time zone | Stored in UTC |

The table is created from the C# classes using EF Core **migrations**, so anyone can recreate it with one command.

## Bugs found, and how

- **Rejected tasks showed up as blank tasks.** Stepping through the backend with the Visual Studio debugger showed the backend was correctly rejecting empty titles. The real problem was the frontend, which treated the error response as if it were a new task. Fixed by checking every response for success before using it.
- **A priority of "5" was accepted.** C#'s `Enum.TryParse` accepts numbers, not just names, and doesn't check the number is a real priority. Found by testing an unusual input instead of only the obvious wrong one. Fixed in the domain and in the validator, which now only accepts the words.
- **A due date without a time couldn't be saved,** and a half-filled date box could erase an existing date. The browser's combined date-and-time box reports itself as empty until both parts are filled. This led to splitting date and time into separate optional fields, and adding a Remove date button.
- **Typing a year got interrupted after one digit,** and actually saved years like 0002. The app saved on every keystroke, which redrew the box mid-typing. Fixed by saving only when the user leaves the box.
- **The date box froze the page.** An invalid year failed to save but stayed in the box, so every click elsewhere sent it again and showed another error. Fixed by checking the date first and putting the saved value back when it's invalid.

## Known limitations, on purpose

- **No reminders or notifications.** A separate feature that wasn't needed to prove the core idea.
- **No accounts.** There's one shared list. A multi-user version would load and save a list per user.
- **The API sends priority as a number but accepts it as a word.** It works, but sending words both ways would be tidier.
- **The API accepts any date format C# can read.** The frontend always sends YYYY-MM-DD.
- **Styling was done last.** Functionality was built and tested first.

## Running it

**You need:** the .NET 10 SDK, Node.js, and PostgreSQL.

**1. Point the backend at your database.** From `backend/TodoApp.Api`, store your connection string as a user secret, so the password never goes into Git:

```
dotnet user-secrets set "ConnectionStrings:TodoDb" "Host=localhost;Port=5432;Database=todoapp;Username=postgres;Password=YOUR_PASSWORD"
```

**2. Create the database table.** In Visual Studio, open the Package Manager Console with `TodoApp.Api` as the default project and run `Update-Database`. Or from the command line:

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

- **Repeating tasks** for daily and weekly routines, like "gym at 5 on Mon/Wed/Fri" or "brush teeth twice a day." The hard part is that "done" becomes "done on a given day," which needs a separate record of completions. That would also be the first real cross-task rule in the app, the point where the aggregate root earns its place beyond demonstrating the pattern.
