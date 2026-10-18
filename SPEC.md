# TuneGrid — Spec

## What it is

TuneGrid is a native Windows IPTV player. It is an original implementation
built against open standards (M3U/M3U8, Xtream Codes API, XMLTV) — it is not
derived from, and shares no code or assets with, any existing IPTV app.

## Goals

1. Import playlists via **M3U/M3U8 URL or file**, or via the **Xtream Codes
   API** (server, username, password).
2. Show a **grid EPG** (channels × time) parsed from **XMLTV**, with
   "now/next" info per channel.
3. Play live streams (**HLS**, **MPEG-TS**, progressive HTTP) via **LibVLC**.
4. Organize channels into **groups/categories** and **favorites**.
5. Persist playlists, EPG cache, and favorites locally (SQLite).

## Non-goals (v1)

- Catch-up/timeshift, recording (DVR) — stretch goal, not blocking v1.
- Multi-view (watching several channels at once).
- Any account/sync service — fully local/offline-first.

## Architecture

```
TuneGrid.Core      netstandard/net8.0 class library — no Windows/UI dependency.
                    Playlist parsing (M3U + Xtream), XMLTV parsing, channel/
                    EPG models, SQLite storage. Unit-testable on any OS/CI.

TuneGrid.App        net8.0-windows10.0.19041 WinUI 3 app. References Core.
                    Views (Setup, Guide, Player), LibVLCSharp playback engine,
                    MVVM view models.

TuneGrid.Core.Tests  xunit tests for Core (parsers are the highest-risk code:
                    malformed M3U/XMLTV from arbitrary user-supplied servers).
```

Splitting Core out matters here specifically because parsers consume
**untrusted input** (a playlist/EPG URL the user points at) — see
`CONSTRAINTS.md` for the threat model this implies.

## Core data model

- `Playlist` — id, name, source (M3U url/file or Xtream credentials), last
  refreshed.
- `Channel` — id, playlist id, name, logo url, group/category, stream url,
  `epg_channel_id` (ties to XMLTV `<channel id>`).
- `EpgProgram` — channel id, start, stop, title, description.
- `FavoriteChannel` — channel id.

## Milestones

1. **Core parsers** — M3U, Xtream client, XMLTV. Unit tested.
2. **Storage** — SQLite schema + repository.
3. **Playback** — LibVLCSharp wrapper behind `IPlaybackEngine`.
4. **UI** — playlist setup → channel list → guide grid → player.
5. **Polish** — favorites, search/filter, error states for bad
   playlists/streams.
