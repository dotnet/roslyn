---
name: "Build Failure Analysis (command)"
run-name: "Build failure analysis command ${{ github.event.comment.id }}"
description: >-
  Reruns the build-failure analysis when a maintainer comments
  `/analyze-build-failure` on a PR. Analyzes the PR's latest `roslyn-CI` build
  only if it failed, reusing the prompt, agent setup and fetch script of
  `build-failure-analysis.agent.md`.

on:
  # fetch-binlog checks the command position: the slash_command trigger's
  # generated predicate does not accept all JavaScript whitespace.
  issue_comment:
    types: [created, edited]
  roles: [admin, maintainer, write]
  reaction: "eyes"
  status-comment: true
  needs: [fetch-binlog]

if: needs.fetch-binlog.outputs.binlog-found == 'true'

# Keep read-only; see build-failure-analysis.agent.md.
permissions:
  contents: read
  pull-requests: read

# Separate from the automatic workflow's group so neither cancels the other.
# Commands for a PR queue rather than cancel; `queue: max` keeps a quoted
# command from evicting a pending real one. Other comments get a unique group.
concurrency:
  group: ${{ github.event_name == 'issue_comment' && github.event.issue.pull_request && contains(github.event.comment.body, '/analyze-build-failure') && contains(fromJSON('["OWNER","MEMBER","COLLABORATOR"]'), github.event.comment.author_association) && format('build-failure-analysis-cmd-{0}', github.event.issue.number) || format('build-failure-analysis-cmd-run-{0}', github.run_id) }}
  cancel-in-progress: false
  queue: max

timeout-minutes: 30

imports:
  - shared/build-failure-analysis-shared.md

engine: copilot

jobs:
  fetch-binlog:
    name: Fetch binlogs (Azure Pipelines)
    # Coarse pre-filter: this job runs before gh-aw's role check, so keep other
    # commenters from starting downloads. The first step applies the exact
    # command and permission rules. KEEP IN SYNC with `roles:`.
    if: >-
      github.event_name == 'issue_comment' &&
      github.event.repository.fork == false &&
      github.event.issue.pull_request &&
      contains(fromJSON('["OWNER","MEMBER","COLLABORATOR"]'), github.event.comment.author_association) &&
      contains(github.event.comment.body, '/analyze-build-failure')
    runs-on: ubuntu-latest
    timeout-minutes: 15
    permissions:
      actions: read
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
      # Uses `.permission`, not `role_name`: a custom role can reuse a base
      # role's name.
      - name: Verify the comment invokes the command and the commenter has write access
        id: perm
        shell: bash
        env:
          GH_TOKEN: ${{ github.token }}
          COMMENTER: ${{ github.event.comment.user.login }}
          COMMENT_BODY: ${{ github.event.comment.body }}
          COMMAND_NAME: "analyze-build-failure"
        run: |
          set +e
          # Same command-position rule as gh-aw's runtime, including trim().
          command_matches=$(node -e '
            const match = process.env.COMMENT_BODY.trim().match(/^\/([a-zA-Z0-9][a-zA-Z0-9._-]*)(?=$|\s)/);
            console.log(match?.[1] === process.env.COMMAND_NAME);
          ') || {
            echo "::error::Unable to check command position."
            exit 1
          }
          if [ "${command_matches}" != "true" ]; then
            echo "Comment does not start with '/${COMMAND_NAME}'; skipping the binlog download."
            echo "authorized=false" >> "$GITHUB_OUTPUT"
            exit 0
          fi
          # Reject malformed and bot logins before using it in an API path.
          if ! printf '%s' "${COMMENTER}" | grep -qE '^[A-Za-z0-9-]+$'; then
            echo "::warning::Commenter login is missing or malformed; skipping the binlog download."
            echo "authorized=false" >> "$GITHUB_OUTPUT"
            exit 0
          fi
          # `jq`, not `gh api --jq`: gh prints error bodies unfiltered. Errors deny.
          resp=$(gh api "repos/${GITHUB_REPOSITORY}/collaborators/${COMMENTER}/permission" 2>/dev/null)
          perm=$(printf '%s' "${resp}" | jq -r '.permission // empty' 2>/dev/null)
          case "${perm}" in
            admin|maintain|write) authorized=true ;;
            *)                    authorized=false ;;
          esac
          if [ "${authorized}" = "true" ]; then
            echo "'${COMMENTER}' has '${perm}' access to ${GITHUB_REPOSITORY}; proceeding."
          else
            echo "::warning::'${COMMENTER}' does not have write access to ${GITHUB_REPOSITORY} (resolved permission '${perm:-none}'); skipping the binlog download."
          fi
          echo "authorized=${authorized}" >> "$GITHUB_OUTPUT"

      - name: Check for completed command publication
        id: command
        if: steps.perm.outputs.authorized == 'true'
        uses: actions/github-script@v9.0.0
        env:
          WORKFLOW_FILE: build-failure-analysis-command.agent.lock.yml
        with:
          script: |
            if (context.eventName !== "issue_comment") {
              core.setOutput("completed", "false");
              return;
            }
            const comment = context.payload.comment;
            const title = `Build failure analysis command ${comment.id}`;
            for (let page = 1; ; page++) {
              const { data } = await github.rest.actions.listWorkflowRuns({
                ...context.repo, workflow_id: process.env.WORKFLOW_FILE,
                event: "issue_comment", status: "completed",
                created: `>=${comment.created_at}`, per_page: 100, page,
              });
              // GitHub caps filtered run searches at 1,000 results. Do not
              // silently treat an incomplete history as permission to publish.
              if (data.total_count > 1000) {
                throw new Error("Command history exceeds the GitHub search limit; post a new command.");
              }
              for (const run of data.workflow_runs) {
                if (run.display_title !== title) continue;
                const jobs = await github.paginate(github.rest.actions.listJobsForWorkflowRunAttempt, {
                  ...context.repo, run_id: run.id, attempt_number: run.run_attempt, per_page: 100,
                });
                if (jobs.some(job => job.name === "safe_outputs" && job.conclusion === "success" &&
                    job.steps?.some(step => step.name === "Process Safe Outputs" && step.conclusion === "success"))) {
                  core.notice("This command completed publication; post a new command to rerun.");
                  core.setOutput("completed", "true");
                  return;
                }
              }
              if (data.workflow_runs.length < 100 || page * 100 >= data.total_count) break;
            }
            core.setOutput("completed", "false");

      # Scripts come from the default branch, never the PR's refs.
      - name: Check out analysis scripts
        if: steps.command.outputs.completed == 'false'
        uses: actions/checkout@v7.0.1
        with:
          ref: refs/heads/${{ github.event.repository.default_branch }}
          sparse-checkout: .github/workflows/scripts
          persist-credentials: false

      # On failure or timeout `binlog-found` stays unset, which skips analysis.
      - name: Download binlogs from the PR's latest failed Azure Pipelines build
        id: fetch
        if: steps.command.outputs.completed == 'false'
        shell: bash
        continue-on-error: true
        env:
          GH_TOKEN: ${{ github.token }}
          GH_AW_REPO: ${{ github.repository }}
          # A comment has no check_run payload; find the PR's latest build.
          RESOLVE_MODE: latest
          PR_NUMBER: ${{ github.event.issue.number }}
          BINLOG_DIR: /tmp/binlogs
          SCRIPT_DIR: ${{ github.workspace }}/.github/workflows/scripts
        # Run outside the checkout so roslyn's global.json and Directory.Build.* do not apply.
        run: cd "${RUNNER_TEMP:-/tmp}" && timeout 600 dotnet run --file "${SCRIPT_DIR}/fetch-build-binlogs.cs" -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false -p:ImportDirectoryPackagesProps=false

      - name: Report an incomplete fetch
        if: steps.fetch.outcome == 'failure'
        run: echo "::warning::Binlog fetch did not complete (${{ steps.fetch.outcome }}); skipping analysis for this build."

      - name: Upload analysis artifact
        if: steps.fetch.outputs.binlog-found == 'true'
        uses: actions/upload-artifact@v7.0.1
        with:
          name: build-failure-analysis-data
          path: /tmp/binlogs
          if-no-files-found: warn
          retention-days: 1

# gh-aw does not import `safe-outputs.data`; the rest is in the shared file.
safe-outputs:
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
---

<!--
  Body provided by shared/build-failure-analysis-shared.md.
-->
