---
name: AI Artifact Audit
description: Audit Roslyn's AI guidance for broken, duplicated, contradictory, or stale instructions.

on:
  schedule: weekly on monday
  workflow_dispatch:

strict: true

permissions:
  contents: read

network:
  allowed: [defaults]

engine: copilot
model: gpt-5-mini

safe-outputs:
  mentions: false
  allowed-github-references: []
  noop:
    report-as-issue: false
  create-issue:
    title-prefix: "[AI artifact audit] "
    max: 1
    close-older-issues: true
    deduplicate-by-title: true
    expires: false
---

# Weekly AI artifact hygiene audit

Audit only the AI guidance in these repository paths:

- `.github/copilot-instructions.md`
- `.github/instructions/`
- `.github/memory/`
- `.github/skills/`

Look for:

1. Broken relative links and references to files or commands that do not exist.
2. Duplicated instructions that could drift independently.
3. Contradictory instructions where following one rule would violate another.
4. Stale claims that conflict with the current repository structure or contents.

For every finding:

- Cite the exact file path and quote the relevant text.
- Cite the repository evidence showing why the finding is actionable.
- Explain the smallest correction that would resolve it.
- Do not report subjective style preferences or claims that cannot be verified from
  the repository.

If there are actionable findings, create one concise issue grouped by severity.
Use a stable title so repeated runs update the existing report rather than creating
noise. Use `create-issue` for this visible output.

If there are no actionable findings, call `noop` with a short explanation that the
current repository guidance was audited and no issue is required.

Never modify repository contents or create a pull request.
