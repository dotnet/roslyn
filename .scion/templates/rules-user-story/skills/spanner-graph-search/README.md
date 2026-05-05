# Spanner Graph Search Skill

A reusable AI agent skill that enables coding agents (Roo Code, GitHub Copilot, Claude Code) to query a Google Cloud Spanner Graph database using **self-contained Python CLI tools** with deterministic logic — no ADK dependency required.

## Overview

This skill provides a set of Python CLI subcommands in [`scripts/spanner_tools.py`](scripts/spanner_tools.py) that wrap the `google-cloud-spanner` SDK with sophisticated query patterns:

| Command | Description | Key Features |
|---------|-------------|--------------|
| **`schema`** | Retrieve graph metadata (node types, edge types, labels, properties) | Summary mode for quick overview |
| **`keyword-search`** | Full-text search using `SEARCH()` / `SCORE()` with RQuery syntax | Auto-discovers searchable tables, auto-fetches content, chunked queries |
| **`fetch-content`** | Get full properties of specific nodes by ID | Excludes binary columns, handles chunking |
| **`fetch-neighbours`** | Graph traversal to find connected nodes via edges | Auto-maps edge table names ↔ labels, multi-hop support |
| **`semantic-search`** | Embedding-based similarity search via `ML.PREDICT` | Auto-discovers embeddable tables |
| **`raw-query`** | Execute arbitrary Graph Query Language or SQL statements | Direct passthrough |

## Prerequisites

### 1. Python 3.10+ with `google-cloud-spanner`

The script uses [PEP 723 inline script metadata](https://peps.python.org/pep-0723/), so dependencies are declared directly in the script file. There are three ways to run it:

#### Option A: `uv run --script` (Recommended — zero setup)

If you have [`uv`](https://docs.astral.sh/uv/) installed, just run the script directly. `uv` automatically creates an ephemeral environment with the correct dependencies:

```bash
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py schema --summary
```

No virtual environment, no `pip install` needed — fully portable across projects and operating systems.

#### Option B: Global/system Python with pip

Install the dependency once and use `python` directly:

```bash
pip install google-cloud-spanner
python .agents/skills/spanner-graph-search/scripts/spanner_tools.py schema --summary
```

#### Option C: Existing project venv

If the current project already has `google-cloud-spanner` installed in a virtual environment:

```bash
# Linux / macOS
.venv/bin/python .agents/skills/spanner-graph-search/scripts/spanner_tools.py schema --summary

# Windows
.venv\Scripts\python.exe .agents/skills/spanner-graph-search/scripts/spanner_tools.py schema --summary
```

### 2. Google Cloud Authentication

Authenticate with Application Default Credentials:

```bash
gcloud auth application-default login
```

### 3. Access Permissions

Your GCP account must have read access to the target Spanner instance and database.

## Configuration

1. Copy the example config:
   ```bash
   cp .agents/skills/spanner-graph-search/scripts/env.config.example .agents/skills/spanner-graph-search/scripts/env.config
   ```

2. Edit `env.config` with your Spanner connection details:
   ```ini
   SPANNER_PROJECT_ID=your-gcp-project-id
   SPANNER_INSTANCE_ID=your-spanner-instance
   SPANNER_DATABASE_ID=your-database
   SPANNER_GRAPH_NAME=YourGraphName
   SPANNER_EMBEDDING_MODEL_NAME=GeminiEmbedding001
   SPANNER_SEARCH_TOP_K=5
   ```

The tools read `env.config` automatically — connection flags are optional overrides.

## Usage

The main skill definition lives in [`SKILL.md`](SKILL.md). Compatible coding agents load this skill file to learn how to call the tools.

### Quick Examples

Get graph schema summary:
```bash
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py schema --summary
```

Keyword search with auto-discovery and content fetch:
```bash
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py keyword-search "telegram configuration"
```

Semantic similarity search:
```bash
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py semantic-search "how does I/O mapping work"
```

Graph traversal:
```bash
uv run --script .agents/skills/spanner-graph-search/scripts/spanner_tools.py fetch-neighbours --start-type Page --start-id abc123 --edge-types TEXT_CHUNK_PART_OF_PAGE --hops 2
```

See [`SKILL.md`](SKILL.md) for full command reference and all options.

## File Structure

```
.agents/skills/spanner-graph-search/
├── SKILL.md                        # Main skill definition (tool descriptions & calling conventions)
├── README.md                       # This file
└── scripts/
    ├── spanner_tools.py            # Self-contained Python CLI with all query tools (PEP 723 metadata)
    ├── requirements.txt            # Fallback dependency file for pip users
    └── env.config.example          # Example environment config (copy to env.config)
```

## Design Principles

| Aspect | Approach |
|--------|----------|
| **Deterministic** | Sophisticated logic (auto-discovery, chunking, edge mapping, content fetch) runs as Python code, not LLM-composed queries |
| **Self-contained** | No imports from external packages beyond `google-cloud-spanner` |
| **Portable** | Works in any project via `uv run --script` or global Python; no venv coupling |
| **Testable** | Can be run and tested independently from the command line |
| **JSON output** | Structured, parseable output the agent can reason about |
| **Cross-platform** | No OS-specific paths; works on Windows, macOS, and Linux |

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `ModuleNotFoundError: google.cloud.spanner` | Use `uv run --script` (auto-installs), or run `pip install google-cloud-spanner` |
| Authentication error | Run `gcloud auth application-default login` |
| Graph not found | Verify `SPANNER_GRAPH_NAME` in `env.config` |
| Empty search results | Try different keywords; run `schema --summary` to check which tables have SearchTokens |
| Unknown edge types | Run `schema --summary` to see available edge tables and labels |
| Query timeout | Reduce `--limit`, add `--node-tables`, or simplify the query |

## References

- [PEP 723 — Inline Script Metadata](https://peps.python.org/pep-0723/)
- [uv — An extremely fast Python package installer](https://docs.astral.sh/uv/)
- [Spanner Graph Query Language (GQL)](https://cloud.google.com/spanner/docs/graph/queries)
- [Spanner Full-Text Search](https://cloud.google.com/spanner/docs/full-text-search)
- [google-cloud-spanner Python SDK](https://cloud.google.com/python/docs/reference/spanner/latest)
