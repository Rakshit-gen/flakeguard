# FlakeGuard dashboard

A minimal read-mostly dashboard over the FlakeGuard API: registered repositories,
their tracked tests sorted by flip rate, and a pass/fail trace per test.

## Scripts

- `npm run dev` — start the Vite dev server on `:5173`
- `npm run mock-api` — start a stand-in REST API on `:4000` with seed data covering
  all three test states (stable, suspicious, quarantined), for frontend work without
  a live backend
- `npm test` — run the Vitest suite
- `npm run build` — typecheck and build for production
- `npm run lint` — oxlint

## Configuration

`VITE_API_BASE_URL` (default `http://localhost:4000`) points the dashboard at a
FlakeGuard API instance.
