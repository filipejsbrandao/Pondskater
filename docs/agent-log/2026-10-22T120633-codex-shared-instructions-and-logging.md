# Shared instructions and agent logging

- Started: 2026-10-22T12:06:33+01:00, Europe/Lisbon
- Agent: Codex, primary agent in this conversation
- Branch: main
- Starting commit: cdb0d6d
- Related logs: none; this is the first entry.

## Intent and starting state

- The user requested shared instructions available from the GraphML_IO and
  Pondskater workspaces, then requested a persistent agent work log.
- The shared instructions were created earlier in this conversation; the entry
  below records that work retrospectively. The timestamp above marks log setup,
  not the start of the earlier task.
- Pre-existing changes in `Pondskater/MiterComponent.cs` and `pondskater-site`
  were not made by this agent and remain untouched. At log setup, `AGENTS.md`
  was untracked and `Pondskater/AGENTS.md` was modified by this conversation.

## Retrospective — Shared instructions

- Changes: Created root `AGENTS.md` with shared workspace, build, native library,
  packaging, validation, and Rhino debugging guidance. Added a parent-file link
  to `Pondskater/AGENTS.md`, preserving its Graphify instructions.
- Decision: Use the repository root for shared instructions because Pondskater
  is nested within it; explicitly support opening Pondskater alone for Rhino.
- Validation: Checked guidance against project documentation, project file,
  smoke program sources, and VS Code launch/task configuration. `git diff
  --check` passed. Builds and Rhino tests were not run for documentation edits.
- Status: complete
- Remaining work / risks: none for the shared-instructions task.

## 2026-10-22T12:06:33+01:00 — Logging setup

- Planned changes: Add `docs/agent-log/README.md`, `docs/agent-log/TEMPLATE.md`,
  and this task file; add the logging requirement to root `AGENTS.md`.
- Decision: One task file per agent avoids concurrent writes to a central log.
  Pondskater reaches the same directory through the shared instructions.
- Validation plan: Check relative documentation links, required files, whitespace,
  and Git status; builds and Rhino tests are unnecessary for these text changes.
- Status: in progress
- Next steps: Verify the documentation and append the outcome.

## 2026-10-22T12:08:11+01:00 — Logging setup complete

- Changes: Added the README, reusable template, and initial task log under
  `docs/agent-log/`. Updated root `AGENTS.md` to require reading recent and
  relevant logs and recording all repository-changing tasks. The existing
  Pondskater parent link makes this workflow available from either workspace.
- Validation: `git diff --check` passed. A local check verified relative links
  and trailing whitespace across all five instruction/log Markdown files.
  Reviewed Git status; unrelated existing changes remain untouched. Builds and
  Rhino testing were skipped because only documentation changed.
- Status: complete
- Remaining work / risks: Logging depends on agents following the instructions;
  it is not automatically enforced. Earlier work outside this conversation has
  not been reconstructed. No implementation work remains.
