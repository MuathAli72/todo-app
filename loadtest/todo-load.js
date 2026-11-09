// Load test for the live To-Do app: simulates normal users with a realistic mix of actions.
// Run (PowerShell):
//   $env:TEST_PASSWORD = Read-Host "Test password"
//   k6 run "C:\Projects\todo-app\loadtest\todo-load.js"
// The password is never written in this file. All 5 test accounts share it.
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.BASE_URL || 'https://todo-app-yaw8.onrender.com/api';
const PASSWORD = __ENV.TEST_PASSWORD;

// example.com is reserved for testing, so these addresses can never belong to a real person.
const ACCOUNTS = [1, 2, 3, 4, 5].map((n) => `loadtest${n}@example.com`);

// Errors scenario (later): when FAULT_KEY and FAULT_RATE are set, that share of requests carries
// the secret header, and the API answers them with a 500. Without them, nothing changes.
const FAULT_KEY = __ENV.FAULT_KEY;
const FAULT_RATE = Number(__ENV.FAULT_RATE || 0);

export const options = {
  // k6 normally empties each virtual user's cookies after every loop round (as if a brand-new visitor
  // arrived each time). Our users log in once and stay logged in, so the login cookie must be kept.
  noCookiesReset: true,
  // Normal traffic, then busy traffic, then quiet again. About 10.5 minutes in total.
  stages: [
    { duration: '1m', target: 5 },
    { duration: '5m', target: 5 },
    { duration: '30s', target: 10 },
    { duration: '3m', target: 10 },
    { duration: '1m', target: 0 },
  ],
  // Pass/fail rules for the whole run, like a mini SLO. Times here are the client's clock
  // (network + Cloudflare + Render + API), so they're bigger than the API's own p95.
  // Only the "load" phase counts: setup() wakes the app (up to ~50 s) and gets an expected
  // 400 "already exists" for each account, which would unfairly fail both rules.
  thresholds: {
    'http_req_failed{phase:load}': ['rate<0.01'],
    'http_req_duration{phase:load}': ['p(95)<1000'],
  },
};

const ALL_DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

// Every request goes through here: JSON headers, the UTC time zone for "today",
// an optional fault header, and a name so k6's report groups requests by endpoint.
function request(method, path, body, name, timeout = '60s', phase = 'load') {
  const headers = { 'Content-Type': 'application/json', 'X-Time-Zone': 'UTC' };
  if (FAULT_KEY && phase === 'load' && Math.random() < FAULT_RATE) {
    headers['X-Fault-Key'] = FAULT_KEY;
  }
  const payload = body === null ? null : JSON.stringify(body);
  return http.request(method, `${BASE}${path}`, payload, { headers, timeout, tags: { name, phase } });
}

// Checks for a 2xx answer. On failure it prints the status and the start of the answer
// (never the request, so the password can't end up in the output).
function ok(res, what) {
  const passed = check(res, { [`${what}: 2xx`]: (r) => r.status >= 200 && r.status < 300 });
  if (!passed) {
    console.warn(`${what} failed: ${res.status} ${String(res.body).slice(0, 150)}`);
  }
  return passed;
}

function login(email, timeout, phase = 'load') {
  const res = request('POST', '/account/login', { email, password: PASSWORD }, 'POST /account/login', timeout, phase);
  ok(res, 'login');
}

// Runs once before the test: makes sure each account exists and has 2 routines (scheduled every
// day, so ticking "today" is always allowed). Safe to run again: existing accounts just log in.
export function setup() {
  if (!PASSWORD) {
    throw new Error('Set the TEST_PASSWORD environment variable first.');
  }
  for (const email of ACCOUNTS) {
    // The first request may wake the app up on Render, so allow 2 minutes.
    const reg = request('POST', '/account/register', { email, password: PASSWORD }, 'POST /account/register', '120s', 'setup');
    if (reg.status !== 201) {
      // 400 "already exists" is expected from the second run on.
      login(email, '120s', 'setup');
    }
    const routines = request('GET', '/routines', null, 'GET /routines', '60s', 'setup').json();
    for (let i = routines.length; i < 2; i++) {
      const created = request('POST', '/routines', { title: `Load test routine ${i + 1}`, days: ALL_DAYS, time: null }, 'POST /routines', '60s', 'setup');
      ok(created, 'create routine');
    }
  }
}

// Each virtual user logs in once, as one of the 5 accounts, and keeps its login cookie.
let loggedIn = false;

function viewRoutines() {
  ok(request('GET', '/routines', null, 'GET /routines'), 'view routines');
}

function viewTasks() {
  ok(request('GET', '/tasks', null, 'GET /tasks'), 'view tasks');
}

// Ticks a routine for today, or unticks it if it's already ticked, so every run really writes.
function tickRoutine() {
  const res = request('GET', '/routines', null, 'GET /routines');
  if (!ok(res, 'view routines')) return;
  const routines = res.json();
  if (routines.length === 0) return;
  const routine = routines[Math.floor(Math.random() * routines.length)];
  const today = new Date().toISOString().slice(0, 10);
  if (routine.isDoneToday) {
    ok(request('DELETE', `/routines/${routine.id}/completions/${today}`, null, 'DELETE /routines/{id}/completions/{date}'), 'untick');
  } else {
    ok(request('POST', `/routines/${routine.id}/completions`, { date: today }, 'POST /routines/{id}/completions'), 'tick');
  }
}

// Adds a task while there are few; otherwise renames or deletes one, so the list stays small.
function changeTask() {
  const res = request('GET', '/tasks', null, 'GET /tasks');
  if (!ok(res, 'view tasks')) return;
  const tasks = res.json();
  if (tasks.length < 8) {
    ok(request('POST', '/tasks', { title: `Load test task ${Date.now() % 100000}` }, 'POST /tasks'), 'add task');
    return;
  }
  const task = tasks[Math.floor(Math.random() * tasks.length)];
  if (Math.random() < 0.5) {
    ok(request('PATCH', `/tasks/${task.id}/title`, { title: `Renamed ${Date.now() % 100000}` }, 'PATCH /tasks/{id}/title'), 'rename task');
  } else {
    ok(request('DELETE', `/tasks/${task.id}`, null, 'DELETE /tasks/{id}'), 'delete task');
  }
}

export default function () {
  if (!loggedIn) {
    login(ACCOUNTS[(__VU - 1) % ACCOUNTS.length], '120s');
    loggedIn = true;
  }

  // The agreed mix: 40% view routines, 20% view tasks, 20% tick, 20% change a task.
  const roll = Math.random();
  if (roll < 0.4) viewRoutines();
  else if (roll < 0.6) viewTasks();
  else if (roll < 0.8) tickRoutine();
  else changeTask();

  // Think time: a real person pauses between clicks.
  sleep(1 + Math.random() * 4);
}
