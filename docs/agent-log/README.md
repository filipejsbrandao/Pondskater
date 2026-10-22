# Agent work log

This directory records agent changes, decisions, validation, and handoffs for
both GraphML_IO and Pondskater. Git records exact edits; these logs explain their
intent and capture unfinished work. Logging is an agent workflow requirement,
not an automatic audit of every filesystem write.

## Starting a task

1. Read the five most recent task files by filename and search older entries for
   the affected paths or topic. Exclude this README and TEMPLATE.md from the
   recent-task count. Check Git status and relevant diffs against the logs.
2. Copy [TEMPLATE.md](TEMPLATE.md) to a new file named
   `YYYY-MM-DDTHHMMSS-agent-task.md`. Use Europe/Lisbon time and a short agent
   identifier and task slug; add a unique suffix if the name already exists.
3. Fill in the task, author, timestamp with UTC offset, branch, starting commit,
   intended scope, and existing changes before editing implementation files.

## Recording work

- Use one file per task and agent. Parallel agents create their own files and
  reference related logs; no shared index needs updating.
- Append a timestamped update after meaningful changes and before stopping,
  handing off, or giving the final response. Record every changed file or
  coherent group of generated files, the behavior and reason, validation and
  results, and remaining work. Summarize changes rather than copying diffs.
- End each update with an explicit status: `in progress`, `complete`, `blocked`,
  or `handed off`. Name concrete next steps and unresolved risks, or say none.
- Preserve earlier updates and other agents' files. Append corrections or link
  a new follow-up task instead of rewriting another agent's history.
- Identify pre-existing edits separately; only claim changes you made or can
  verify. Record skipped checks and their reason. Include commit IDs when
  available, but logging does not require making a commit.
- Keep credentials, tokens, personal data, and sensitive command output out of
  logs. Reference safe paths and summaries instead.

Before reporting completion, reconcile the task log with the relevant Git diff
and status. Logs provide context, not locks: an in-progress entry signals possible
overlap, so check current files and coordinate before editing the same area.
