import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { initializeFaro, getWebInstrumentations } from '@grafana/faro-web-sdk'
import { TracingInstrumentation } from '@grafana/faro-web-tracing'
import App from './App.tsx'

// Frontend telemetry (Grafana Faro): errors, page speed, sessions, console logs and traces.
// Only on the live site: on your laptop (npm run dev) and in the Kubernetes setup it stays off.
// It starts before React, so it also catches errors that happen during startup.
if (window.location.hostname === 'todo-app-yaw8.onrender.com') {
  initializeFaro({
    // Our own public collector (todo-faro-collector), not Grafana directly.
    url: 'https://todo-faro-collector.onrender.com/collect',
    app: {
      name: 'todo-web',
      environment: 'production',
    },
    instrumentations: [
      // The standard set: errors, Web Vitals (page speed), sessions, page views, console logs.
      ...getWebInstrumentations(),
      // Traces fetch calls and adds the traceparent header, linking each click to the API's trace.
      new TracingInstrumentation(),
    ],
  })
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)