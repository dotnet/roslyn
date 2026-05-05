---
name: roo-chat-log
description: >-
  provides instructions for fetching, analyzing, and exporting Roo Code chat/task
  logs from the local VS Code globalStorage. Useful for measuring workflow quality,
  token usage, cost tracking, and auditing agent conversations.
---

# Roo Chat Log Skill

This skill provides a **self-contained Python CLI tool** for fetching and analyzing Roo Code task/chat logs stored in VS Code's globalStorage. Use it to measure workflow quality, audit conversations, track costs, and export logs.

## Prerequisites

- **Python 3.10+** available on the system PATH
- **`uv`** installed (recommended — handles script execution cleanly) **OR** plain `python` (no external dependencies needed)
- Roo Code VS Code extension installed (the tool reads its local storage)

No external packages are required — the tool uses only the Python standard library.

## How to Run Tools

All tools are subcommands of the [`scripts/chat_log.py`](scripts/chat_log.py) script. Execute them using **`uv run --script`** (recommended):

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py <command> [options]
```

> **Note:** All commands output **JSON to stdout** (except `export` which outputs Markdown). Errors go to stderr.

> **Alternative:** Use `python .agents/skills/roo-chat-log/scripts/chat_log.py <command> [options]` — no dependencies to install.

---

## Available Tools

### 1. List Tasks (`list`)

List recent tasks with summary info (ID, prompt, mode, workspace, cost, tokens).

**List the 20 most recent tasks:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py list
```

**Filter by workspace:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py list --workspace "agent-skills-hub"
```

**Filter by mode:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py list --mode code
```

**Control result count:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py list --limit 50
```

**Output format:**

```json
[
  {
    "id": "019d010a-bdb4-7178-bca0-88a786e7a19d",
    "task": "is it possible to fetch a roo code chat log?...",
    "mode": "code",
    "workspace": "c:\\git\\AI\\agent-skills-hub",
    "timestamp": "2026-03-18T13:07:12.394000+00:00",
    "tokensIn": 160921,
    "tokensOut": 3910,
    "totalCost": 0.8025
  }
]
```

### 2. Get Current Task (`current`)

Get metadata for the most recent task (likely the currently running one).

**Get latest task:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py current
```

**Get latest task in a specific workspace:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py current --workspace "agent-skills-hub"
```

**Get the Nth most recent task (offset):**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py current --offset 1
```

**Output format:**

```json
{
  "id": "019d010a-bdb4-7178-bca0-88a786e7a19d",
  "task": "is it possible to fetch a roo code chat log?...",
  "mode": "code",
  "workspace": "c:\\git\\AI\\agent-skills-hub",
  "timestamp": "2026-03-18T13:07:12.394000+00:00",
  "tokensIn": 160921,
  "tokensOut": 3910,
  "cacheWrites": 42976,
  "cacheReads": 117865,
  "totalCost": 0.8025
}
```

### 3. Get Full Conversation (`get`)

Retrieve the full conversation log for a specific task.

**Get API conversation history (full LLM messages):**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py get <task-id>
```

**Get simplified/summarized messages:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py get <task-id> --summary
```

**Get UI messages instead:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py get <task-id> --source ui
```

**Get task metadata (files in context):**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py get <task-id> --source metadata
```

**Source options:**

| `--source` | File | Content |
|---|---|---|
| `api` (default) | `api_conversation_history.json` | Full LLM conversation with tool calls |
| `ui` | `ui_messages.json` | Messages as shown in Roo Code UI |
| `metadata` | `task_metadata.json` | Files in context, read/edit timestamps |

### 4. Analyze Task (`analyze`)

Produce quality and measurement metrics for a task's conversation.

**Analyze a specific task:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py analyze <task-id>
```

**Analyze the most recent task:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py analyze current
```

**Analyze current task in a specific workspace:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py analyze current --workspace "agent-skills-hub"
```

**Output format:**

```json
{
  "task_id": "019d010a-bdb4-7178-bca0-88a786e7a19d",
  "task_summary": "is it possible to fetch a roo code chat log?...",
  "mode": "code",
  "workspace": "c:\\git\\AI\\agent-skills-hub",
  "started_at": "2026-03-18T13:02:54.719000+00:00",
  "ended_at": "2026-03-18T13:07:20.888000+00:00",
  "duration_seconds": 266.2,
  "metrics": {
    "user_turns": 5,
    "assistant_turns": 5,
    "total_turns": 10,
    "total_tool_calls": 12,
    "tool_errors": 0,
    "tool_usage": {
      "attempt_completion": 1,
      "read_file": 4,
      "execute_command": 3,
      "switch_mode": 1,
      "list_files": 1,
      "update_todo_list": 1,
      "write_to_file": 1
    },
    "mode_switches": 1,
    "followup_questions": 0,
    "ended_with_completion": true
  },
  "tokens": {
    "input": 160921,
    "output": 3910,
    "cache_writes": 42976,
    "cache_reads": 117865
  },
  "cost": 0.8025,
  "first_prompt": "is it possible to fetch a roo code chat log?...",
  "last_response": "Now I have a complete understanding of the data structures..."
}
```

**Key metrics explained:**

| Metric | Meaning |
|---|---|
| `ended_with_completion` | Did the workflow end with `attempt_completion`? (`true` = clean finish) |
| `total_turns` | Total message exchanges — lower is generally better |
| `tool_errors` | How many tool calls failed — indicates friction |
| `followup_questions` | How many times the agent asked for clarification |
| `mode_switches` | How many mode transitions (relevant for orchestrator flows) |
| `duration_seconds` | Wall-clock time from first to last message |

### 5. Export as Markdown (`export`)

Export a task's conversation as human-readable Markdown.

**Export to stdout:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py export <task-id>
```

**Export the current task:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py export current
```

**Export to a file:**

```
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py export <task-id> --output chat-log.md
```

---

## Example Workflows

### Measure Current Chat Quality

```
1. Run `current` to get the active task ID
2. Run `analyze <task-id>` to get quality metrics
3. Check: ended_with_completion? tool_errors == 0? reasonable turn count?
```

### Audit a Specific Workflow

```
1. Run `list --workspace "my-project"` to find the task
2. Run `get <task-id> --summary` to see the simplified conversation
3. Run `analyze <task-id>` for metrics
4. Run `export <task-id> --output audit.md` for a readable report
```

### Cost Tracking Across Tasks

```
1. Run `list --limit 100 --workspace "my-project"`
2. Parse the JSON output to sum `totalCost` across tasks
```

## Data Location

Roo Code stores task data at:

| OS | Path |
|---|---|
| **Windows** | `%APPDATA%/Code/User/globalStorage/rooveterinaryinc.roo-cline/tasks/` |
| **macOS** | `~/Library/Application Support/Code/User/globalStorage/rooveterinaryinc.roo-cline/tasks/` |
| **Linux** | `~/.config/Code/User/globalStorage/rooveterinaryinc.roo-cline/tasks/` |

Each task folder contains:

| File | Purpose |
|---|---|
| `api_conversation_history.json` | Full LLM conversation (messages, tool calls, tool results) |
| `ui_messages.json` | UI-facing message display data |
| `task_metadata.json` | Files in context with read/edit timestamps |
| `history_item.json` | Task index entry (same as in `_index.json`) |
| `checkpoints/` | Intermediate state checkpoints |
| `command-output/` | Stored command execution output |

## Error Handling

- If Roo Code is not installed: the tool reports the expected globalStorage path
- If a task ID is not found: a clear error message with the invalid ID
- All errors are written to stderr as JSON (`{"error": "..."}`)
- The tool never modifies any files — it is strictly read-only
