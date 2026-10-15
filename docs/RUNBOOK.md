# CI/CD Runbook: Todo App

How the automatic pipelines work, how to check things on your own computer, and what to do when something breaks.

**Quick links**
- Live site: https://muathali72.github.io/todo-app/
- Live API: https://todo-app-yaw8.onrender.com (health check: `/health`)
- Pipeline runs: the repo's **Actions** tab
- Hosting dashboards: Render (API), Neon (database), repo **Settings → Pages** (frontend)

---

## 1. Overview

Every change goes through two automatic pipelines, one for the backend and one for the frontend. Each runs on a fresh GitHub computer: it scans the code for naming and style, builds it, and tests it. If everything passes and the change is merged to main, the pipeline deploys it:

- **Backend:** update the Neon database (migrations) → tell Render to deploy → Render switches to the new version only if `/health` answers.
- **Frontend:** build the live version → publish it on GitHub Pages.

Nothing deploys unless every check is green.

```
Pull request ──> Backend CI  (restore → scan → build → test → Docker image)  ─┐
             └─> Frontend CI (install → scan → type-check + build)          ─┴─> both ✅ → merge allowed

Merge to main ─> CI again ─> Backend deploy:  migrations → Render
                          └> Frontend deploy: build live version → GitHub Pages
```

## 2. When they run

| Event | What runs | Deploys? |
|---|---|---|
| Pull request into main | Both pipelines' checks, always | No. Deploy jobs show as *skipped* |
| Merge to main | Checks, then deploy, only for the side that changed | Yes |

Pull requests always run both pipelines, even if only one side changed. Main requires both checks, so a check that never ran would leave the pull request stuck on **"Expected"** forever.

Main is protected (repo **Settings → Rules**): every change needs a pull request, the checks **Build and test** and **Type-check and build** must pass, and force pushes and deleting main are blocked.

## 3. The backend pipeline

File: `.github/workflows/backend.yml`

**Job: Build and test** (runs on every pull request and every merge to main)

| Step | What it does | If it fails, it usually means |
|---|---|---|
| Restore packages | Downloads the NuGet packages onto the fresh computer | A package version can't be found, or two packages need conflicting versions |
| Scan naming and code style | Checks naming and formatting against the rules in `.editorconfig` (Microsoft's C# conventions) | A naming rule or formatting rule was broken. See [section 5](#5-the-scanners) |
| Build | Compiles the code (Release mode) | A compile error: wrong syntax, a missing type, a typo in a name |
| Run tests | Runs the unit tests. They don't need a database, so they run anywhere | **The code broke a rule a test protects**: the test found a real bug. Less often, the test itself is wrong |
| Build Docker image | Builds the container from `backend/Dockerfile`, the same recipe Render uses | Something makes it work on your machine but isn't in the repo, or the Dockerfile itself is wrong (for example, a new project that the Dockerfile doesn't copy) |

**Job: Deploy to Render** (only after a merge to main, only if the job above passed)

| Step | What it does | If it fails, it usually means |
|---|---|---|
| Restore packages | Same as above. Every job runs on its own fresh computer, so packages must be restored again | Same as above |
| Install the EF Core tool | Installs `dotnet-ef`, the command-line version of Update-Database | A temporary download problem. Re-run the job |
| Update the live database | Applies any new migrations to the Neon database, **before** the new code goes live | A broken migration, a database setup that can't be built, a wrong `DATABASE_CONNECTION_STRING` secret, or Neon is down. **The deploy below doesn't run**, so the live app is unchanged |
| Tell Render to deploy | Presses Render's secret "deploy now" button (the deploy hook) | The `RENDER_DEPLOY_HOOK_URL` secret is wrong or missing, or Render is down |

After the button is pressed, Render builds the Dockerfile and switches over in a few minutes. Watch it in **Render → todo-app → Events**.

## 4. The frontend pipeline

File: `.github/workflows/frontend.yml`

**Job: Type-check and build** (runs on every pull request and every merge to main)

| Step | What it does | If it fails, it usually means |
|---|---|---|
| Install packages | `npm ci`: installs exactly the versions in `package-lock.json` | `package.json` and `package-lock.json` don't match. Run `npm install` locally and commit the updated lock file |
| Scan naming and code quality | ESLint checks code quality and the naming rules in `frontend/eslint.config.js`. Warnings count as failures | A rule was broken. Run the scanner locally to see which ([section 5](#5-the-scanners)) |
| Type-check and build | `npm run build`: the TypeScript checker, then Vite | A type error, for example a wrong type passed to a function or a missing import |

**Job: Deploy to GitHub Pages** (only after a merge to main, only if the job above passed)

| Step | What it does | If it fails, it usually means |
|---|---|---|
| Build the live version | Builds with `VITE_API_URL` (from the repo variable `API_URL`) and `--base=/todo-app/` | The `API_URL` variable is missing, so the site would call the wrong address |
| Upload / Publish on GitHub Pages | Publishes `frontend/dist` | Pages isn't set to **Source: GitHub Actions** in repo Settings → Pages |

## 5. The scanners

| | Backend | Frontend |
|---|---|---|
| Tool | `dotnet format` | ESLint |
| Rules live in | `.editorconfig` (repo root) | `frontend/eslint.config.js` |
| Naming rules | Classes, methods, properties, constants: `PascalCase`. Interfaces: `IName`. Private fields: `_camelCase`. Test method names may use underscores (`Tick_FutureDay_Throws`) | Variables and functions: `camelCase`. Components and schemas: `PascalCase`. Fixed settings: `UPPER_CASE`. Types: `PascalCase` |
| Check it yourself | `dotnet format backend/TodoApp.slnx --verify-no-changes --severity warn` | `cd frontend` then `npm run lint -- --max-warnings 0` |
| Fix it | Run the same command **without** `--verify-no-changes`: formatting fixes itself. Names must be renamed by hand (Visual Studio: **Ctrl+R, Ctrl+R**) | Fix what it lists. Many problems fix themselves with `npx eslint . --fix` |

No output means clean.

**Always run both checks before pushing.** It's faster than waiting for the pipeline to fail.

## 6. Where things live

| Part | Where | Notes |
|---|---|---|
| Frontend | GitHub Pages | Free. Only the deploy job publishes to it |
| API | Render, free web service, Docker, Frankfurt | **Sleeps after 15 minutes without visitors.** The first request after that takes about a minute. **Auto-Deploy is off**: only the pipeline deploys |
| Database | Neon, free Postgres, Frankfurt | Sleeps after 5 minutes idle and wakes up by itself |

**Settings and secrets** (names only. Never put values in the repo or in chat):

| Name | Where | What it is |
|---|---|---|
| `DATABASE_CONNECTION_STRING` | GitHub → Settings → Secrets and variables → Actions → **Secrets** | Neon connection string, used by the migration step |
| `RENDER_DEPLOY_HOOK_URL` | GitHub → same place → **Secrets** | Render's secret "deploy now" URL |
| `API_URL` | GitHub → same place → **Variables** | The live API address, baked into the frontend build |
| `ConnectionStrings__TodoDb` | Render → todo-app → **Environment** | Neon connection string, used by the running API |
| `Cors__AllowedOrigins__0` | Render → todo-app → **Environment** | The website allowed to call the API: `https://muathali72.github.io` |

On your own computer, the connection string lives in **User Secrets** (`ConnectionStrings:TodoDb`). The `__` in the names above is how environment variables write `:`.

## 7. When something breaks

Real problems hit while setting this up. Each one: **what you see → what's really wrong → the fix.**

| What you see | What's really wrong | The fix |
|---|---|---|
| The live site says **CORS error** / "Failed to fetch" | Often the API is **crashing (500)**. A crash response has no CORS header, so the browser reports CORS instead | Open the API address directly (for example `/tasks`) to see the real status, then read **Render → Logs** for the exception line |
| A pull request is stuck on **"Expected — waiting for status"** | The workflow file is invalid (for example a step with a `name` but no `run` or `uses`), so the pipeline never started | Actions tab → the red run says **Invalid workflow file** and gives the line number. Fix it and push to the same branch: the PR re-runs by itself |
| Deploy job: **"Assets file 'project.assets.json' not found"** | Every job runs on its own fresh computer. The deploy job hadn't restored packages | Add a **Restore packages** step to that job |
| Render says **Deploy live**, but the API still behaves like an **older version** | Stale leftovers in Render's build cache | Render → **Manual Deploy → Clear build cache & deploy**. To check which side is wrong first, run main on your own computer: if it works there, the problem is on Render |
| The API crashes with **"The specified field '_days' cannot be used for the property 'Routine.days'"**, even though every check was green | The field was renamed to `_days` for the naming rules, but EF was told "property `days`, stored in field `_days`". EF requires a value stored only in a private field to use the field's **exact** name. The tests don't touch the database setup, so CI couldn't catch it | In `TodoDbContext`: `Property<List<DayOfWeek>>("_days")` with `HasColumnName("Days")`, so the database column keeps its name. Then add a migration (its `Up` is empty: only EF's snapshot changes). Lesson: **run the app locally before merging database changes** |

## 8. Common jobs

**Re-run a failed run.** Actions → open the run → **Re-run failed jobs**. Good for one-off download hiccups.

**Roll back the API.** Render → todo-app → **Events** → **Rollback** on an earlier deploy. This rolls back the **code only**. Database migrations are not undone.

**Roll back the frontend (or anything).** On the merged pull request, click **Revert**. That opens a new pull request that undoes it. Merge it like any other, and the pipelines redeploy.

**Add a database migration.**
1. Package Manager Console: `Add-Migration DescriptiveName`
2. **Read the new file's `Up(...)` before applying it.** `DropColumn` or `DropTable` means data loss, so stop and check.
3. `Update-Database` locally, then run the app and try the affected endpoints.
4. Open a pull request. After the merge, the pipeline applies it to Neon before deploying.

**Change a secret.** GitHub: Settings → Secrets and variables → Actions → click the secret → **Update**. Render: Environment → edit → save (Render restarts the API).

**Force a clean rebuild on Render.** Render → todo-app → **Manual Deploy → Clear build cache & deploy**.

## 9. Known limits

- **Free hosting sleeps.** The first visit after a quiet period takes about a minute. That's normal, not an error.
- **Deploy jobs only run after a merge.** A pull request can't test them, so check the Actions tab after every merge.
- **The tests don't touch the database.** A mistake in how classes map to tables passes CI and only crashes when the app runs. Run the app locally before merging database changes.
- **A CORS error can hide a crash.** When the API crashes, its error response has no CORS header, so the browser reports CORS. Open the API address directly to see the real status.
- **Rolling back on Render doesn't undo migrations.**
