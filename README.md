# TuneGrid

A native Windows IPTV player built with WinUI 3 and LibVLCSharp. Original
implementation against open standards — no code or assets from any existing
IPTV app.

See [`SPEC.md`](SPEC.md) for what it does and how it's structured, and
[`CONSTRAINTS.md`](CONSTRAINTS.md) for this project's quality bar.

## Features (v1 target)

- Import playlists via M3U/M3U8 URL, or the Xtream Codes API.
- Grid EPG from XMLTV, with now/next per channel.
- Live playback (HLS, MPEG-TS) via LibVLC.
- Channel groups and favorites.
- Fully local/offline-first — SQLite storage, no account or sync service.

## Project layout

```
src/TuneGrid.Core    Playlist/EPG parsing, Xtream client, SQLite storage.
                      net8.0, no Windows dependency — builds and tests on
                      any OS/CI runner.
src/TuneGrid.App      WinUI 3 app (net8.0-windows). Views, view models,
                      LibVLCSharp playback. Windows-only — build on Windows
                      or via the Windows CI job.
tests/TuneGrid.Core.Tests   xunit tests for the Core parsers.
```

## Building

**Core + tests** (any OS with the .NET 8 SDK):

```bash
dotnet test tests/TuneGrid.Core.Tests/TuneGrid.Core.Tests.csproj
```

**The Windows app** (requires Windows, the .NET 8 SDK, and the Windows App
SDK — Visual Studio 2022 with the "Windows App SDK" / WinUI workload covers
all of it):

```powershell
dotnet build src/TuneGrid.App/TuneGrid.App.csproj
```

CI (`.github/workflows/ci.yml`) builds and tests `TuneGrid.Core` on
`ubuntu-latest` and builds `TuneGrid.App` on `windows-latest` on every push.

> **Note:** this scaffold was authored in a Linux sandbox with no .NET SDK
> reachable (network policy blocked the SDK download host), so none of this
> has been locally built or run yet. Treat the first `dotnet restore`/
> `dotnet build` on a real machine as the real smoke test, and expect to fix
> small things (package version pins, WinUI project quirks) that only show
> up once a build actually runs.

## Status

Early scaffold: parsers (M3U, XMLTV, Xtream) and storage are implemented
with tests; the WinUI shell has three screens (playlist setup, guide,
player) wired to the view models but UI polish, DI wiring, and favorites
are still TODO.
