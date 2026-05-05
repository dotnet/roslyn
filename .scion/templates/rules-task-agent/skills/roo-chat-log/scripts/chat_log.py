#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = []
# ///
"""
Self-contained CLI tool for fetching and analyzing Roo Code chat/task logs.

Reads task data from VS Code's globalStorage directory where Roo Code persists
all conversation history, token usage, and metadata.

Usage (with uv — recommended):
    uv run --script chat_log.py <command> [options]

Usage (plain Python — no dependencies needed):
    python chat_log.py <command> [options]

Commands:
    list               List recent tasks with summary info
    get                Get full conversation log for a task
    current            Get the most recent task (likely the current one)
    analyze            Analyze a task's conversation for quality metrics
    export             Export a task's conversation as readable Markdown
"""

import argparse
import json
import os
import platform
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

# ---------------------------------------------------------------------------
# Storage location discovery
# ---------------------------------------------------------------------------

EXTENSION_ID = "rooveterinaryinc.roo-cline"


def _get_global_storage_path() -> Path:
    """Locate the Roo Code globalStorage directory based on the OS."""
    system = platform.system()
    if system == "Windows":
        appdata = os.environ.get("APPDATA", "")
        if not appdata:
            raise RuntimeError("APPDATA environment variable not set")
        base = Path(appdata) / "Code" / "User" / "globalStorage"
    elif system == "Darwin":
        base = Path.home() / "Library" / "Application Support" / "Code" / "User" / "globalStorage"
    else:  # Linux and others
        base = Path.home() / ".config" / "Code" / "User" / "globalStorage"

    storage = base / EXTENSION_ID
    if not storage.exists():
        raise RuntimeError(
            f"Roo Code globalStorage not found at: {storage}\n"
            f"Make sure Roo Code (VS Code extension) is installed."
        )
    return storage


def _get_tasks_dir() -> Path:
    """Return the tasks directory path."""
    tasks_dir = _get_global_storage_path() / "tasks"
    if not tasks_dir.exists():
        raise RuntimeError(f"Tasks directory not found at: {tasks_dir}")
    return tasks_dir


# ---------------------------------------------------------------------------
# Index loading
# ---------------------------------------------------------------------------


def load_index(tasks_dir: Path) -> list[dict[str, Any]]:
    """Load and return the task index entries, sorted by timestamp descending."""
    index_path = tasks_dir / "_index.json"
    if not index_path.exists():
        raise RuntimeError(f"Task index not found at: {index_path}")

    data = json.loads(index_path.read_text(encoding="utf-8"))
    entries = data.get("entries", [])
    # Sort by timestamp descending (most recent first)
    entries.sort(key=lambda e: e.get("ts", 0), reverse=True)
    return entries


def load_task_file(tasks_dir: Path, task_id: str, filename: str) -> Any:
    """Load a JSON file from a specific task directory."""
    file_path = tasks_dir / task_id / filename
    if not file_path.exists():
        raise FileNotFoundError(f"File not found: {file_path}")
    return json.loads(file_path.read_text(encoding="utf-8"))


# ---------------------------------------------------------------------------
# Formatting helpers
# ---------------------------------------------------------------------------


def ts_to_iso(ts_ms: int) -> str:
    """Convert millisecond timestamp to ISO 8601 string."""
    return datetime.fromtimestamp(ts_ms / 1000, tz=timezone.utc).isoformat()


def truncate(text: str, max_len: int = 100) -> str:
    """Truncate text to max_len characters."""
    if len(text) <= max_len:
        return text
    return text[: max_len - 3] + "..."


def format_cost(cost: float) -> str:
    """Format cost as USD string."""
    return f"${cost:.4f}"


# ---------------------------------------------------------------------------
# Analysis helpers
# ---------------------------------------------------------------------------


def extract_messages(conversation: list[dict]) -> list[dict[str, Any]]:
    """Extract a simplified message list from api_conversation_history."""
    messages = []
    for entry in conversation:
        role = entry.get("role", "unknown")
        content = entry.get("content", [])
        ts = entry.get("ts")

        if isinstance(content, str):
            messages.append({
                "role": role,
                "ts": ts,
                "text": content[:500],
                "tool_calls": [],
                "tool_results": [],
            })
            continue

        text_parts = []
        tool_calls = []
        tool_results = []

        for block in content:
            block_type = block.get("type", "")
            if block_type == "text":
                raw = block.get("text", "")
                # Strip environment_details XML blocks entirely
                if "<environment_details>" in raw:
                    # Extract just the user_message part if present
                    user_msg = re.search(
                        r"<user_message>(.*?)</user_message>", raw, re.DOTALL)
                    if user_msg:
                        text_parts.append(user_msg.group(1).strip())
                    # Otherwise skip the environment_details-only block
                else:
                    # Strip <user_message> wrapper tags if present
                    cleaned = re.sub(r"</?user_message>", "", raw).strip()
                    if cleaned:
                        text_parts.append(cleaned)
            elif block_type == "tool_use":
                tc_entry: dict[str, Any] = {"tool": block.get("name", "unknown"), "input_keys": list(
                    block.get("input", {}).keys()) if isinstance(block.get("input"), dict) else [], }
                # Capture the result text from attempt_completion
                inp = block.get("input", {})
                if isinstance(inp, dict) and block.get("name") == "attempt_completion":
                    tc_entry["result"] = inp.get("result", "")
                tool_calls.append(tc_entry)
            elif block_type == "tool_result":
                tool_results.append({
                    "tool_use_id": block.get("tool_use_id", ""),
                    "is_error": block.get("is_error", False),
                })

        messages.append({
            "role": role,
            "ts": ts,
            "text": "\n".join(text_parts)[:500] if text_parts else "",
            "tool_calls": tool_calls,
            "tool_results": tool_results,
        })

    return messages


def analyze_conversation(conversation: list[dict], index_entry: dict) -> dict[str, Any]:
    """Produce quality/measurement metrics from a task's conversation."""
    messages = extract_messages(conversation)

    # Count turns
    user_turns = sum(1 for m in messages if m["role"] == "user")
    assistant_turns = sum(1 for m in messages if m["role"] == "assistant")

    # Tool usage analysis
    all_tool_calls = []
    tool_errors = 0
    for m in messages:
        all_tool_calls.extend(m["tool_calls"])
        tool_errors += sum(1 for tr in m["tool_results"] if tr.get("is_error"))

    tool_counts: dict[str, int] = {}
    for tc in all_tool_calls:
        tool_name = tc["tool"]
        tool_counts[tool_name] = tool_counts.get(tool_name, 0) + 1

    # Check workflow completeness
    ended_with_completion = False
    last_assistant = None
    for m in reversed(messages):
        if m["role"] == "assistant":
            last_assistant = m
            break
    if last_assistant:
        ended_with_completion = any(
            tc["tool"] == "attempt_completion" for tc in last_assistant["tool_calls"]
        )

    # Mode switches
    mode_switches = sum(
        1
        for m in messages
        for tc in m["tool_calls"]
        if tc["tool"] == "switch_mode"
    )

    # Followup questions
    followup_questions = sum(
        1
        for m in messages
        for tc in m["tool_calls"]
        if tc["tool"] == "ask_followup_question"
    )

    # First user prompt
    first_prompt = ""
    for m in messages:
        if m["role"] == "user" and m["text"]:
            first_prompt = m["text"]
            break

    # Last assistant message text
    last_response = ""
    if last_assistant and last_assistant["text"]:
        last_response = last_assistant["text"]

    # Timestamps
    first_ts = messages[0]["ts"] if messages and messages[0].get("ts") else None
    last_ts = messages[-1]["ts"] if messages and messages[-1].get("ts") else None
    duration_seconds = None
    if first_ts and last_ts:
        duration_seconds = round((last_ts - first_ts) / 1000, 1)

    return {
        "task_id": index_entry.get("id", "unknown"),
        "task_summary": truncate(index_entry.get("task", ""), 200),
        "mode": index_entry.get("mode", "unknown"),
        "workspace": index_entry.get("workspace", "unknown"),
        "started_at": ts_to_iso(first_ts) if first_ts else None,
        "ended_at": ts_to_iso(last_ts) if last_ts else None,
        "duration_seconds": duration_seconds,
        "metrics": {
            "user_turns": user_turns,
            "assistant_turns": assistant_turns,
            "total_turns": user_turns + assistant_turns,
            "total_tool_calls": len(all_tool_calls),
            "tool_errors": tool_errors,
            "tool_usage": tool_counts,
            "mode_switches": mode_switches,
            "followup_questions": followup_questions,
            "ended_with_completion": ended_with_completion,
        },
        "tokens": {
            "input": index_entry.get("tokensIn", 0),
            "output": index_entry.get("tokensOut", 0),
            "cache_writes": index_entry.get("cacheWrites", 0),
            "cache_reads": index_entry.get("cacheReads", 0),
        },
        "cost": index_entry.get("totalCost", 0),
        "first_prompt": truncate(first_prompt, 300),
        "last_response": truncate(last_response, 300),
    }


# ---------------------------------------------------------------------------
# Export to Markdown
# ---------------------------------------------------------------------------


def export_markdown(conversation: list[dict], index_entry: dict) -> str:
    """Export a task conversation as readable Markdown."""
    messages = extract_messages(conversation)
    lines = []

    task_text = index_entry.get("task", "Untitled Task")
    task_id = index_entry.get("id", "unknown")
    mode = index_entry.get("mode", "unknown")
    cost = index_entry.get("totalCost", 0)
    ts = index_entry.get("ts", 0)

    lines.append(f"# Task: {truncate(task_text, 200)}")
    lines.append("")
    lines.append(f"- **Task ID:** `{task_id}`")
    lines.append(f"- **Mode:** {mode}")
    lines.append(f"- **Workspace:** {index_entry.get('workspace', 'N/A')}")
    lines.append(f"- **Timestamp:** {ts_to_iso(ts) if ts else 'N/A'}")
    lines.append(f"- **Total Cost:** {format_cost(cost)}")
    lines.append(f"- **Tokens In:** {index_entry.get('tokensIn', 0):,}")
    lines.append(f"- **Tokens Out:** {index_entry.get('tokensOut', 0):,}")
    lines.append("")
    lines.append("---")
    lines.append("")

    for i, msg in enumerate(messages):
        role_label = "🧑 User" if msg["role"] == "user" else "🤖 Assistant"
        ts_label = f" ({ts_to_iso(msg['ts'])})" if msg.get("ts") else ""
        lines.append(f"## {role_label}{ts_label}")
        lines.append("")

        if msg["text"]:
            lines.append(msg["text"])
            lines.append("")

        if msg["tool_calls"]:
            for tc in msg["tool_calls"]:
                lines.append(f"- 🔧 **Tool call:** `{tc['tool']}`")
                # Render attempt_completion result as a blockquote
                if tc["tool"] == "attempt_completion" and tc.get("result"):
                    lines.append("")
                    lines.append("### ✅ Task Completed")
                    lines.append("")
                    for result_line in tc["result"].splitlines():
                        lines.append(f"> {result_line}")
            lines.append("")

        if msg["tool_results"]:
            for tr in msg["tool_results"]:
                status = "❌ Error" if tr.get("is_error") else "✅ Success"
                lines.append(f"- {status}")
            lines.append("")

        lines.append("---")
        lines.append("")

    return "\n".join(lines)


# ---------------------------------------------------------------------------
# CLI Commands
# ---------------------------------------------------------------------------


def cmd_list(args: argparse.Namespace) -> None:
    """List recent tasks."""
    tasks_dir = _get_tasks_dir()
    entries = load_index(tasks_dir)

    # Filter by workspace if specified
    if args.workspace:
        ws = args.workspace.lower().replace("/", "\\")
        entries = [
            e for e in entries if ws in e.get(
                "workspace",
                "").lower().replace(
                "/",
                "\\")]

    # Filter by mode if specified
    if args.mode:
        entries = [e for e in entries if e.get("mode", "") == args.mode]

    # Limit results
    entries = entries[: args.limit]

    result = []
    for entry in entries:
        result.append({
            "id": entry.get("id"),
            "task": truncate(entry.get("task", ""), 150),
            "mode": entry.get("mode", ""),
            "workspace": entry.get("workspace", ""),
            "timestamp": ts_to_iso(entry["ts"]) if entry.get("ts") else None,
            "tokensIn": entry.get("tokensIn", 0),
            "tokensOut": entry.get("tokensOut", 0),
            "totalCost": entry.get("totalCost", 0),
        })

    print(json.dumps(result, indent=2))


def cmd_get(args: argparse.Namespace) -> None:
    """Get full conversation log for a task."""
    tasks_dir = _get_tasks_dir()
    task_id = args.task_id

    # Determine which file to load
    if args.source == "api":
        filename = "api_conversation_history.json"
    elif args.source == "ui":
        filename = "ui_messages.json"
    elif args.source == "metadata":
        filename = "task_metadata.json"
    else:
        filename = "api_conversation_history.json"

    try:
        data = load_task_file(tasks_dir, task_id, filename)
    except FileNotFoundError as e:
        print(json.dumps({"error": str(e)}), file=sys.stderr)
        sys.exit(1)

    if args.summary:
        # For API conversation, extract simplified messages
        if args.source in ("api", None, "api"):
            messages = extract_messages(data)
            print(json.dumps(messages, indent=2))
        else:
            print(json.dumps(data, indent=2))
    else:
        print(json.dumps(data, indent=2))


def cmd_current(args: argparse.Namespace) -> None:
    """Get the most recent task (current task)."""
    tasks_dir = _get_tasks_dir()
    entries = load_index(tasks_dir)

    # Filter by workspace if specified
    if args.workspace:
        ws = args.workspace.lower().replace("/", "\\")
        entries = [
            e for e in entries if ws in e.get(
                "workspace",
                "").lower().replace(
                "/",
                "\\")]

    if not entries:
        print(json.dumps({"error": "No tasks found"}), file=sys.stderr)
        sys.exit(1)

    # The first entry is the most recent (sorted desc by ts)
    current = entries[0]

    # Optionally skip N tasks from the top (to get "previous" tasks)
    offset = args.offset if args.offset else 0
    if offset >= len(entries):
        print(json.dumps(
            {"error": f"Offset {offset} exceeds available tasks ({len(entries)})"}), file=sys.stderr)
        sys.exit(1)
    current = entries[offset]

    result = {
        "id": current.get("id"),
        "task": current.get("task", ""),
        "mode": current.get("mode", ""),
        "workspace": current.get("workspace", ""),
        "timestamp": ts_to_iso(current["ts"]) if current.get("ts") else None,
        "tokensIn": current.get("tokensIn", 0),
        "tokensOut": current.get("tokensOut", 0),
        "cacheWrites": current.get("cacheWrites", 0),
        "cacheReads": current.get("cacheReads", 0),
        "totalCost": current.get("totalCost", 0),
    }

    print(json.dumps(result, indent=2))


def cmd_analyze(args: argparse.Namespace) -> None:
    """Analyze a task's conversation for quality metrics."""
    tasks_dir = _get_tasks_dir()

    # Resolve task ID
    task_id = args.task_id
    if task_id == "current":
        entries = load_index(tasks_dir)
        if args.workspace:
            ws = args.workspace.lower().replace("/", "\\")
            entries = [
                e for e in entries if ws in e.get(
                    "workspace",
                    "").lower().replace(
                    "/",
                    "\\")]
        if not entries:
            print(json.dumps({"error": "No tasks found"}), file=sys.stderr)
            sys.exit(1)
        task_id = entries[0]["id"]
        index_entry = entries[0]
    else:
        entries = load_index(tasks_dir)
        index_entry = next((e for e in entries if e["id"] == task_id), None)
        if not index_entry:
            print(json.dumps(
                {"error": f"Task {task_id} not found in index"}), file=sys.stderr)
            sys.exit(1)

    try:
        conversation = load_task_file(
            tasks_dir, task_id, "api_conversation_history.json")
    except FileNotFoundError as e:
        print(json.dumps({"error": str(e)}), file=sys.stderr)
        sys.exit(1)

    analysis = analyze_conversation(conversation, index_entry)
    print(json.dumps(analysis, indent=2))


def cmd_export(args: argparse.Namespace) -> None:
    """Export a task's conversation as Markdown."""
    tasks_dir = _get_tasks_dir()

    # Resolve task ID
    task_id = args.task_id
    entries = load_index(tasks_dir)

    if task_id == "current":
        if args.workspace:
            ws = args.workspace.lower().replace("/", "\\")
            entries = [
                e for e in entries if ws in e.get(
                    "workspace",
                    "").lower().replace(
                    "/",
                    "\\")]
        if not entries:
            print("Error: No tasks found", file=sys.stderr)
            sys.exit(1)
        task_id = entries[0]["id"]
        index_entry = entries[0]
    else:
        index_entry = next((e for e in entries if e["id"] == task_id), None)
        if not index_entry:
            print(f"Error: Task {task_id} not found in index", file=sys.stderr)
            sys.exit(1)

    try:
        conversation = load_task_file(
            tasks_dir, task_id, "api_conversation_history.json")
    except FileNotFoundError as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

    md = export_markdown(conversation, index_entry)

    if args.output:
        Path(args.output).write_text(md, encoding="utf-8")
        print(json.dumps({"exported": args.output, "task_id": task_id}))
    else:
        print(md)


# ---------------------------------------------------------------------------
# CLI entry point
# ---------------------------------------------------------------------------


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Fetch and analyze Roo Code chat/task logs.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    # --- list ---
    p_list = subparsers.add_parser("list", help="List recent tasks")
    p_list.add_argument(
        "--limit",
        type=int,
        default=20,
        help="Max tasks to return (default: 20)")
    p_list.add_argument(
        "--workspace",
        type=str,
        help="Filter by workspace path (substring match)")
    p_list.add_argument(
        "--mode",
        type=str,
        help="Filter by mode (e.g., code, ask, architect)")
    p_list.set_defaults(func=cmd_list)

    # --- get ---
    p_get = subparsers.add_parser(
        "get", help="Get conversation log for a specific task")
    p_get.add_argument("task_id", help="Task UUID")
    p_get.add_argument(
        "--source",
        choices=["api", "ui", "metadata"],
        default="api",
        help="Which log file to read (default: api)",
    )
    p_get.add_argument(
        "--summary",
        action="store_true",
        help="Return simplified message list instead of raw data")
    p_get.set_defaults(func=cmd_get)

    # --- current ---
    p_current = subparsers.add_parser("current", help="Get the most recent task")
    p_current.add_argument("--workspace", type=str, help="Filter by workspace path")
    p_current.add_argument(
        "--offset",
        type=int,
        default=0,
        help="Skip N most recent tasks (0 = latest)")
    p_current.set_defaults(func=cmd_current)

    # --- analyze ---
    p_analyze = subparsers.add_parser(
        "analyze", help="Analyze a task for quality metrics")
    p_analyze.add_argument(
        "task_id",
        help="Task UUID or 'current' for the most recent task")
    p_analyze.add_argument(
        "--workspace",
        type=str,
        help="Filter by workspace (used with 'current')")
    p_analyze.set_defaults(func=cmd_analyze)

    # --- export ---
    p_export = subparsers.add_parser("export", help="Export a task as Markdown")
    p_export.add_argument(
        "task_id",
        help="Task UUID or 'current' for the most recent task")
    p_export.add_argument(
        "--workspace",
        type=str,
        help="Filter by workspace (used with 'current')")
    p_export.add_argument(
        "--output",
        "-o",
        type=str,
        help="Write to file instead of stdout")
    p_export.set_defaults(func=cmd_export)

    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
