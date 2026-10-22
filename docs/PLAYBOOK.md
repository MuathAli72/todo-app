# Deployment Playbook: To-Do App on Kubernetes

> Draft. Sections 1–4 explain **why** the deployment looks the way it does.
> The **how** (install, update, roll back, troubleshoot) is added as we build it.

## 1. What we deploy

| Piece      | What it does                                                        | Remembers data? |
|------------|---------------------------------------------------------------------|-----------------|
| Front door | The one address users visit. Sends API requests to the API and everything else to the frontend. | No |
| Frontend   | Hands the website files to browsers. The app itself runs in the user's browser. | No |
| API        | Handles requests from users: runs the app's rules and reads/writes the database. | No |
| Database   | Stores tasks and routines (PostgreSQL).                             | **Yes**         |
| Migrations | Changes the shape of the database tables (columns, tables) when new code needs it. | No |

## 2. Why high availability?

To keep the app working at all times, for as many users as possible, even when one
part of it crashes, a machine goes down, or a new version is being rolled out.

## 3. Design decisions: why before how

| Piece      | Count                | Why |
|------------|----------------------|-----|
| Front door | 2                    | It is the only way in. If it is down, nothing is reachable, even if everything behind it is healthy. |
| Frontend   | 2                    | Very light work. The second copy is there in case one fails. |
| API        | 3                    | Every click lands here, so it gets the most traffic. With 3, two keep serving while one is being updated. |
| Database   | 1 primary + 1 backup | Only the primary accepts writes. If it goes down, the backup is promoted to primary automatically, and the old primary returns as the new backup. |
| Migrations | 1, once per deploy   | It runs once and stops. Two running at the same time could both try to make the same change and clash. |

**Copies are spread across nodes** so that if one node dies, the copies on the other
node keep serving.

**Health checks:** the "is it frozen?" check (liveness) only tests the API itself, never
the database. Restarting the API cannot fix a database problem. It would only put every
API copy into a restart loop and keep the API down longer than the database was.

**Deploy order:** migrations run **before** the new API starts, because the new API
fails if the tables it expects do not exist yet.

**Migrations only add, never delete or rename, in the same deploy.** During an update the
old API copies are still running. If a column they use disappears, they break. Removing
old columns happens in a later deploy, once no running code uses them.

## 4. Honest limits

This runs on one PC with 3 simulated nodes (kind, inside Docker Desktop). It survives a
copy crashing or one node going down, but if the PC itself or Docker Desktop stops,
everything stops. A real company runs the same setup on separate physical machines.
