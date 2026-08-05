---
coverage: Index and loading map for all .github/memory/ knowledge-base files
---

# Memory Index

This is the loading map for the agent knowledge base under `.github/memory/`. **Read this file first** when starting any task. Load other files **on demand** — and read only the path-scoped **instruction file** for the area you're working in (see below), not all of them. This keeps context small.

## Repo-wide files (load as the task needs)

| File | Purpose | When to load |
|------|---------|--------------|
| **`INDEX.md`** (this file) | Discovery map for the knowledge base | Always — read first |
| **`ARCHITECTURE.md`** | Layered system overview & data flow (one row per layer) | Always for non-trivial tasks |
| **`CONVENTIONS.md`** | Repo-wide code style, naming, immutability, resource & public-API rules | When writing or reviewing code |
| **`FILE_MAP.md`** | Top-level `src/` map (one line per area) + layer pointers | When deciding which area/layer to work in |
| **`API_MAP.md`** | Build/test entry points & PublicAPI tracking | When changing build, tests, or public APIs |
| **`KNOWN_ISSUES.md`** | Repo-wide / cross-cutting quirks & workarounds | Always for code review; unfamiliar areas |
| **`TESTING_STRATEGY.md`** | Test layout, shared authoring conventions & how to run tests | When writing tests or debugging test failures |

## Layer-specific knowledge

Per-layer directory detail, key files/APIs, and coding conventions live in the
path-scoped **instruction files**, which auto-apply when you edit `.cs`/`.vb`
under their glob. **Known issues** and **testing** are broken out into dedicated
per-layer memory files so you load only what the task needs. Read the row for the
area you're working in:

| Layer (src areas) | Instruction file (rules + dir detail) | Known issues | Testing |
|-------------------|---------------------------------------|--------------|---------|
| `Compilers`, `Dependencies`, `ExpressionEvaluator`, `Tools` | `.github/instructions/Compiler.instructions.md` | `known-issues/compiler.md` | `testing/compiler.md` |
| `Analyzers`, `CodeStyle`, `Features`, `Workspaces`, `EditorFeatures`, `VisualStudio`, `LanguageServer` | `.github/instructions/IDE.instructions.md` | `known-issues/ide.md` | `testing/ide.md` |
| `Razor` | `.github/instructions/Razor.instructions.md` | `known-issues/razor.md` | `testing/razor.md` |

The repo-wide memory files above hold only cross-cutting content and point into
these layer files for specifics.

<!-- Add rows here as new memory files are created -->

## Related Existing Docs

This repo predates this knowledge base and has authoritative docs the memory files cross-reference:

- `AGENTS.md` — thin root pointer to `.github/copilot-instructions.md` (the canonical repo-wide guidance).
- `.github/instructions/{Compiler,IDE,Razor}.instructions.md` — path-scoped layer rules **and** knowledge, applied automatically by area.
- `.github/skills/*/SKILL.md` — task-specific skills (code-review, ci-analysis, snap, merge-into-branch, update-agent-docs, etc.).
- `docs/` — deep-dive docs (`docs/wiki/Roslyn-Overview.md`, `docs/Layering.md`, `docs/area-owners.md`).

## Conventions

- **Treat memory files as authoritative for repo conventions, but cross-check against actual code** — they can drift.
- **Prefer small focused files over large monolithic ones.** Keep layer-specific detail in the matching `.github/instructions/<area>.instructions.md`, not in the repo-wide memory files.
- **New files should have minimal frontmatter** with a `coverage:` field describing what the file covers.

## Maintenance

When you change the knowledge base, keep this index in sync:

- **Added, removed, or renamed a memory file?** → Update this index.
- **Significantly changed a file's purpose or scope?** → Update its row in the loading map.
- **Added layer-specific knowledge?** → Known issues go in `known-issues/<area>.md`; test conventions go in `testing/<area>.md`; directory detail, key files/APIs, and coding conventions go in the matching `.github/instructions/<area>.instructions.md`. Keep it out of the repo-wide memory files.
