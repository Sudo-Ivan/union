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

You need the .NET 10 SDK and Node 24.

```sh
yarn install --frozen-lockfile && yarn build
dotnet build src/NzbDrone.Console/Sonarr.Console.csproj -c Release
dotnet _output/net10.0/Union.dll -nobrowser -data /path/to/config
```
