---
name: "Build Failure Analysis (command)"
description: >-
  Rerun the build-failure analysis on a pull request when a maintainer comments
  `/analyze-build-failure`. Shares its body and its fetch script with
  `build-failure-analysis.agent.md` and likewise never rebuilds: it inspects the
  PR's latest Azure Pipelines `roslyn-CI` build and, only when that build has
  failed, downloads the binary logs of its failed or canceled jobs and delegates
  to the `build-failure-analyst` agent. Useful when a previous run was cancelled
  or the analysis comment was dismissed.

on:
  slash_command:
    name: analyze-build-failure
    events: [pull_request_comment]
  roles: [admin, maintainer, write]
  reaction: "eyes"
  # Gate the AI pipeline on the fetch job so the agent only runs when a binlog
  # was actually retrieved from a failed Azure DevOps build.
  needs: [fetch-binlog]

# Skip activation (and the agent) unless a binlog was retrieved — e.g. if the
# PR's latest Azure DevOps build did not fail, or the PR is out of scope.
if: needs.fetch-binlog.outputs.binlog-found == 'true'

# The agent runs read-only and posts nothing itself. gh-aw compiles all PR
# writes into a separate `safe_outputs` job that holds the write scope, so this
# must stay at `read`. (The command's acknowledgement reaction is likewise
# emitted by gh-aw's own job, not by the agent.)
#
# Do NOT add `copilot-requests: write`: it switches the generated lock from
# `secrets.COPILOT_GITHUB_TOKEN` to `github.token`, which is not entitled for
# inference against api.githubcopilot.com in this org, and every run then fails
# with HTTP 403 before it reads the prompt.
permissions:
  contents: read
  pull-requests: read

concurrency:
  # Distinct from the automatic workflow's group (`build-failure-analysis-<pr>`).
  # Concurrency groups are repository-global, so sharing the name made the two
  # workflows cancel each other for the same PR: a newly failing build would
  # kill an on-demand analysis a maintainer had just asked for. Command-like
  # invocations for a PR are serialized instead of canceling an active run.
  # Unrelated comments get a run-unique group; slash-command authorization
  # runs only after concurrency is evaluated.
  group: ${{ (github.event.comment.body == '/analyze-build-failure' || startsWith(github.event.comment.body, '/analyze-build-failure ') || startsWith(github.event.comment.body, format('/analyze-build-failure{0}', fromJSON('"\n"')))) && contains(fromJSON('["OWNER","MEMBER","COLLABORATOR"]'), github.event.comment.author_association) && format('build-failure-analysis-cmd-{0}', github.event.issue.number) || format('build-failure-analysis-cmd-run-{0}', github.run_id) }}
  cancel-in-progress: false

timeout-minutes: 30

network:
  allowed:
    - defaults
    - dotnet

imports:
  - shared/build-failure-analysis-shared.md

engine: copilot

# Live binlog access for the agent — see build-failure-analysis.agent.md for the
# trust model. The digest is pinned in `.github/aw/actions-lock.json` because
# this container parses artifacts from untrusted PRs.
mcp-servers:
  binlog-mcp:
    container: "mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64"
    mounts:
      - "/tmp/binlogs:/data/binlogs:ro"
    allowed: ["*"]

# Reuses the binlogs from the PR's most recent failed `roslyn-CI` build. Shares
# scripts/fetch-build-binlogs.sh with build-failure-analysis.agent.md, in
# `latest` resolution mode because a slash command carries no `check_run`
# payload.
jobs:
  fetch-binlog:
    name: Fetch binlogs (Azure Pipelines)
    # Cheap pre-gate. This job is a dependency of gh-aw's `pre_activation`, so
    # it runs BEFORE the authoritative role and command-position check; without
    # a guard any commenter could make it download hundreds of MB on every
    # comment. It is deliberately an over-approximation — `contains()` is the
    # only substring test available in an `if:` — and the first step of the job
    # narrows it to gh-aw's real rules before anything is downloaded.
    #
    # KEEP IN SYNC with `roles:` above: the author_association list here and the
    # permission step below restate that policy by hand, because only
    # `pre_activation` is generated from the frontmatter.
    #
    # `github.event.issue.pull_request` is what keeps plain issue comments out;
    # gh-aw emits no such filter of its own despite `pull_request_comment`.
    if: >-
      github.event.repository.fork == false &&
      github.event.issue.pull_request &&
      contains(fromJSON('["OWNER","MEMBER","COLLABORATOR"]'), github.event.comment.author_association) &&
      contains(github.event.comment.body, '/analyze-build-failure')
    runs-on: ubuntu-latest
    timeout-minutes: 15
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
      # `author_association` cannot tell a read-only org member apart from a
      # maintainer, so resolve the real repository permission here, before any
      # download. KEEP IN SYNC with `roles: [admin, maintainer, write]`.
      #
      # Test `.permission`, which returns the legacy base roles
      # admin|write|read|none with maintain mapped to write — exactly "has push
      # access or better". `.role_name` is deliberately not consulted: a custom
      # org role only has to avoid the base names, so a role merely *named*
      # `maintainer` while inheriting read would pass.
      - name: Verify the comment invokes the command and the commenter has write access
        id: perm
        if: github.event_name == 'issue_comment'
        shell: bash
        env:
          GH_TOKEN: ${{ github.token }}
          COMMENTER: ${{ github.event.comment.user.login }}
          COMMENT_BODY: ${{ github.event.comment.body }}
          COMMAND_NAME: "analyze-build-failure"
        run: |
          set +e
          # --- 1. Command position (free; do this before the API call) ------
          # gh-aw trims the body and requires the command to be the FIRST token
          # (`/^\/([a-zA-Z0-9][a-zA-Z0-9._-]*)(?=$|\s)/`), but that check runs
          # after this job by construction, so reproduce it here. `awk 'NF
          # {print $1; exit}'` is the same rule, and `tr -d '\r'` is needed
          # because JS treats CR as whitespace while awk's field splitting does
          # not. KEEP IN SYNC with `on.slash_command.name`.
          first_word=$(printf '%s' "${COMMENT_BODY}" | tr -d '\r' | awk 'NF {print $1; exit}')
          if [ "${first_word}" != "/${COMMAND_NAME}" ]; then
            # Never echo the raw token: it is attacker-controlled and `::`-
            # prefixed text is interpreted by the runner as a workflow command.
            safe_word=$(printf '%s' "${first_word}" | tr -cd 'A-Za-z0-9/._-' | cut -c1-40)
            echo "Comment does not start with '/${COMMAND_NAME}' (first token: '${safe_word}'); skipping the binlog download."
            echo "authorized=false" >> "$GITHUB_OUTPUT"
            exit 0
          fi
          # --- 2. Repository permission -------------------------------------
          # `COMMENTER` reaches an API path and the log, so require the shape of
          # a real login; a bot login such as `github-actions[bot]` is rejected.
          if ! printf '%s' "${COMMENTER}" | grep -qE '^[A-Za-z0-9-]+$'; then
            echo "::warning::Commenter login is missing or malformed; skipping the binlog download."
            echo "authorized=false" >> "$GITHUB_OUTPUT"
            exit 0
          fi
          # Extract with `jq` rather than `gh api --jq`: on a non-2xx response
          # `gh` prints the error document to stdout, which `--jq` does not
          # filter, so the raw JSON would land in `perm` and be echoed. Any
          # error shape yields an empty string, which denies.
          resp=$(gh api "repos/${GITHUB_REPOSITORY}/collaborators/${COMMENTER}/permission" 2>/dev/null)
          perm=$(printf '%s' "${resp}" | jq -r '.permission // empty' 2>/dev/null)
          case "${perm}" in
            admin|write) authorized=true ;;
            *)           authorized=false ;;
          esac
          if [ "${authorized}" = "true" ]; then
            echo "'${COMMENTER}' has '${perm}' access to ${GITHUB_REPOSITORY}; proceeding."
          else
            echo "::warning::'${COMMENTER}' does not have write access to ${GITHUB_REPOSITORY} (resolved permission '${perm:-none}'); skipping the binlog download."
          fi
          echo "authorized=${authorized}" >> "$GITHUB_OUTPUT"

      # Checks out this workflow's own scripts at the event ref, never the PR
      # head, so no PR-authored code is fetched or run.
      - name: Check out analysis scripts
        if: github.event_name != 'issue_comment' || steps.perm.outputs.authorized == 'true'
        uses: actions/checkout@v7.0.1
        with:
          sparse-checkout: .github/workflows/scripts
          persist-credentials: false

      - name: Download binlogs from the PR's latest failed Azure Pipelines build
        id: fetch
        if: github.event_name != 'issue_comment' || steps.perm.outputs.authorized == 'true'
        shell: bash
        env:
          GH_TOKEN: ${{ github.token }}
          GH_AW_REPO: ${{ github.repository }}
          ADO_API: "https://dev.azure.com/dnceng-public/public/_apis"
          ADO_BUILD_UI: "https://dev.azure.com/dnceng-public/public/_build/results"
          # roslyn-CI pipeline definition id in dnceng-public/public.
          ADO_BUILD_DEFINITION_ID: "95"
          # No `check_run` payload exists on a slash command, so locate the
          # build by the PR's merge branch instead.
          RESOLVE_MODE: latest
          PR_NUMBER: ${{ github.event.issue.number || fromJSON(github.event.inputs.aw_context || github.event.client_payload.aw_context || '{}').item_number }}
          BINLOG_DIR: /tmp/binlogs
          SCRIPT_DIR: ${{ github.workspace }}/.github/workflows/scripts
        run: bash "${SCRIPT_DIR}/fetch-build-binlogs.sh"

      - name: Upload analysis artifact
        if: steps.fetch.outputs.binlog-found == 'true'
        uses: actions/upload-artifact@v7.0.1
        with:
          name: build-failure-analysis-data
          path: /tmp/binlogs
          if-no-files-found: warn
          retention-days: 1

# Steps that run in the agent job. The top-level `if:` gates these on binlogs
# having been retrieved, so the agent never runs without something to analyze.
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
      # See build-failure-analysis.agent.md for the binlog path conventions. The
      # failed-job binlogs are read through the binlog-mcp MCP server (mounted
      # at `/data/binlogs`); GH_AW_BINLOG_HOST_PATH points at the Azure DevOps
      # build for human-facing references.
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
  # This workflow is triggered by an `issue_comment` on a PR, so it HAS a
  # triggering item — and it is the same PR `fetch-binlog` resolves from
  # `github.event.issue.number`. Binding to it prevents untrusted binlog/source
  # content from selecting a different repository target.
  report-failure-as-issue: false
  add-comment:
    max: 1
    target: "triggering"
    hide-older-comments: true
  create-pull-request-review-comment:
    max: 25
    target: "triggering"
    commit-id: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
  noop:
    max: 1
    report-as-issue: false
---

<!--
  Body provided by shared/build-failure-analysis-shared.md.
-->
