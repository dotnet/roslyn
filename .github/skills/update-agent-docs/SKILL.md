---
name: update-agent-docs
description: >
  Update the agent knowledge base after making code changes in the Roslyn repo. Run at the end of
  every task that modifies code, adds files, changes public APIs or diagnostics, or establishes new
  patterns. Keeps .github/memory/ fresh and reliable.
---

# Update Agent Docs

Run at the end of every task that changes code. This is not optional.

## Checklist

**Files added or moved?** → Update `.github/memory/FILE_MAP.md` (top-level area) and the matching `.github/instructions/<area>.instructions.md` (directory detail).

**Memory file added, removed, renamed, or had its purpose change?** → Update `.github/memory/INDEX.md` and any memory files that reference it.

**Public API changed?** → Update the matching `.github/instructions/<area>.instructions.md` (Compiler/IDE) and the owning project's `PublicAPI.Unshipped.txt` (RS0016 enforces this). `API_MAP.md` covers only repo-wide entry points.

**New compiler error code, IDE diagnostic ID, or resource string added?** → Reflect it in the matching `.github/instructions/<area>.instructions.md`; ensure `ErrorCode.cs` / `IDEDiagnosticIds.cs` / `.resx` (+ `/t:UpdateXlf`) are consistent.

**New pattern established?** → If repo-wide, add to `.github/memory/CONVENTIONS.md`; if layer-specific, add to the matching `.github/instructions/<area>.instructions.md`.

**Surprising or undocumented behavior found?** → Repo-wide → `.github/memory/KNOWN_ISSUES.md`; layer-specific → `.github/memory/known-issues/<area>.md`.

**Changed test base classes, locations, or how to run a suite?** → Repo-wide layout → `.github/memory/TESTING_STRATEGY.md`; layer-specific bases/conventions → `.github/memory/testing/<area>.md`.

**Any doc updated?** → No additional tracking needed. Git history tracks changes automatically.

## Creating New Doc Files

If knowledge doesn't fit existing files:
- Create a new file in `.github/memory/` with a descriptive name (e.g., `incremental-generators.md`, not `misc.md`).
- Add YAML frontmatter with a `coverage` field describing what it covers.
- Add a row to `.github/memory/INDEX.md`.

You do not need permission to create new files in `.github/memory/`. This space is yours to evolve.

## Frontmatter Format

New docs should have minimal frontmatter — only the `coverage` field:

```yaml
---
coverage: Brief description of what this doc covers
---
```

Do NOT add `last_updated`, `updated_by`, `confidence`, or date fields. Git history provides this without creating merge conflicts.
