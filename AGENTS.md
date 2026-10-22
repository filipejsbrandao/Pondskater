# GraphML_IO and Pondskater

## Shared scope and working directory

This is the shared instruction file for the GraphML_IO repository and its
Pondskater project. Keep shared guidance here so it is available when either
`GraphML_IO/` or `GraphML_IO/Pondskater/` is opened as the workspace.
Paths below are relative to the repository root unless stated otherwise.

The user normally closes the GraphML_IO workspace and opens `Pondskater/` to
test the plugin in Rhino. Treat these as two working directories in the same
repository. Check the current directory before running commands; from the
Pondskater workspace, run repository-level commands in `..`.

Before working on Pondskater code, read `Pondskater/AGENTS.md` for its additional
instructions. Keep project-specific instructions there and shared instructions
in this file.

## Agent work log

Before making changes, read [`docs/agent-log/README.md`](docs/agent-log/README.md),
the five most recent task logs by filename, and any older logs relevant to the
affected files or topic. Inspect Git status and relevant diffs for work not yet
logged. From the Pondskater workspace, the log directory is `../docs/agent-log/`.

For every task that changes repository files, create a separate task log before
editing and append progress after meaningful changes and before handoff or the
final response. Follow the log README and template; include documentation,
configuration, generated artifacts, and code changes. Keep history in the logs
and durable instructions in AGENTS.md.

## Relevant context

- For component behavior or changes to Grasshopper inputs and outputs, read
  `docs/Pondskater_Components.md` and the affected component implementation.
  Preserve existing component GUIDs and saved-definition compatibility unless
  the task explicitly calls for a breaking change.
- For GraphML weights, read `Pondskater/README.md` and the fixtures under
  `tests/graphml/weights/` before changing parsing or wavefront behavior.
- For native Surfer2 changes, read `native/BUILD.md`. Check the native library
  paths in `Pondskater/Pondskater.csproj` as well: the plugin copies libraries
  from `yak/mac/` or `yak/win/`, while smoke tests use `native/bin/`. Verify that
  the test or Rhino process loads the newly built library.
- For packaging, read `Pondskater_GH/README.md` and the build targets in the
  project file. `Pondskater_GH/dist/` is the authoritative artifact directory;
  `distribution_archived_*` directories are historical archives.

## Build and validation

Run these commands from the repository root:

```sh
dotnet build Pondskater/Pondskater.csproj -c Debug -f net7.0
```

For release or cross-target changes, also validate the relevant targets declared
in `Pondskater/Pondskater.csproj`. Builds copy the `.gha` to
`Pondskater_GH/dist/`; review generated changes separately from source edits.

For native interop, weighting, or component-selection changes, run the applicable
smoke programs after preparing the native library using `native/BUILD.md`:

```sh
dotnet run --project tests/SurferNativeSmoke/SurferNativeSmoke.csproj
dotnet run --project tests/SurferAdditiveSmoke/SurferAdditiveSmoke.csproj
dotnet run --project tests/SurferMultiComponentSmoke/SurferMultiComponentSmoke.csproj
```

Keep the repository root as the working directory: some smoke programs resolve
fixtures from the current directory and write output files under `tests/`.

## Testing in Rhino

Use the existing configuration in `Pondskater/.vscode/launch.json` with
`Pondskater/` opened as the workspace. Its `${workspaceFolder}` paths assume
that directory. The configuration launches Rhino 8 on macOS, opens Grasshopper,
and points `RHINO_PACKAGE_DIRS` at the Debug net7.0 output.

The pre-launch task in `Pondskater/.vscode/tasks.json` uses `--no-restore`.
If dependencies have not been restored, run `dotnet restore Pondskater.csproj`
from the Pondskater workspace before launching the debugger.

For changes affecting Grasshopper behavior, exercise the affected component in
Rhino with representative geometry and check outputs and runtime messages.
Report build/smoke results separately from Rhino validation; when Rhino testing
has not been performed, state what remains to be checked.
