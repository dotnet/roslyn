# Install Roo Modes Skill

A reusable AI agent skill that synchronises **Roo mode definitions**, **Roo rules**, and **GitHub/Copilot agent configurations** from a central source repository into a local project. Keeps your project's agent modes consistent with the latest shared definitions without manual file copying.

## Overview

This is an **instruction-only** skill — no scripts or runtime dependencies. It depends on the **`gitlab-tools` skill** for all GitLab API access. The agent follows step-by-step instructions in [`SKILL.md`](SKILL.md) to:

1. Fetch mode definitions from a configurable GitLab source repository via `glab api` (provided by `gitlab-tools`)
2. Ask the user whether to **overwrite** or **merge** with existing local definitions
3. Apply changes locally (never commits or pushes)

### What It Syncs

| Component | Path | Description |
|-----------|------|-------------|
| Mode definitions | `.roomodes` | JSON file registering custom Roo modes |
| Mode rules | `.roo/rules-{slug}/*.md` | Per-mode instruction files |
| Global rules | `.roo/rules/*.md` | Shared rules for all modes |
| Agent configs | `.github/agents/*.md` | GitHub Copilot agent definitions |

### What It Does NOT Sync

- Skills (`.agents/skills/`) — managed separately
- `.claude/` directory — out of scope
- Any other project files

## Prerequisites

- **`gitlab-tools` skill** installed in your project at `.agents/skills/gitlab-tools/`
- All `gitlab-tools` prerequisites met (`glab` CLI installed and authenticated) — see the [`gitlab-tools` README](../gitlab-tools/README.md) for setup instructions

## Configuration

1. Copy the example config:
   ```bash
   cp .agents/skills/install-roo-modes/env.config.example .agents/skills/install-roo-modes/env.config
   ```

2. Edit `env.config` with your source repository details:
   ```ini
   SOURCE_REPO=https://gitlab.example.com/my-group/my-modes-repo
   SOURCE_BRANCH=main
   ```

The agent parses the GitLab host and project path from `SOURCE_REPO` automatically.

## Usage

Ask a compatible coding agent to install or update modes:

- _"Sync my Roo modes from the source repository"_
- _"Update my agent modes and rules from the hub"_
- _"Install the latest mode definitions"_

The agent will:
1. Read `env.config` for the source repository
2. Verify access to the source repo
3. Ask you: **Overwrite** or **Merge**?
4. Fetch and apply changes component by component
5. Present a summary of all changes

### Merge Mode

In merge mode, the skill handles conflicts interactively:

- **New items** (in source but not local) → added automatically
- **Identical items** → skipped
- **Changed items** (in both but different) → diff shown, you choose per item
- **Local-only items** (in local but not source) → always preserved

### Overwrite Mode

Replaces all synced components with the source versions. Local-only customisations in `.roomodes`, `.roo/rules-*/`, and `.github/agents/` will be lost.

## Installation in Your Project

### For Roo Code

Copy the skill directory into your project:

```bash
cp -r .agents/skills/install-roo-modes /path/to/your-project/.agents/skills/
```

Then configure `env.config` to point at your source repository.

### For GitHub Copilot

Reference the SKILL.md content in your `.github/copilot-instructions.md` or include the skill directory in your workspace.

### For Claude Code

Include the SKILL.md content in your `CLAUDE.md` project instructions or reference it from your project's configuration.

## File Structure

```
.agents/skills/install-roo-modes/
├── SKILL.md                # Main skill definition (agent-facing instructions)
├── README.md               # This file (human-facing documentation)
├── env.config.example      # Example configuration template (copy to env.config)
└── env.config              # Your local config (gitignored — never committed)
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `glab` not found | Install using the platform-specific command above |
| Authentication failed | Run `glab auth login --hostname <host>` for the source repo's host |
| Source repo not found | Verify `SOURCE_REPO` in `env.config` — must be the full URL |
| 404 on `.github/agents/` | Normal if the source repo has no Copilot agents — the skill skips this component |
| Local `.roomodes` parse error | Fix the JSON manually, then retry the sync |
| `env.config` missing | Copy `env.config.example` to `env.config` and edit the values |

## Dependencies

- **`gitlab-tools` skill** — required; provides GitLab API access via the `glab` CLI. Must be installed at `.agents/skills/gitlab-tools/`
- **`glab` CLI** — required by `gitlab-tools`; must be installed and authenticated for the source repository's GitLab host

## References

- [glab CLI documentation](https://gitlab.com/gitlab-org/cli)
- [GitLab Repository Files API](https://docs.gitlab.com/ee/api/repository_files.html)
- [GitLab Repositories API — Tree](https://docs.gitlab.com/ee/api/repositories.html#list-repository-tree)
