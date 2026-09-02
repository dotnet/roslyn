---
name: "Build Failure Analysis"
description: >-
  When the Azure Pipelines PR build (`roslyn-CI`) fails, downloads the binary
  logs from its failed or canceled jobs — it does NOT rebuild — and delegates
  to the `build-failure-analyst` agent, which queries the binlogs live via the
  containerized `binlog-mcp` MCP server to identify root causes, post a PR
  comment summarizing them, and attach inline `suggestion` blocks tied to the
  diff.

# Advisory, not gating, and it never builds. Roslyn's authoritative PR build
# runs on Azure DevOps (dnceng-public/public, pipeline "roslyn-CI", definition
# 95) and publishes each job's binary logs as a build-log artifact. When that
# build's GitHub check reports failure, this workflow selects the artifacts of
# the failed jobs, downloads them anonymously (the project is public) and lets
# the agent read them.
#
# TRUST MODEL. Everything downstream of the download — archive entries, binlog
# contents, and therefore the agent's own input — is attacker-controlled on any
# PR. The workflow does not try to make that content trustworthy; it contains
# the blast radius instead:
#   * no PR code is built or executed (gh-aw checks out the repo to load its own
#     agent configuration, at the event ref, never the PR head);
#   * extraction bounds only where bytes land and how many, in one script;
#   * the agent job holds no write permission and cannot post anything;
#   * writes happen in a separate safe-outputs job, restricted to a fixed set of
#     schema-validated outputs aimed at the PR from the trigger event.

on:
  # `check_run` fires for every check on a commit, so `fetch-binlog` filters
  # tightly to the `roslyn-CI` check reporting failure.
  check_run:
    types: [completed]
  # Run for every failing PR, including external contributors' — the most likely
  # to break the build. gh-aw's default author-association gate would skip them
  # (and on `check_run` the actor is the pipeline app anyway). Safe because the
  # agent only reads artifacts and cannot write.
  roles: all
  # Manual entry point for reruns and testing.
  workflow_dispatch:
    inputs:
      ado-build-id:
        description: "Azure DevOps build id to analyze (dnceng-public/public)."
        required: true
        type: string
      pr-number:
        description: "PR number to post the analysis on."
        required: true
        type: string
  # Gate the whole AI pipeline on the fetch job so the agent only runs when a
  # binlog was actually retrieved.
  needs: [fetch-binlog]

# When `check_run` fires for an unrelated or passing check, `fetch-binlog` is
# skipped, its output is empty, and the agent is skipped too — so no AI call
# happens on anything but a real `roslyn-CI` failure on an in-scope PR.
if: needs.fetch-binlog.outputs.binlog-found == 'true'

# The agent runs read-only and posts nothing itself. gh-aw compiles all PR
# writes into a separate `safe_outputs` job that holds the write scope, so this
# must stay at `read` — raising it would hand PR-write scope to the job that
# reads untrusted binlogs.
#
# Do NOT add `copilot-requests: write`: it switches the generated lock from
# `secrets.COPILOT_GITHUB_TOKEN` to `github.token`, which is not entitled for
# inference against api.githubcopilot.com in this org, and every run then fails
# with HTTP 403 before it reads the prompt.
permissions:
  contents: read
  pull-requests: read

concurrency:
  # Real `roslyn-CI` failures and manual dispatches share a PR-scoped group so a
  # newer analysis supersedes a running one. Every other completed check_run on
  # the PR gets a unique group, so cancel-in-progress can't abort a real
  # analysis.
  group: ${{ (github.event_name == 'check_run' && github.event.check_run.name == 'roslyn-CI' && format('build-failure-analysis-{0}', github.event.check_run.pull_requests[0].number || github.event.check_run.head_sha)) || (github.event_name == 'workflow_dispatch' && format('build-failure-analysis-{0}', inputs['pr-number'])) || format('build-failure-analysis-run-{0}', github.run_id) }}
  cancel-in-progress: true

timeout-minutes: 30

network:
  allowed:
    - defaults
    - dotnet

imports:
  - shared/build-failure-analysis-shared.md

engine: copilot

# Live binlog access for the agent. fetch-binlog uploads the binlogs, the agent
# job downloads them to /tmp/binlogs, and the gh-aw MCP gateway mounts that
# read-only into this container at /data/binlogs.
#
# The digest is pinned in `.github/aw/actions-lock.json` because this container
# parses artifacts from untrusted PRs. Inspect the current digest with:
#   docker buildx imagetools inspect \
#     mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64
mcp-servers:
  binlog-mcp:
    container: "mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64"
    mounts:
      - "/tmp/binlogs:/data/binlogs:ro"
    allowed: ["*"]

# Reuses the binlogs from the failed Azure DevOps build instead of rebuilding.
# The logic lives in scripts/fetch-build-binlogs.cs so it can be read, reviewed
# and run outside the workflow.
jobs:
  fetch-binlog:
    name: Fetch binlogs (Azure Pipelines)
    runs-on: ubuntu-latest
    timeout-minutes: 15
    # `check_run` fires for every check; only act on the Roslyn PR build check
    # reporting failure (or a manual dispatch).
    if: >
      github.event_name == 'workflow_dispatch' ||
      (github.event.check_run.name == 'roslyn-CI' && github.event.check_run.conclusion == 'failure')
    permissions:
      contents: read
      pull-requests: read
    outputs:
      binlog-found: ${{ steps.fetch.outputs.binlog-found }}
      pr-number: ${{ steps.fetch.outputs.pr-number }}
      pr-head-sha: ${{ steps.fetch.outputs.pr-head-sha }}
      pr-merge-sha: ${{ steps.fetch.outputs.pr-merge-sha }}
      ado-build-id: ${{ steps.fetch.outputs.ado-build-id }}
      ado-build-url: ${{ steps.fetch.outputs.ado-build-url }}
    steps:
      # Checks out this workflow's own scripts at the event ref, never the PR
      # head, so no PR-authored code is fetched or run.
      - name: Check out analysis scripts
        uses: actions/checkout@v7.0.1
        with:
          sparse-checkout: .github/workflows/scripts
          persist-credentials: false

      - name: Download binlogs from the failed Azure Pipelines build
        id: fetch
        shell: bash
        # One wall-clock bound for the whole fetch, applied here rather than
        # tracked inside the script. `timeout` kills it at 10 minutes, the step
        # fails, `binlog-found` is never written, and the step below turns that
        # into a warning instead of a red job. (gh aw strips step-level
        # `timeout-minutes` from custom steps, so the bound goes on the command.)
        continue-on-error: true
        env:
          GH_TOKEN: ${{ github.token }}
          GH_AW_REPO: ${{ github.repository }}
          ADO_API: "https://dev.azure.com/dnceng-public/public/_apis"
          ADO_BUILD_UI: "https://dev.azure.com/dnceng-public/public/_build/results"
          # roslyn-CI pipeline definition id in dnceng-public/public.
          ADO_BUILD_DEFINITION_ID: "95"
          RESOLVE_MODE: ${{ github.event_name == 'workflow_dispatch' && 'dispatch' || 'check_run' }}
          # Event-owned, and the same value safe outputs are bound to. Empty for
          # fork PRs, which the script then resolves from CHECK_HEAD_SHA.
          PR_NUMBER: ${{ github.event.check_run.pull_requests[0].number || inputs['pr-number'] }}
          CHECK_HEAD_SHA: ${{ github.event.check_run.head_sha }}
          CHECK_DETAILS_URL: ${{ github.event.check_run.details_url }}
          DISPATCH_BUILD_ID: ${{ inputs['ado-build-id'] }}
          BINLOG_DIR: /tmp/binlogs
          SCRIPT_DIR: ${{ github.workspace }}/.github/workflows/scripts
        # `dotnet run` evaluates the fetcher as an MSBuild project, so it picks
        # up the ambient configuration around it. Cone-mode sparse checkout
        # materializes every root-level file, so the work tree holds roslyn's
        # `global.json` (which pins an SDK the runner does not have) and
        # `Directory.Build.props` (whose Arcade import is not checked out). The
        # two are found from different roots: the SDK by walking up from the
        # working directory, the MSBuild imports by walking up from the file. So
        # run from a directory outside the tree and switch the inherited imports
        # off; the fetcher itself stays in the repository.
        run: cd "${RUNNER_TEMP:-/tmp}" && timeout 600 dotnet run "${SCRIPT_DIR}/fetch-build-binlogs.cs" -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false -p:ImportDirectoryPackagesProps=false

      # A fetch that was killed or errored leaves `binlog-found` unset, so the
      # activation gate already declines to analyze. Say so in the log rather
      # than failing the run: a build we could not read is not a build failure.
      - name: Report an incomplete fetch
        if: steps.fetch.outcome != 'success'
        run: echo "::warning::Binlog fetch did not complete (${{ steps.fetch.outcome }}); skipping analysis for this build."

      - name: Upload analysis artifact
        if: steps.fetch.outputs.binlog-found == 'true'
        uses: actions/upload-artifact@v7.0.1
        with:
          name: build-failure-analysis-data
          path: /tmp/binlogs
          if-no-files-found: warn
          retention-days: 1

# Agent-job steps. The top-level `if:` already gates activation on
# `binlog-found`, so these only run once binlogs were retrieved.
steps:
  - name: Download analysis artifact
    uses: actions/download-artifact@v8.0.1
    with:
      name: build-failure-analysis-data
      path: /tmp/binlogs

  - name: Export agent context
    shell: bash
    env:
      GH_AW_BINLOG_FOUND_VALUE: ${{ needs.fetch-binlog.outputs.binlog-found }}
      GH_AW_PR_NUMBER_VALUE: ${{ needs.fetch-binlog.outputs.pr-number }}
      GH_AW_PR_HEAD_SHA_VALUE: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
      GH_AW_PR_MERGE_SHA_VALUE: ${{ needs.fetch-binlog.outputs.pr-merge-sha }}
      GH_AW_ADO_BUILD_URL_VALUE: ${{ needs.fetch-binlog.outputs.ado-build-url }}
      GH_AW_GITHUB_WORKSPACE: ${{ github.workspace }}
    run: |
      # The binlogs are mounted into the binlog-mcp container at
      # `/data/binlogs`. Build the list of in-container binlog paths (one per
      # selected artifact) that the agent should query. `GH_AW_BINLOG_PATH` is
      # the first entry for tools/prompts that expect a single path.
      BINLOG_DIR="/data/binlogs"
      LIST=""
      if [ "${GH_AW_BINLOG_FOUND_VALUE:-false}" = "true" ] && [ -d /tmp/binlogs ]; then
        for f in /tmp/binlogs/*.binlog; do
          [ -f "$f" ] || continue
          LIST="${LIST}${BINLOG_DIR}/$(basename "$f")"$'\n'
        done
      fi
      # `shell: bash` puts this step under `-eo pipefail`, so take the first
      # entry with a parameter expansion instead of `printf | head -1`: a pipe
      # whose reader exits early would raise SIGPIPE and abort the step.
      FIRST=${LIST%%$'\n'*}
      {
        echo "GH_AW_BUILD_OUTCOME=failure"
        echo "GH_AW_BINLOG_DIR=${BINLOG_DIR}"
        echo "GH_AW_BINLOG_PATH=${FIRST}"
        echo "GH_AW_BINLOG_HOST_PATH=${GH_AW_ADO_BUILD_URL_VALUE}"
        echo "GH_AW_PR_NUMBER=${GH_AW_PR_NUMBER_VALUE}"
        echo "GH_AW_PR_HEAD_SHA=${GH_AW_PR_HEAD_SHA_VALUE}"
        echo "GH_AW_PR_MERGE_SHA=${GH_AW_PR_MERGE_SHA_VALUE}"
        echo "GH_AW_WORKSPACE=${GH_AW_GITHUB_WORKSPACE}"
        echo "GH_AW_BINLOG_LIST<<GH_AW_EOF"
        printf '%s' "$LIST"
        echo "GH_AW_EOF"
      } >> "$GITHUB_ENV"

tools:
  github:
    toolsets: [pull_requests, repos]
  bash:
    - "cat"
    - "head"
    - "tail"
    - "grep"
    - "wc"
    - "sort"
    - "uniq"
    - "ls"
    - "find"
    # binlog-mcp is also mounted as a CLI wrapper (…/mcp-cli/bin/binlog-mcp);
    # allow it so the agent can query the binlogs via the wrapper when it does
    # not call the MCP tool natively.
    - "binlog-mcp:*"

safe-outputs:
  needs: [fetch-binlog]
  steps:
    - name: Revalidate PR revision before applying queued outputs
      shell: bash
      env:
        GH_TOKEN: ${{ github.token }}
        GH_AW_REPO: ${{ github.repository }}
        PR_NUMBER: ${{ needs.fetch-binlog.outputs.pr-number }}
        EXPECTED_HEAD: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
        EXPECTED_MERGE: ${{ needs.fetch-binlog.outputs.pr-merge-sha }}
      run: |
        set -euo pipefail
        if [ -z "${EXPECTED_HEAD}" ] || [ -z "${EXPECTED_MERGE}" ] ||
           ! gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" |
             jq -e --arg head "${EXPECTED_HEAD}" --arg merge "${EXPECTED_MERGE}" \
               '.head.sha == $head and .merge_commit_sha == $merge' >/dev/null; then
          echo "::error::PR #${PR_NUMBER} moved or could not be verified before applying queued build-analysis outputs."
          exit 1
        fi
  messages:
    footer: "> 🤖 **Automated content by GitHub Copilot.** Generated by the [{workflow_name}]({agentic_workflow_url}) workflow.{ai_credits_suffix} · [◷]({history_link})"
  data:
    type: object
    properties:
      workflow_artifact:
        type: string
        enum: [build-failure-analysis]
      artifact_kind:
        type: string
        enum: [analysis]
    required: [workflow_artifact, artifact_kind]
    additionalProperties: false
  # Bind writes to the PR number the fetch job resolved and validated, rather
  # than allowing untrusted binlog/source content to choose an arbitrary
  # repository target. `check_run.pull_requests` is empty for fork PRs, so the
  # raw event expression would leave the target blank on exactly the PRs the
  # fetch job now resolves via the head SHA. That job verifies the number is
  # numeric, that the ADO build's sourceBranch is `refs/pull/<n>/merge`, and
  # that the head and merge revisions still match before the agent can run, so
  # this is no less trusted than the trigger and is correct more often.
  report-failure-as-issue: false
  add-comment:
    max: 1
    target: ${{ needs.fetch-binlog.outputs.pr-number }}
    hide-older-comments: true
  create-pull-request-review-comment:
    max: 25
    target: ${{ needs.fetch-binlog.outputs.pr-number }}
    commit-id: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
  noop:
    max: 1
    report-as-issue: false
---

<!--
  Body provided by shared/build-failure-analysis-shared.md.

  All build-failure analysis expertise (binlog parsing, error grouping,
  suggestion authoring) lives in the reusable agent at
  .github/agents/build-failure-analyst.agent.md.
-->
