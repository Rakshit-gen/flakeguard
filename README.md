# FlakeGuard

CI test suites accumulate tests that fail once every dozen runs for no reason
anyone can reproduce. Teams either ignore them (and stop trusting red builds)
or someone burns an afternoon re-running CI to prove it's "probably fine."
FlakeGuard tells you which failing tests are actually broken and which are
just flaky, then quarantines the flaky ones automatically so they stop
blocking merges while still getting tracked until they're fixed for real.

## How it decides

A test that fails every time is broken, not flaky — FlakeGuard never touches
those. A flakiness signal only exists when a test's outcome is inconsistent
(some passes, some failures) *and* it flips between them often. Two numbers
drive everything:

- **Failure rate** — how often the test fails at all
- **Flip rate** — how often consecutive runs disagree with each other

A test needs a non-trivial failure rate (not 0%, not 100%) and a high flip
rate to get flagged. Once quarantined, a test only returns to stable after a
run of consecutive clean passes — a couple of green runs right after
quarantine isn't enough, which stops a test from flapping in and out of
quarantine on every other build.

## Architecture

```
CI run → POST /api/ingest/{owner}/{repo}   (HMAC-signed, like a GitHub webhook)
              │
              ▼
     TestRunEvent + TestCaseResults + outbox row   (one Postgres transaction)
              │
              ▼ (Channel-based wake-up, polling fallback)
        OutboxWorker → FlakinessScorer → TestCase status updated
              │
              ▼ (on newly-quarantined tests)
     GitHub Commit Status API   (flakeguard/quarantine check on the commit)
```

The ingestion endpoint's only job is to durably record the run and return —
scoring, and the GitHub API call it can trigger, happen afterward on a
background worker reading from the outbox. If the worker is down or slow,
nothing is lost: the outbox table is the source of truth, and the in-memory
channel used to wake the worker immediately is just a latency shortcut.

## Stack

- **Backend**: ASP.NET Core 8 Minimal APIs, EF Core + Npgsql (PostgreSQL),
  `System.Threading.Channels`, HMAC-SHA256 request signing, Swashbuckle/OpenAPI
- **Frontend**: React 19 + TypeScript + Vite + Material UI
- **Testing**: xUnit against in-memory fakes for the scoring algorithm and
  request handling, plus `WebApplicationFactory` integration tests against a
  real Postgres service container in CI
- **CI/CD**: GitHub Actions, Render (Docker + managed Postgres), Vercel

## Project layout

```
backend/
  src/FlakeGuard.Core/     domain model, scoring algorithm, repository
                           interfaces, Postgres/EF Core implementations
  src/FlakeGuard.Api/      Minimal API endpoints, outbox worker, GitHub
                           status notifier, DI wiring
  tests/FlakeGuard.Tests/  unit tests (fakes) + Postgres integration tests
frontend/                  dashboard: repositories, tests sorted by flip
                            rate, pass/fail trace per test
```

## Local development

Backend needs the .NET 8 SDK and a Postgres connection string in
`ConnectionStrings:Postgres` (user secrets or an environment variable).

```
dotnet run --project backend/src/FlakeGuard.Api
```

Frontend:

```
cd frontend
npm install
npm run mock-api   # stand-in API with seed data, in one terminal
npm run dev        # in another
```
