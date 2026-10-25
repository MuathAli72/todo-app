# Deployment Playbook: To-Do App on Kubernetes

How the to-do app runs on Kubernetes, **why** it is built this way, and **how** to install,
update, roll back, test and fix it. Sections 1–4 are the why; 5–13 are the how.

All commands are PowerShell, run from the repo root (`C:\Projects\todo-app`).

---

## 1. What we deploy

```
                            ┌─► /api/...  ──► API (3 copies) ──► todo-db-rw ──► DB primary
browser ──► front door ─────┤                                                     │ syncs
            (2 copies)      └─► everything else ──► frontend (2 copies)        DB backup
```

| Piece      | What it does                                                        | Remembers data? |
|------------|---------------------------------------------------------------------|-----------------|
| Front door | The one address users visit. Sends `/api/...` to the API (with `/api` removed) and everything else to the frontend. | No |
| Frontend   | Hands the website files to browsers (nginx). The app itself runs in the user's browser. | No |
| API        | Handles requests: runs the app's rules and reads/writes the database. | No |
| Database   | Stores tasks and routines (PostgreSQL).                              | **Yes**         |
| Migrations | Changes the shape of the database tables when new code needs it. Runs once per deploy, then stops. | No |

Where the files live:

| Path | What |
|---|---|
| `deploy/charts/todo-db/` | Helm chart for the database |
| `deploy/charts/todo-app/` | Helm chart for the front door, frontend, API and migrations |
| `backend/Dockerfile` | Builds the API image (default) and the migrations image (`--target migrate`) |
| `frontend/Dockerfile`, `frontend/nginx.conf` | Builds the frontend image |
| `.github/workflows/*.yml` | CI/CD: tests, then publishes images to GHCR on every merge to main |

**To change how the app runs, edit a chart's `values.yaml`.** You should never need to touch `templates/`.

## 2. Why high availability?

To keep the app working at all times, for as many users as possible, even when one
part of it crashes, a machine goes down, or a new version is being rolled out.

## 3. Design decisions: why before how

### How many copies

| Piece      | Count                | Why |
|------------|----------------------|-----|
| Front door | 2                    | It is the only way in. If it is down, nothing is reachable, even if everything behind it is healthy. |
| Frontend   | 2                    | Very light work. The second copy is there in case one fails. |
| API        | 3                    | Every click lands here, so it gets the most traffic. With 3, two keep serving while one is being updated. |
| Database   | 1 primary + 1 backup | Only the primary accepts writes. If it goes down, the backup is promoted to primary automatically, and the old primary returns as the new backup. |
| Migrations | 1, once per deploy   | It runs once and stops. Two running at the same time could both try to make the same change and clash. |

### Rules every piece follows

- **Copies are spread across nodes**, so that if one node dies, the copies on the other node keep serving.
- **Health checks:** the "is it frozen?" check (liveness) only tests the API itself, never the
  database. Restarting the API cannot fix a database problem; it would only put every API copy
  into a restart loop and keep the API down longer than the database was.
- **Resources:** every copy reserves a minimum (request) and has a memory ceiling (limit), so one
  misbehaving copy cannot starve everything else on its node.
- **Deploy order:** migrations run **before** the new API starts, because the new API fails if the
  tables it expects do not exist yet. Helm does this with a *pre-install/pre-upgrade hook*.
- **Migrations only add, never delete or rename, in the same deploy.** During an update the old API
  copies are still running; if a column they use disappears, they break. Removing old columns
  happens in a later deploy. (This also makes rolling back the app safe: old code still works with
  the newer tables.)

### Tool choices

| Choice | Why | Alternatives considered |
|---|---|---|
| **kind with 3 nodes** (1 control-plane + 2 workers) inside Docker Desktop | High availability needs more than one node; a single node cannot survive its own failure. | kubeadm (single node only) |
| **CloudNativePG** operator for the database | Actively maintained (CNCF project) and handles primary/backup sync and automatic failover. | Bitnami `postgresql-ha` (free images no longer receive security updates since 2025); doing failover by hand (too risky) |
| **Envoy Gateway** for the front door, using the **Gateway API** | The Gateway API is the current Kubernetes standard for routing (the popular ingress-nginx was retired in March 2026). Envoy Gateway is a CNCF project, so no single company controls it. | Traefik, NGINX Gateway Fabric. The routing rules (Gateway, HTTPRoute) are the same for any of them. |
| **All API calls under `/api`** | One routing rule forever. New endpoints just work, with no front door change. | Listing every API path in the front door |
| **Two charts** (`todo-db`, `todo-app`) | The database lives for years; the app changes every week. Separate charts mean upgrading the app never touches the database. It also fixes the hook order: the migrations hook needs the database to exist already. | Database outside Helm; everything in one chart |
| **Migrations as a separate image and Job** | Runs exactly once, before the API, and keeps migrations separate from the API. | The API migrating itself at startup |
| **Images tagged with the commit ID and `latest`** | Every running copy points to exact code; rolling back means pointing at an older commit ID. | `latest` only |

## 4. Honest limits

- This runs on **one PC** with 3 simulated nodes (kind, inside Docker Desktop). It survives a copy
  crashing or a node going away, but if the PC or Docker Desktop stops, everything stops. A real
  company runs the same charts on separate physical machines.
- **Database failover takes seconds, not zero.** During that time writes fail; then the backup takes over
  automatically. A clean shutdown switches in about 3 seconds; a real crash takes longer (tens of
  seconds), because the operator must first be sure the primary is really dead.
- **`kubectl port-forward` is a test tool** that connects to one single pod. If that pod moves, the
  tunnel dies even though the app is fine. In production, a load balancer sits in front of both
  front-door copies instead.

---

## 5. One-time setup (a new machine)

1. **Docker Desktop** (WSL 2). If the installer says "Virtual Machine Platform is not enabled":
   run `wsl --install` in an *administrator* PowerShell, restart, and run the installer again.
2. **Kubernetes:** Docker Desktop → Kubernetes → Create cluster → type **kind**, **3 nodes**.
   Check: `kubectl get nodes` shows 3 nodes, all `Ready`.
3. **Helm:** `winget install Helm.Helm`, then reopen the terminal. Check: `helm version`.
4. **The database operator** (once per cluster):
   ```
   helm repo add cnpg https://cloudnative-pg.github.io/charts
   helm upgrade --install cnpg --namespace cnpg-system --create-namespace cnpg/cloudnative-pg
   ```
   Check: `kubectl get pods -n cnpg-system` shows 1 pod `Running`.
5. **Envoy Gateway** (once per cluster):
   ```
   helm install eg oci://docker.io/envoyproxy/gateway-helm --version v1.8.5 -n envoy-gateway-system --create-namespace
   kubectl wait --timeout=5m -n envoy-gateway-system deployment/envoy-gateway --for=condition=Available
   ```
6. **Images:** the cluster downloads them from GHCR (`ghcr.io/muathali72/todo-api`, `todo-web`,
   `todo-migrate`). They must be **public** (GitHub profile → Packages → package → Package settings
   → Change visibility). Images built only on your PC are *not* visible to kind's nodes.

## 6. Install

```
helm install todo-db deploy\charts\todo-db
kubectl get cluster todo-db          # wait for "Cluster in healthy state"
helm install todo-app deploy\charts\todo-app
```
The second command pauses while the migrations run (the hook). Then check:
```
helm list                             # todo-db and todo-app, both "deployed"
kubectl get pods -o wide              # 2 todo-db, 3 todo-api, 2 todo-web Running; todo-migrate Completed
```

## 7. Open the app

```
$svc = kubectl get svc -n envoy-gateway-system --selector=gateway.envoyproxy.io/owning-gateway-name=todo-gateway -o jsonpath='{.items[0].metadata.name}'
kubectl -n envoy-gateway-system port-forward service/$svc 8888:80
```
Then open http://localhost:8888. Ctrl+C stops it.

## 8. Ship a new version

1. Merge a PR into `main`. CI tests the code, then publishes images tagged with the **full commit ID**
   (also shown on the Actions run). Only the side that changed is rebuilt.
2. Deploy that exact code:
   ```
   helm upgrade todo-app deploy\charts\todo-app --reuse-values --set api.tag=<commit ID>
   ```
   Set the tag only for the side that changed (`api.tag` and/or `web.tag`). `--reuse-values` keeps
   every other setting as it was. Migrations run first, then the copies are replaced one at a time
   with no downtime.
3. Check: `kubectl get pods -o wide` (all `Running`, `1/1`), then open the app.

## 9. Roll back

```
helm history todo-app          # numbered list of every install/upgrade
helm rollback todo-app <number>
```
This puts back the previous settings, including the old image tags. The database tables are **not**
rolled back, which is safe because migrations only ever add (see section 3).

## 10. Check health

| Question | Command | Healthy answer |
|---|---|---|
| Are the releases installed? | `helm list` | both `deployed` |
| Are all copies running and spread? | `kubectl get pods -o wide` | all `Running 1/1`, on both workers |
| Is the database healthy? Who is primary? | `kubectl get cluster todo-db` | `Cluster in healthy state`, PRIMARY shown |
| Is the front door up? | `kubectl get gateway todo-gateway` | `PROGRAMMED True` |
| Did migrations work? | `kubectl logs job/todo-migrate` | ends with `Done.` |

## 11. HA drills (prove it survives failures)

**Watcher** (shows the app answering every half second). First, a tunnel that reconnects by itself:
```
$svc = kubectl get svc -n envoy-gateway-system --selector=gateway.envoyproxy.io/owning-gateway-name=todo-gateway -o jsonpath='{.items[0].metadata.name}'
while ($true) { kubectl -n envoy-gateway-system port-forward service/$svc 8888:80; Start-Sleep 1 }
```
In a second terminal:
```
while ($true) { try { Invoke-WebRequest http://localhost:8888/api/tasks -UseBasicParsing -TimeoutSec 2 | Out-Null; Write-Host "$(Get-Date -f HH:mm:ss) OK" -ForegroundColor Green } catch { Write-Host "$(Get-Date -f HH:mm:ss) FAIL" -ForegroundColor Red }; Start-Sleep -Milliseconds 500 }
```

| Drill | Command | Expected |
|---|---|---|
| Kill an API copy | `$p = kubectl get pods -l app=todo-api -o jsonpath='{.items[0].metadata.name}'; kubectl delete pod $p` | All OK; Kubernetes starts a replacement |
| Kill the DB primary | `$primary = kubectl get cluster todo-db -o jsonpath='{.status.currentPrimary}'; kubectl delete pod $primary` | A few seconds of FAIL, then OK; the backup is now primary |
| Take a node away | see below | Mostly OK (the tunnel reconnects if its pod moved) |

Take a node away: drain the worker that does **not** hold the DB primary (the operator protects the primary from being moved by a drain):
```
$primary = kubectl get cluster todo-db -o jsonpath='{.status.currentPrimary}'
$pnode = kubectl get pod $primary -o jsonpath='{.spec.nodeName}'
$node = if ($pnode -eq 'desktop-worker') { 'desktop-worker2' } else { 'desktop-worker' }
kubectl drain $node --ignore-daemonsets --delete-emptydir-data
kubectl uncordon $node     # put it back afterwards
```
Kubernetes does not move copies back after `uncordon`. To spread them again: `kubectl rollout restart deployment todo-api todo-web`.

## 12. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Pod stuck in `ErrImagePull` / `ImagePullBackOff` | Package is private, or that tag does not exist (e.g. a frontend tag for a backend-only commit) | Make the package public; check the tag exists on GHCR |
| `ErrImageNeverPull` | Image was only built on your PC | Use the GHCR image; kind's nodes cannot see local images |
| `helm install/upgrade todo-app` fails or hangs | The migrations hook failed | `kubectl logs job/todo-migrate`. Fix the migration and run the upgrade again; the old version keeps running meanwhile |
| API pods `CrashLoopBackOff` | Wrong DB settings, or the database is down | `kubectl logs <pod>`; `kubectl get cluster todo-db` |
| Pods stay `Pending` | Not enough room on the nodes, or a node is drained | `kubectl describe pod <pod>` (read Events); `kubectl uncordon <node>` |
| Watcher red forever after a drain | The `port-forward` tunnel's pod moved | Restart the tunnel (or use the reconnecting loop in section 11) |
| Site loads but tasks fail | Front door not routing `/api` | `kubectl get gateway todo-gateway` and `kubectl get httproute todo-routes` |
| Harmless log lines | `Cannot load library libgssapi_krb5.so.2`; `Failed executing ... __EFMigrationsHistory` on a new database; EF warning 10620 | No action needed |
| `helm install` says a resource already exists | It was created with `kubectl apply`, not Helm | Delete it with `kubectl delete`, then install with Helm |

## 13. Remove everything

```
helm uninstall todo-app
helm uninstall todo-db      # ⚠️ deletes the database and its data
```

---

## Notifications

GitHub emails the person who triggered a pipeline run, on success and failure
(GitHub → Settings → Notifications → Actions → untick "Only notify for failed workflows").
A Slack notification step is planned for team visibility.
