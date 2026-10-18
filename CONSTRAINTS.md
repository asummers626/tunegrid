# TuneGrid — Quality bar

## Why this exists

The riskiest code in this app is the parsers: `M3uPlaylistParser`,
`XtreamClient`, and `XmltvParser` all consume data from a server the *user*
points the app at — not Microsoft, not us. A malicious or broken playlist URL
is attacker-controlled input by definition. See `security-and-hardening` in
the agent-skills pack this project follows for the general framing; the
concrete bar for this project is below.

## Floor (non-negotiable)

- **No raw string concatenation into URLs/paths.** Build request URLs with
  `UriBuilder`/`Uri.EscapeDataString`; never interpolate user-entered
  server/username/password strings directly into a format string used as a
  URL.
- **All network I/O has a timeout.** No unbounded reads from a stream URL or
  XMLTV/M3U download.
- **Parsers never throw on malformed input in a way that crashes the app.**
  Bad M3U/XMLTV/Xtream JSON results in a caught, reported parse error —
  never an unhandled exception that takes down the UI thread.
- **Credentials (Xtream username/password) are never logged**, including in
  exception messages or crash reports.
- **No `TODO`/stub left in a merged increment** — ship the slice working or
  don't ship it.
- Core parsing/storage logic lives in `TuneGrid.Core` and has **unit tests**
  for: well-formed input, empty input, and at least one malformed-input case
  per parser.

## Targets (adjust as the project grows)

- `TuneGrid.Core` unit test coverage: aim for the parsing/storage code paths
  being exercised by at least one happy-path and one malformed-input test
  each — exact % coverage isn't tracked yet.
- No analyzer warnings (`dotnet build -warnaserror` once CI is set up) in
  `TuneGrid.Core`.

## What would weaken this bar (don't do these quietly)

- Swallowing a parse exception with an empty `catch {}` instead of
  surfacing a reportable error.
- Deleting or skipping a parser test to get CI green.
- Storing the Xtream password in plaintext anywhere other than the local
  SQLite file the user already controls (no cloud sync in v1 — see
  `SPEC.md` non-goals).
