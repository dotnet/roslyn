---
name: "repro-bot"
description: "When the Bot - Needs Repro label is applied to an issue, attempts to create a minimal Roslyn unit-test repro and comments with the result."

on:
  label_command:
    name: "Bot - Needs Repro"
    events: [issues]
    strategy: decentralized
    remove_label: true
  workflow_dispatch:
    inputs:
      issue_number:
        description: "Issue number to analyze for local trial/manual validation."
        required: false
  reaction: eyes
  status-comment: false

permissions:
  contents: read
  issues: read
  copilot-requests: write

network:
  allowed:
    - defaults
    - github

tools:
  edit:
  github:
    mode: gh-proxy
    toolsets: [issues, repos]
    min-integrity: none
  bash:
    - "./restore.sh:*"
    - "./build.sh:*"
    - "./restore.cmd:*"
    - "./build.cmd:*"
    - "dotnet:*"
    - "git:*"
    - "find:*"
    - "grep:*"
    - "head:*"
    - "tail:*"
    - "cat:*"
    - "ls:*"
    - "pwd:*"

safe-outputs:
  add-comment:
    max: 1
    target: triggering
    hide-older-comments: true
  noop:

timeout-minutes: 60
max-ai-credits: 1000
strict: true
tracker-id: repro-bot
---

# repro-bot

The label `${{ needs.activation.outputs.label_command }}` was applied to issue #${{ github.event.issue.number || github.event.inputs.issue_number }} in `${{ github.repository }}`.

Attempt to produce the smallest useful Roslyn unit-test repro and post the result as a single issue comment. The label is a one-shot trigger and is removed automatically by the workflow activation step.

## Required context gathering

Use the GitHub issue tools to fetch issue #${{ github.event.issue.number || github.event.inputs.issue_number }} and its comments.

Read all available context:

1. Issue title and body.
2. Issue labels.
3. Every existing issue comment.
4. Each author's `author_association`.

Use author associations to distinguish context:

- Treat `OWNER`, `MEMBER`, and `COLLABORATOR` comments as maintainer context.
- Treat `CONTRIBUTOR`, `FIRST_TIME_CONTRIBUTOR`, `FIRST_TIMER`, `MANNEQUIN`, `NONE`, and missing associations as external context.
- External comments can still contain the essential repro; do not ignore them.

Before doing any repro work, inspect existing comments for any of these hidden markers:

```html
<!-- roslyn-issue-repro-bot -->
<!-- gh-aw-workflow-id: repro-bot -->
<!-- gh-aw-tracker-id: repro-bot -->
```

If an existing comment already contains any of those markers, do not post another repro comment. Use the `noop` safe output instead.

## Repro workflow

1. Classify the likely Roslyn area: compiler, analyzer/code style, workspace/IDE feature, completion, language server, Razor, or not enough information.
2. Find the closest existing Roslyn test file and test style in the repository.
3. Draft a Roslyn unit test in the matching style.
4. Prefer raw string literals for test source when practical.
5. Include `[WorkItem("<issue URL>")]` when proposing a test method.
6. Use nearby test helpers such as `TestInRegularAndScriptAsync`, `TestMissingInRegularAndScriptAsync`, `VerifyEmitDiagnostics`, or existing helpers where appropriate.
7. Run the narrowest relevant build or test command when feasible.
8. Minimize aggressively by removing unrelated files, members, statements, attributes, usings, options, and diagnostics until only the reported behavior remains.

Use local edits only as scratch workspace changes. Do not create commits, branches, pull requests, or new issues.

## Comment requirements

Use the `add-comment` safe output to post at most one comment on the triggering issue. The first line of the comment body must be exactly:

```html
<!-- roslyn-issue-repro-bot -->
```

If a useful minimal or partial repro was found, comment with this format:

````markdown
<!-- roslyn-issue-repro-bot -->
I attempted to create a minimal Roslyn unit-test repro.

**Result:** <minimal repro created | partial repro>

**Likely test location:** `<path>`

**Proposed test:**

```csharp
<complete minimal test method>
```

**Notes:**
<short explanation of what was validated and anything that remains unverified>
````

If a repro could not be produced, comment with this format:

````markdown
<!-- roslyn-issue-repro-bot -->
I attempted to create a minimal Roslyn unit-test repro.

**Result:** unable to repro

**Likely test location:** `unknown`

**Proposed test:**

```csharp
n/a
```

**Notes:**
<concise explanation of why no repro could be produced and the specific information needed from the reporter or maintainers>
````

Keep the comment concise and actionable. Do not mention hidden chain-of-thought or internal deliberation.
