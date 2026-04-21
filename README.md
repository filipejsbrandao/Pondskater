# Pondskater

Overview
- `Pondskater/` — core source code and native libraries.
- `Pondskater_GH/` — Grasshopper plugin packaging and distribution artifacts (`dist/`).
- `distribution/` — legacy packaged artifacts (copied into `Pondskater_GH/dist/`).
  
Legacy archive
- `distribution/` has been archived to `distribution_archived_20260420T104344Z` — use `Pondskater_GH/dist/` as the authoritative location for plugin artifacts.

Quickstart
1. Build the plugin (from project):

```bash
dotnet build Pondskater/Pondskater.csproj -c Release
```

2. Find packaged `.gha` and zips in `Pondskater_GH/dist/` (or run the helper):

```bash
bash Pondskater_GH/prepare_dist.sh
```

Notes
- The Grasshopper plugin artifacts live under `Pondskater_GH/dist/` to avoid confusion between the repository name and the plugin folder name on GitHub.
- Solution name remains `Pondskater` and the project assembly uses the `Pondskater` namespace.
