---
name: update-agent-docs
description: >
  Assess whether task-relevant agent documentation needs changes in the Roslyn repo. Run at the end of
  every task that modifies code, adds files, changes public APIs or diagnostics, or establishes new
  patterns. Keeps .github/memory/ fresh and reliable.
---

# Update Agent Docs

Run at the end of every task that changes code. The assessment is mandatory; a documentation
edit is not. No documentation change is a valid outcome.

## Edit Criteria

Edit docs only when the change makes existing guidance incorrect or leaves a necessary
contributor-facing workflow, contract, or architectural decision undocumented. Before editing,
identify the specific incorrect statement or missing information and who needs it.

Do not add guidance merely to record a fix, repeat an existing rule, describe an implementation
detail, or remind contributors about requirements already enforced by configuration, analyzers,
tests, or CI. Prefer links to authoritative configuration or tooling over duplicated rules.
Document automated checks only when contributors need otherwise-undocumented instructions
to run them or act on failures.

Correct inaccuracies encountered in task-relevant docs; do not expand the task into a general
freshness audit. Apply these criteria before making documentation changes in the checklist below.
Required API, diagnostic, and resource updates remain required.

When adding or changing a workaround for a known issue, document why it is needed inline at the
workaround. Do not rely on or update the repo knowledge base as the explanation for a code-specific workaround. Include a link to the issue when possible.

## Checklist

**Memory file added, removed, renamed, or had its purpose change?** → Update `.github/memory/INDEX.md` and any memory files that reference it.

**Public API changed?** → Update the owning project's `PublicAPI.Unshipped.txt` (RS0016 enforces this). `API_MAP.md` covers only repo-wide entry points.

**New compiler error code, IDE diagnostic ID, or resource string added?** → Ensure `ErrorCode.cs` / `IDEDiagnosticIds.cs` / `.resx` (+ `/t:UpdateXlf`) are consistent.

**Necessary guidance for a new pattern missing?** → If it meets the edit criteria, document it in `.github/memory/CONVENTIONS.md` for repo-wide guidance or the matching `.github/instructions/<area>.instructions.md` for layer-specific guidance.

**Surprising or undocumented behavior found?** → Assess whether it meets the edit criteria. Ask the user only when a necessary documentation decision cannot be made confidently.

**Changed test base classes, locations, or how to run a suite?** → Correct affected guidance or fill a necessary gap in `.github/memory/TESTING_STRATEGY.md` for repo-wide layout or `.github/memory/testing/<area>.md` for layer-specific bases/conventions.

**Any doc updated?** → No additional tracking needed. Git history tracks changes automatically.

## Documentation Quality

Documentation must describe the repository's current state, not the history or mechanics of the
change being made. Do not add guidance that only makes sense in the context of the current diff,
mentions behavior removed by the task, or warns against a workaround that no longer exists. Before
adding guidance, ask whether it would help a future contributor starting from a clean checkout; if
not, leave it out.

## Creating New Doc Files

If necessary guidance meets the edit criteria and doesn't fit existing files:
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
