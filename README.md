# Union

One app for movies and TV. Union merges Radarr and Sonarr into a single process: one database, one web UI, one API host, shared indexers, download clients, and notifications. It monitors RSS feeds, grabs releases, sorts and renames files, and upgrades quality when something better shows up.

Forked from upstream Sonarr and Radarr with telemetry and Sentry removed.

## What it looks like

- Single process on port 8989, single SQLite database
- Movies and series in the same UI. Movies live under /movies, with collections and discover included
- Movie API endpoints stay scoped under /api/v3/movie, everything else uses the usual /api/v3 paths
- One combined migration chain, verified against a fresh database

## Features

- Usenet and BitTorrent: SABnzbd, NZBGet, qBittorrent, Deluge, rTorrent, Transmission, uTorrent, and more
- Major indexers for both movies and series
- Automatic failed download handling and quality upgrades
- Manual and interactive search
- Movie import lists, collections, and discovery
- Configurable renaming for episodes and movies
- Windows, Linux, macOS, ARM

## Docker

```sh
docker run -d \
  -p 8989:8989 \
  -v union-config:/config \
  -v /media:/media \
  ghcr.io/sudo-ivan/union:latest
```

Multi-arch image for amd64 and arm64. zstd compressed layers, non-root, cosign keyless signed, SBOM attached, trivy and grype scanned in CI.

## Building

You need the .NET 10 SDK and Node 26.

```sh
yarn install --frozen-lockfile && yarn build
dotnet build src/NzbDrone.Console/Sonarr.Console.csproj -c Release
dotnet _output/net10.0/Union.dll -nobrowser -data /path/to/config
```
## Migrating from Sonarr and Radarr

Union can take over your existing setup without starting over.

For Sonarr, mount your existing config folder straight onto /config (or copy
sonarr.db, config.xml and MediaCover into the Union config folder). Union
recognizes the schema version and upgrades the database in place. Keep a backup
first.

For Radarr, drop the whole Radarr config folder into the Union config folder as
a radarr subdirectory, so <config>/radarr/radarr.db plus config.xml and
MediaCover if present. On first start Union imports movies, collections, files,
metadata, history, blocklist, exclusions, tags, root folders, indexers,
download clients, notifications, import lists, custom formats, users, quality
profiles, filters, delay and release profiles, and remote path mappings.
Matching definitions are deduplicated by name or by implementation and name.
Movie ids are remapped to fresh ids so nothing collides with your series. The
radarr.db stays untouched and gets a radarr.db.imported marker when done, so a
failed import can be retried on the next start.

Movie paths keep pointing at the same folders on disk. Keep your media mounts
identical and the library is picked up as is.

What does not carry over: per-app config.xml values that already exist on the
Sonarr side (Radarr values only fill in keys Union does not already have), and
scheduled task state. Check Settings once it is up.

## License

Union is free software under the GNU General Public License v3. See LICENSE.

Union combines code from Radarr and Sonarr, both GPL v3 projects. Copyright
2010-2025 by their respective contributors. This combined work has been
modified from both upstreams in 2025. See COPYRIGHT.md and the git history for
details.
