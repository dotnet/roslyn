---
name: "Build Failure Analysis"
description: >-
  When the Azure Pipelines PR build (`roslyn-CI`) fails, downloads the binary
  logs of its failed or canceled jobs (it does not rebuild) and has the
  `build-failure-analyst` agent post a root-cause summary and inline
  `suggestion` comments on the PR.

# Trust model: archive entries and binlog contents are attacker-controlled on
# any PR. No PR code is built or run, fetch scripts come from the default
# branch, the agent job is read-only, and writes go through gh-aw's
# schema-validated safe outputs bound to the validated PR. Manual dispatch is
# refused on other refs.

on:
  check_run:
    types: [completed]
  # Include external contributors; the agent cannot write.
  roles: all
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
  needs: [fetch-binlog]

if: needs.fetch-binlog.outputs.binlog-found == 'true'

# Keep read-only; gh-aw's separate safe_outputs job holds the write scope. Do
# not add `copilot-requests: write`: inference via github.token gets HTTP 403
# in this org.
permissions:
  contents: read
  pull-requests: read

# roslyn-CI failures and dispatches for a PR share a group, so a newer analysis
# supersedes an older one. Any other check_run gets a unique group.
concurrency:
  group: ${{ (github.event_name == 'check_run' && github.event.check_run.name == 'roslyn-CI' && format('build-failure-analysis-{0}', github.event.check_run.pull_requests[0].number || github.event.check_run.head_sha)) || (github.event_name == 'workflow_dispatch' && github.ref == format('refs/heads/{0}', github.event.repository.default_branch) && format('build-failure-analysis-{0}', inputs['pr-number'])) || format('build-failure-analysis-run-{0}', github.run_id) }}
  cancel-in-progress: true
  job-discriminator: ${{ github.run_id }}

timeout-minutes: 30

imports:
  - shared/build-failure-analysis-shared.md

engine: copilot

jobs:
  fetch-binlog:
    name: Fetch binlogs (Azure Pipelines)
    runs-on: ubuntu-latest
    timeout-minutes: 15
    if: >
      github.event.repository.fork == false &&
      (github.event_name == 'workflow_dispatch' ||
       (github.event_name == 'check_run' &&
        github.event.check_run.name == 'roslyn-CI' && github.event.check_run.conclusion == 'failure'))
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
      - name: Require the default branch for manual dispatch
        if: github.event_name == 'workflow_dispatch'
        shell: bash
        env:
          DEFAULT_BRANCH: ${{ github.event.repository.default_branch }}
        run: |
          # Branch names are case-sensitive; Actions expression equality is not.
          if [ "$GITHUB_REF" != "refs/heads/${DEFAULT_BRANCH}" ]; then
            echo "::error::Manual build-failure analysis must run from the repository default branch."
            exit 1
          fi

      - name: Check out analysis scripts
        uses: actions/checkout@v7.0.1
        with:
          ref: refs/heads/${{ github.event.repository.default_branch }}
          sparse-checkout: .github/workflows/scripts
          persist-credentials: false

      # On failure or timeout `binlog-found` stays unset, which skips analysis.
      - name: Download binlogs from the failed Azure Pipelines build
        id: fetch
        shell: bash
        continue-on-error: true
        env:
          GH_TOKEN: ${{ github.token }}
          GH_AW_REPO: ${{ github.repository }}
          RESOLVE_MODE: ${{ github.event_name == 'workflow_dispatch' && 'dispatch' || 'check_run' }}
          # Empty for fork PRs; the script resolves those from CHECK_HEAD_SHA.
          PR_NUMBER: ${{ github.event.check_run.pull_requests[0].number || inputs['pr-number'] }}
          CHECK_HEAD_SHA: ${{ github.event.check_run.head_sha }}
          CHECK_DETAILS_URL: ${{ github.event.check_run.details_url }}
          DISPATCH_BUILD_ID: ${{ inputs['ado-build-id'] }}
          BINLOG_DIR: /tmp/binlogs
          SCRIPT_DIR: ${{ github.workspace }}/.github/workflows/scripts
        # Run outside the checkout so roslyn's global.json and Directory.Build.* do not apply.
        run: cd "${RUNNER_TEMP:-/tmp}" && timeout 600 dotnet run --file "${SCRIPT_DIR}/fetch-build-binlogs.cs" -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false -p:ImportDirectoryPackagesProps=false

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

<!-- Body provided by shared/build-failure-analysis-shared.md. -->
