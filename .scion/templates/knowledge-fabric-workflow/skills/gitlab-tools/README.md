# GitLab Tools Skill

A reusable AI agent skill that enables coding agents (Roo Code, GitHub Copilot, Claude Code) to interact with the **GitLab API** via the [`glab` CLI](https://gitlab.com/gitlab-org/cli) — listing and creating issues, managing merge requests, working with branches, and more.

## Overview

This skill wraps the `glab` CLI to provide coding agents with GitLab project management capabilities. The agent constructs `glab` commands and executes them via `execute_command`.

| Category  | Actions                                                                 | User Approval |
|-----------|-------------------------------------------------------------------------|---------------|
| 🟢 Read   | List/view issues, MRs, branches, labels, project info, pipelines       | Not required  |
| 🔴 Mutate | Create/edit/close issues, create/merge MRs, create/delete branches     | **Required**  |

All mutating operations **require explicit user approval** before execution.

## Prerequisites

### 1. Install `glab` CLI

| Platform | Command |
|----------|---------|
| Windows  | `choco install glab` (via [Chocolatey](https://chocolatey.org/)) |
| macOS    | `brew install glab` |
| Linux    | `apt install glab` or see [glab installation docs](https://gitlab.com/gitlab-org/cli#installation) |

### 2. Authenticate

**Recommended — interactive login:**

```bash
glab auth login --hostname <your-gitlab-host>
```

**Alternative — Personal Access Token:**

```bash
# Windows (cmd)
set GITLAB_TOKEN=glpat-xxxxxxxxxxxxxxxxxxxx

# PowerShell
$env:GITLAB_TOKEN = "glpat-xxxxxxxxxxxxxxxxxxxx"

# Linux / macOS
export GITLAB_TOKEN=glpat-xxxxxxxxxxxxxxxxxxxx
```

The token needs `api` scope. See [GitLab PAT documentation](https://docs.gitlab.com/ee/user/profile/personal_access_tokens.html).

**Verify authentication:**

```bash
glab auth status --hostname <your-gitlab-host>
```

## Configuration

1. Copy the example config:
   ```bash
   cp .agents/skills/gitlab-tools/env.config.example .agents/skills/gitlab-tools/env.config
   ```

2. Edit `env.config` with your GitLab details:
   ```ini
   GITLAB_HOST=gitlab.example.com
   GITLAB_PROJECT=my-group/my-project
   ```

The agent reads `env.config` to construct `glab` commands with the correct `--repo` flag.

## Usage

The main skill definition lives in [`SKILL.md`](SKILL.md). Compatible coding agents load this file to learn how to use the `glab` CLI.

### Quick Examples

List open issues:
```bash
glab issue list --repo gitlab.example.com/my-group/my-project --state opened --output json
```

View a specific issue:
```bash
glab issue view 42 --repo gitlab.example.com/my-group/my-project --output json
```

List merge requests:
```bash
glab mr list --repo gitlab.example.com/my-group/my-project --output json
```

View project info:
```bash
glab repo view --repo gitlab.example.com/my-group/my-project --output json
```

See [`SKILL.md`](SKILL.md) for the full command reference.

## Installation in Your Project

### For Roo Code

Copy the skill directory into your project:

```bash
cp -r .agents/skills/gitlab-tools /path/to/your-project/.agents/skills/
```

### For GitHub Copilot

Reference the SKILL.md content in your `.github/copilot-instructions.md` or include the skill directory in your workspace.

### For Claude Code

Include the SKILL.md content in your `CLAUDE.md` project instructions or reference it from your project's configuration.

## File Structure

```
.agents/skills/gitlab-tools/
├── SKILL.md                # Main skill definition (agent-facing instructions)
├── README.md               # This file (human-facing documentation)
├── env.config.example      # Example configuration template (copy to env.config)
└── env.config              # Your local config (gitignored — never committed)
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `glab` not found | Install it using the platform-specific command above |
| Authentication failed | Run `glab auth login --hostname <host>` or set `GITLAB_TOKEN` |
| Project not found | Verify `GITLAB_PROJECT` in `env.config` uses `namespace/project` format |
| Permission denied | Ensure your token has `api` scope |
| Rate limited | Wait and retry, or reduce request frequency |

## References

- [glab CLI documentation](https://gitlab.com/gitlab-org/cli)
- [GitLab REST API](https://docs.gitlab.com/ee/api/)
- [Personal Access Tokens](https://docs.gitlab.com/ee/user/profile/personal_access_tokens.html)
