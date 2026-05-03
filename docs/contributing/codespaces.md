# Codespaces

This repository provides a GitHub Codespaces configuration for full-repository Roslyn work.

## Available configuration

| Configuration | Default solution | Intended usage | Minimum machine |
| --- | --- | --- | --- |
| `Roslyn (.NET 10)` | `Roslyn.slnx` | Full-repository work | 8 cores, 16 GB RAM, 32 GB storage |

The configuration uses `.devcontainer/Dockerfile`, which starts from the .NET 10 SDK image and then installs the exact SDK pinned by `global.json`.

## Lifecycle choices

The repo uses `.devcontainer/restore-workspace.sh <solution>` in `updateContentCommand`.

That placement is intentional:

- `updateContentCommand` runs during Codespaces prebuild creation.
- `postCreateCommand` does not run during prebuild creation.
- Keeping restore in `updateContentCommand` allows GitHub Codespaces to cache the expensive first-run restore step.

The configuration also installs `gh` and `pwsh` through dev container features so the container matches common Roslyn command-line workflows more closely.

## Recommended repository-level GitHub settings

These settings live in the repository's **Settings > Codespaces** page rather than in the repo itself.

1. Enable a prebuild for the default branch using `.devcontainer/devcontainer.json`.
2. Start with the **On configuration change** trigger to keep storage and Actions usage under control.
3. Keep retained prebuild versions at `2` unless you have a clear need for a longer rollback window.
4. Limit prebuild regions to the regions your team actually uses.

## Validation checklist

After changing Codespaces settings or the devcontainer configuration:

1. Create a fresh codespace from `.devcontainer/devcontainer.json`.
2. Confirm the expected default solution loads in VS Code.
3. Confirm `gh --version` and `pwsh --version` work in the terminal.
4. Confirm the restore step completes successfully for the selected solution.
