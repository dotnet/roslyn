# Roo Chat Log Skill

A Roo Code agent skill for fetching, analyzing, and exporting chat/task logs from Roo Code's local storage.

## What It Does

This skill provides a Python CLI tool that reads Roo Code's task history stored in VS Code's `globalStorage` directory. It enables:

- **Listing** recent tasks with metadata (tokens, cost, mode, workspace)
- **Fetching** full conversation logs (API messages, UI messages, or metadata)
- **Analyzing** task quality metrics (turn count, tool usage, errors, completion status)
- **Exporting** conversations as readable Markdown

## Quick Start

```bash
# List recent tasks
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py list

# Get the current/most recent task
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py current

# Analyze a task's quality metrics
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py analyze current

# Export as Markdown
uv run --script .agents/skills/roo-chat-log/scripts/chat_log.py export current --output report.md
```

## Requirements

- Python 3.10+
- No external packages (stdlib only)
- Roo Code VS Code extension installed

## Use Cases

- **Quality measurement**: Check if agent workflows complete cleanly
- **Cost tracking**: Monitor token usage and API costs across tasks
- **Audit trails**: Export conversations for review or documentation
- **Debugging**: Inspect tool call patterns and error rates

See [SKILL.md](SKILL.md) for full documentation.
