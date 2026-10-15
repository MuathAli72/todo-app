/**
 * Where the backend lives.
 *
 * @remarks
 * On your laptop nothing is set, so it's localhost.
 * The live build sets VITE_API_URL to the Render address (see .github/workflows/frontend.yml).
 * Vite bakes the value in when it builds, so it can't change after the build.
 */
export const API_URL: string = import.meta.env.VITE_API_URL ?? 'http://localhost:5149'
