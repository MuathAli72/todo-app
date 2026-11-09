# Deployment Playbook: To-Do App on Kubernetes

How the to-do app runs on Kubernetes, **why** it is built this way, and **how** to install,
update, roll back, test and fix it. Sections 1–4 are the why; 5–13 are the how.
Section 14 covers the audit-trail stack (Elasticsearch, Kibana, Logstash); section 15 covers
monitoring, alerts and load tests on the live app.

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
| `deploy/elastic/` | Audit-trail stack: operator settings, Elasticsearch, Kibana, Logstash (section 14) |
| `deploy/grafana/` | The on-call dashboard as JSON (section 15) |
| `loadtest/` | k6 load test for the live app (section 15) |
| `scripts/` | Helper scripts, e.g. keeping the collector awake during tests (section 15) |
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
- **Database failover is automatic, but not instant.** Measured with the watcher while the API was
  connected: a simulated crash of the primary (force delete) caused **about 20 seconds of errors**,
  then the backup took over as primary.
- **A normal `kubectl delete pod` on the primary is slow on purpose.** The primary shuts down
  politely (waits for connections and data syncing; up to 30 minutes by default), and the
  CloudNativePG docs warn against deleting the primary pod. For planned moves, use a switchover
  (`kubectl cnpg promote`, needs the cnpg kubectl plugin) instead.
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
| Crash the DB primary | `$primary = kubectl get cluster todo-db -o jsonpath='{.status.currentPrimary}'; kubectl delete pod $primary --grace-period=0 --force` | About 20 seconds of FAIL, then OK; the backup is now primary (test data only: a forced crash can lose the last few writes) |
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

## 14. Audit-trail stack (Elasticsearch, Kibana, Logstash)

The live app writes every audit event (data changes and account/security events) to the
`AuditEvents` table in Neon **first**. Logstash copies new rows into Elasticsearch once a minute,
and Kibana shows them. Everything here runs in its own namespace, `elastic`.

```
Neon (AuditEvents) ──► Logstash (once a minute, read-only login) ──► Elasticsearch ──► Kibana
```

### Why it is built this way

| Choice | Why |
|---|---|
| **Database first, copy later** | The table is the complete record. Elasticsearch can be wiped and refilled from it, and nothing is lost while this PC is off: Logstash continues from where it stopped. |
| **ECK operator** | Same pattern as CloudNativePG: we describe what we want, and the operator creates it and looks after it (certificates, passwords, and the logins between the pieces). |
| **Logstash with a SQL query (JDBC)** | The classic ELK path. It remembers the last copied `Id` (`:sql_last_value`) on its own disk, so each run only asks for newer rows. |
| **Row `Id` = document id in Elasticsearch** | Copying a row twice overwrites it instead of making a duplicate, so copying again is always safe. |
| **Read-only `audit_reader` login** | Logstash can only read `AuditEvents`, nothing else. A leaked Logstash password cannot change data or read other tables. |
| **Images from Docker Hub** (`docker.io/elastic/...`) | Downloads from `docker.elastic.co` get dropped on this network. Same images, different address. |

### Honest limits

- **One Elasticsearch node.** Cluster health can show **yellow**: each index wants a backup copy
  (replica) on a second node, and there isn't one. Fine for this setup.
- **Copies only while this PC and the cluster run.** Nothing is lost when they're off, but Kibana
  is behind until Logstash catches up.
- **Heavy on RAM:** Elasticsearch 2Gi + Kibana 1Gi + Logstash 1Gi. Pause the app to make room (below).

### Files

| File | What |
|---|---|
| `deploy/elastic/eck-operator-values.yaml` | Our settings for the ECK operator's Helm install |
| `deploy/elastic/elasticsearch.yaml` | Elasticsearch `audit`: 1 node, 2Gi |
| `deploy/elastic/kibana.yaml` | Kibana `audit`: connects to Elasticsearch `audit`, usage reports to Elastic turned off |
| `deploy/elastic/logstash.yaml` | Logstash `audit`: the SQL query, the copy schedule, the Postgres driver download |

### Install (once per cluster)

1. **The ECK operator:**
   ```
   helm repo add elastic https://helm.elastic.co
   helm upgrade --install elastic-operator elastic/eck-operator --version 3.5.0 -n elastic-system --create-namespace -f deploy\elastic\eck-operator-values.yaml
   ```
   Check: `kubectl get pods -n elastic-system` shows 1 pod `Running`.
2. **The namespace and the Neon login** (the values in `<...>` are placeholders; the real ones
   come from Neon and **never go into git**):
   ```
   kubectl create namespace elastic
   kubectl create secret generic neon-audit-reader -n elastic --from-literal=NEON_JDBC_URL="jdbc:postgresql://<neon host>/neondb?sslmode=require" --from-literal=NEON_USER="audit_reader" --from-literal=NEON_PASSWORD="<password>"
   ```
   The `audit_reader` role was created in Neon with SQL and may only `SELECT` on `AuditEvents`.
3. **The three pieces:**
   ```
   kubectl apply -f deploy\elastic\elasticsearch.yaml
   kubectl apply -f deploy\elastic\kibana.yaml
   kubectl apply -f deploy\elastic\logstash.yaml
   ```
   Check: `kubectl get pods -n elastic` shows `audit-es-default-0`, an `audit-kb-...` pod and
   `audit-ls-0`, all `Running`.
4. **In Kibana** (open it as below): Data Views → Create data view → Name `Audit events`,
   Index pattern `logstash-audit`, Timestamp field `@timestamp` → Save data view to Kibana.

### Open Kibana

In a second terminal (it stays busy while the tunnel is open):
```
kubectl port-forward service/audit-kb-http 5601 -n elastic
```
Copy the `elastic` user's password to the clipboard (it is **not** printed):
```
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String((kubectl get secret audit-es-elastic-user -n elastic -o jsonpath="{.data.elastic}"))) | Set-Clipboard
```
Open https://localhost:5601 and log in as `elastic`. The browser warns about the certificate
because the operator made its own: Advanced → Proceed to localhost.
In Discover, set the time picker wider than the default **Last 15 minutes** to see older events.

### Make room (RAM)

Check what's free: `wsl -d docker-desktop -- free -m` → look at **available**.
Pause the app's copies (the database keeps running):
```
kubectl scale deployment todo-api -n default --replicas=0
kubectl scale deployment todo-web -n default --replicas=0
```
Unpause:
```
kubectl scale deployment todo-api -n default --replicas=3
kubectl scale deployment todo-web -n default --replicas=2
```

### Check health

| Question | Command | Healthy answer |
|---|---|---|
| Are the pieces up? | `kubectl get elasticsearch,kibana,logstash -n elastic` | Elasticsearch `green` or `yellow` (see limits), Kibana `green` |
| Is Logstash copying? | `kubectl logs -n elastic audit-ls-0 --tail=10` | A `SELECT "Id", ... WHERE "Id" > <number>` line every minute, no errors |
| Does the cluster have my latest change? | `kubectl get logstash audit -n elastic -o yaml \| Select-String "statement"` | Shows the query that's in your file |

### Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Elastic image downloads fail or hang | `docker.elastic.co` is blocked on this network | Use `docker.io/elastic/<name>:<version>` |
| Logstash log: `MissingConverterException ... PGobject` | The query returns a Postgres-only type (like `jsonb`) | Turn it into text in the query: `"Changes"::text AS "Changes"` |
| Logstash still runs the old query after you edited the file | The change never reached the cluster | `kubectl apply -f deploy\elastic\logstash.yaml`, check it says `configured`, then check with the `Select-String` command above |
| Logstash log: `403` / `security_exception` | The operator's Logstash login may not write to that index name | Check the index name in `logstash.yaml` |
| Discover shows nothing | Time picker is on Last 15 minutes, or no data view yet | Widen the time range; create the data view (Install step 4) |
| Certificate warning on https://localhost:5601 | The operator's own certificate | Expected; continue to localhost |

### Remove

```
kubectl delete -f deploy\elastic\logstash.yaml
kubectl delete -f deploy\elastic\kibana.yaml
kubectl delete -f deploy\elastic\elasticsearch.yaml   # ⚠️ deletes the copies in Elasticsearch (Neon keeps the originals)
```

## 15. Monitoring, alerts and load tests (live app on Render)

The full story (findings, numbers, decisions) is in the report doc: **To-Do App: SRE Report**
(https://claude.ai/artifact/666HHfw7twi5vPsSZDaT1h). This section is only **how to run it**.

### Files and settings

| Where | What |
|---|---|
| `deploy/grafana/todo-on-call-dashboard.json` | The Grafana dashboard `To-Do App – On-call` (8 rows, 14 panels) |
| `loadtest/todo-load.js` | k6 load test: 5 test users → 10, realistic mix of actions, pass/fail thresholds |
| `scripts/keep-collector-awake.ps1` | Pings the telemetry collector every 10 min so it doesn't sleep during a test |
| `backend/TodoApp.Api/Program.cs` | Fault injection: a request with the secret `X-Fault-Key` header gets a 500 (off unless configured) |
| Render `todo-app` → Environment | `OTEL_EXPORTER_OTLP_TIMEOUT=90000` (keep it: covers the collector's start-up). `FAULT_INJECTION_KEY` only during an error test, then delete it |
| Grafana → Alert rules → folder `To-Do App` | 5 alerts: server errors, slow API, lockout, memory, telemetry missing (paused outside tests) |

### Why the collector needs help

Free Render services sleep after 15 idle minutes. The API's own exports **cannot wake** the
collector, so while it sleeps the API's telemetry is lost. Requests from outside Render (a
browser, PowerShell) do wake it. So: start the keep-awake script before any test session.

### Before a test session

1. Terminal 1 (leave it running), and wait for `collector: 200`:
   ```
   powershell -ExecutionPolicy Bypass -File "C:\Projects\todo-app\scripts\keep-collector-awake.ps1"
   ```
2. Grafana → Alert rules → **resume** `API telemetry missing (test sessions only)`.
3. Open the dashboard on **Last 15 minutes**.

### Normal traffic run (~10.5 min)

Terminal 2. The test accounts' password (8+ characters) is typed, never stored:
```
$env:TEST_PASSWORD = Read-Host "Test password"
$env:TEST_PASSWORD.Length                        # must print 8 or more
k6 run --vus 1 --duration 30s "C:\Projects\todo-app\loadtest\todo-load.js"   # smoke test first
$env:K6_WEB_DASHBOARD = "true"
$env:K6_WEB_DASHBOARD_EXPORT = "$env:USERPROFILE\Documents\k6-normal-traffic.html"
k6 run "C:\Projects\todo-app\loadtest\todo-load.js"
```
Run `Read-Host` on its own line and type the password at the prompt: anything pasted while it
waits becomes the "password". Live charts: http://localhost:5665 (only while k6 runs). Expected:
both thresholds ✓, no alert fires.

### Lockout scenario (proves the lockout alert)

```
1..6 | ForEach-Object { $code = try { (Invoke-WebRequest "https://todo-app-yaw8.onrender.com/api/account/login" -Method Post -ContentType "application/json" -Body '{"email":"loadtest5@example.com","password":"wrong-password-on-purpose"}' -UseBasicParsing -TimeoutSec 90).StatusCode } catch { [int]$_.Exception.Response.StatusCode }; Write-Host "$(Get-Date -f HH:mm:ss) attempt $_ -> $code" }
```
Expected: some `401`, then `429`. `Account lockout (429)` fires within ~2 min. The account
unlocks itself after 5 min.

### Error scenario (proves the server-errors alert)

1. Terminal 2 — makes a key, keeps it in the terminal **and** copies it, in one command:
   ```
   $env:FAULT_KEY = [guid]::NewGuid().ToString("N"); $env:FAULT_KEY | Set-Clipboard
   ```
2. **Right away** (copying anything else replaces the clipboard): Render → `todo-app` →
   Environment → `FAULT_INJECTION_KEY` = paste → Save and deploy → wait for **Deploy live**.
3. Check it works (must print `500`):
   ```
   try { (Invoke-WebRequest "https://todo-app-yaw8.onrender.com/api/health" -Headers @{ "X-Fault-Key" = $env:FAULT_KEY } -UseBasicParsing -TimeoutSec 90).StatusCode } catch { [int]$_.Exception.Response.StatusCode }
   ```
4. Run with 20% injected failures:
   ```
   $env:FAULT_RATE = "0.2"
   k6 run --vus 5 --duration 8m "C:\Projects\todo-app\loadtest\todo-load.js"
   ```
   Expected: errors panel ~20%, `(500)` lines on real endpoints, `API server errors > 5%`
   firing ~6–7 min in. k6's thresholds fail — that's the point.
5. **Switch it off:** delete `FAULT_INJECTION_KEY` on Render (Save and deploy), then
   `Remove-Item Env:FAULT_KEY, Env:FAULT_RATE`.

### After a test session

Stop the keep-awake script (Ctrl+C) and **pause** `API telemetry missing` again.

### Import the dashboard into another Grafana

Grafana → Dashboards → New → Import → upload `deploy/grafana/todo-on-call-dashboard.json` →
pick the Prometheus and traces data sources when asked.

### Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Traces show only `todo-web` (1 service) | The collector was asleep | Start the keep-awake script; check its Render logs for a start-up |
| Collector's Render logs empty for days | It never woke up (nothing outside Render called it) | Same as above |
| k6: every request `401 "Please log in first."` after the first round | k6 clears cookies each round | `noCookiesReset: true` is in the script; keep it |
| k6: login fails, register returns 400 | Password under 8 characters, or `TEST_PASSWORD` not set | Check `$env:TEST_PASSWORD.Length` |
| Error run shows no 500s | Terminal key ≠ Render key (often a replaced clipboard), or the deploy isn't live | Step 3's check must print 500 before running |
| A panel shows 0 or nothing at low traffic | `rate()` needs the counter to move inside one awake process | Judge panels under test traffic; gaps = app asleep, not zero |
| Legend shows `Value` | `{{name}}` refers to a label that doesn't exist | Type a plain name without braces |
| Exemplar has no "open trace" button | Grafana Cloud's locked data source expects `traceID`, the data has `trace_id` | Copy the `trace_id` into Explore → traces → TraceQL |

---

## Notifications

GitHub emails the person who triggered a pipeline run, on success and failure
(GitHub → Settings → Notifications → Actions → untick "Only notify for failed workflows").
A Slack notification step is planned for team visibility.
