# Roslyn (.NET Compiler Platform) — AI Agent Instructions

This file is intentionally thin. The canonical, repo-wide agent guidance lives in
**[`.github/copilot-instructions.md`](.github/copilot-instructions.md)** — read it first.
It covers the project overview, build/test entry points, global code style, the
memory-first orientation protocol, and the doc-update obligation.

## Where to find things

- **Repo-wide rules & orientation:** [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
- **Knowledge base (load on demand):** [`.github/memory/INDEX.md`](.github/memory/INDEX.md) — the loading map; start here for architecture, conventions, file map, APIs, known issues, and testing.
- **Area-specific rules (auto-applied by path):** [`.github/instructions/`](.github/instructions/)
  - [`Compiler.instructions.md`](.github/instructions/Compiler.instructions.md) — `src/{Compilers,Dependencies,ExpressionEvaluator,Tools}`
  - [`IDE.instructions.md`](.github/instructions/IDE.instructions.md) — `src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio,LanguageServer}`
  - [`Razor.instructions.md`](.github/instructions/Razor.instructions.md) — `src/Razor`
- **Task-specific skills:** [`.github/skills/`](.github/skills/) (auto-discovered; e.g. `code-review`, `ci-analysis`, `update-agent-docs`).

## Orientation protocol

1. Read [`.github/copilot-instructions.md`](.github/copilot-instructions.md).
2. Read [`.github/memory/INDEX.md`](.github/memory/INDEX.md) and load only the memory files relevant to your task.
3. The path-scoped instruction file for the area you're editing applies automatically — follow it.
4. After changing code, run the `update-agent-docs` skill to keep `.github/memory/` fresh.
