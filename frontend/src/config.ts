/**
 * Where the backend lives: always "/api" on the website's own address.
 *
 * @remarks
 * Live (Render) and Kubernetes: the API sits behind the same address under /api.
 * On your laptop: Vite's dev server passes /api calls to the API (see vite.config.ts).
 * VITE_API_URL can still override it; Vite bakes the value in at build time.
 */
export const API_URL: string = import.meta.env.VITE_API_URL ?? '/api'
