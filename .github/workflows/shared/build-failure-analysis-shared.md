---
# Agent setup, tools, safe outputs and prompt shared by
# build-failure-analysis.agent.md and build-failure-analysis-command.agent.md.
# Each importer defines the trigger and a `fetch-binlog` job that uploads the
# `build-failure-analysis-data` artifact and sets the outputs used below.

description: "Shared body for build-failure-analysis workflows"

model: gpt-5.6-sol

network:
  allowed:
    - defaults
    - dotnet
    # Also keeps Azure build links in sanitized safe outputs.
    - dev.azure.com

# Reads untrusted binlogs, so its digest is pinned in .github/aw/actions-lock.json.
mcp-servers:
  binlog-mcp:
    container: "mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64"
    mounts:
      - "/tmp/binlogs:/data/binlogs:ro"
    allowed: ["*"]

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
      # binlog-mcp sees /tmp/binlogs as /data/binlogs.
      BINLOG_DIR="/data/binlogs"
      LIST=""
      if [ "${GH_AW_BINLOG_FOUND_VALUE:-false}" = "true" ] && [ -d /tmp/binlogs ]; then
        for f in /tmp/binlogs/*.binlog; do
          [ -f "$f" ] || continue
          LIST="${LIST}${BINLOG_DIR}/$(basename "$f")"$'\n'
        done
      fi
      # Parameter expansion, not `| head -1`, which can SIGPIPE under pipefail.
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
  # Read-only agent: no write tool (honored by gh-aw >= v0.87.10).
  edit: false
  github:
    # External PRs must be readable; their content is untrusted, like binlogs.
    min-integrity: none
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
    # CLI wrapper for binlog-mcp, for when the agent does not call the MCP tool.
    - "binlog-mcp:*"

safe-outputs:
  needs: [fetch-binlog]
  steps:
    # Command runs only: skip outputs a retried run already published.
    - name: Prepare retry-safe command outputs
      if: github.event_name == 'issue_comment' && steps.download-agent-output.outcome == 'success'
      uses: actions/github-script@v9.0.0
      env:
        GH_AW_AGENT_OUTPUT: ${{ steps.setup-agent-output-env.outputs.GH_AW_AGENT_OUTPUT }}
        EXPECTED_HEAD: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
      with:
        script: |
          const fs = require("node:fs");
          const { createHash } = require("node:crypto");
          const request = context.payload.comment.id;
          const pullNumber = context.payload.issue.number;
          const head = process.env.EXPECTED_HEAD;
          if (!Number.isSafeInteger(request) || !/^[a-f0-9]{40}$/.test(head)) {
            throw new Error("Missing verified command or revision identity.");
          }
          const outputPath = process.env.GH_AW_AGENT_OUTPUT;
          const output = JSON.parse(fs.readFileSync(outputPath, "utf8"));
          if (!Array.isArray(output.items)) throw new Error("Expected an output items array.");
          const comments = await github.paginate(github.rest.issues.listComments, {
            ...context.repo, issue_number: pullNumber, per_page: 100,
          });
          const reviews = await github.paginate(github.rest.pulls.listReviews, {
            ...context.repo, pull_number: pullNumber, per_page: 100,
          });
          const inline = await github.paginate(github.rest.pulls.listReviewComments, {
            ...context.repo, pull_number: pullNumber, per_page: 100,
          });
          const isBot = item => item.user?.login === "github-actions[bot]" && item.user?.type === "Bot";
          const submitted = reviews.filter(review => isBot(review) && review.state !== "PENDING" && review.submitted_at);
          const reviewIds = new Set(submitted.map(review => review.id));
          // Include review bodies: gh-aw moves unanchorable findings there.
          const published = [...comments.filter(isBot), ...submitted,
            ...inline.filter(item => isBot(item) && (item.pull_request_review_id == null || reviewIds.has(item.pull_request_review_id)))];
          const markers = new Set(published.flatMap(item =>
            [...(item.body || "").matchAll(/^Build-analysis output: `(\d+:[a-f0-9]{64})`$/gm)].map(match => match[1])));
          output.items = output.items.filter(item => {
            if (item.type !== "add_comment" && item.type !== "create_pull_request_review_comment") return true;
            if (typeof item.body !== "string") throw new Error("Expected a comment body.");
            const body = item.body.replace(/^Build-analysis output: `\d+:[a-f0-9]{64}`\n\n/, "").replace(/\r\n/g, "\n").trim();
            // There is one summary per request/revision. Inline identity also
            // includes the full finding and anchor, so distinct findings survive.
            const identity = item.type === "add_comment" ? [request, head, item.type] :
              [request, head, item.type, item.path, Number(item.line), item.side || "RIGHT",
                item.start_line ? Number(item.start_line) : null, body];
            const key = `${request}:${createHash("sha256").update(JSON.stringify(identity)).digest("hex")}`;
            if (markers.has(key)) {
              core.info(`Skipping previously published ${item.type} (${key}).`);
              return false;
            }
            markers.add(key);
            item.body = `Build-analysis output: \`${key}\`\n\n${body}`;
            return true;
          });
          fs.writeFileSync(outputPath, JSON.stringify(output));
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
  report-failure-as-issue: false
  # Bound to the PR that fetch-binlog validated against the build, so binlog or
  # source content cannot choose another target.
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

# Build Failure Analyst

You are the **build-failure analyst**. Analyze the binary logs of the Azure
DevOps build that just failed and produce a PR review using the safe-output
tools (a later `safe_outputs` job performs the actual GitHub write).
Do **not** try to spawn a sub-agent: the `task` tool is intentionally not
available here. Work directly with the tools you do have: `binlog-mcp` to
read the logs, the `github` tools to read PR/repo context (the GitHub MCP
server is **read-only** here), the `safeoutputs` tools (`add_comment`,
`create_pull_request_review_comment`, `noop`) to post results, and a small set
of read-only `shell` commands (including `cat`).

## Instructions

1. Read the agent-context environment variables: `GH_AW_BUILD_OUTCOME`,
   `GH_AW_BINLOG_LIST`, `GH_AW_BINLOG_DIR`, `GH_AW_BINLOG_PATH`,
   `GH_AW_BINLOG_HOST_PATH`, `GH_AW_PR_NUMBER`, `GH_AW_PR_HEAD_SHA`,
   `GH_AW_PR_MERGE_SHA`, `GH_AW_WORKSPACE`.

2. If `GH_AW_BUILD_OUTCOME == 'success'`, the build did not actually fail —
   there is nothing to analyze. Call `noop` with the message
   `"Build succeeded — no analysis required."` and stop.

3. Load your detailed playbook: `cat .github/agents/build-failure-analyst.agent.md`
   (it is checked out with the repository config). Follow that methodology —
   root-cause grouping, source-context reading via the GitHub API at
   `GH_AW_PR_HEAD_SHA`, comment/suggestion formatting, and defensive behavior.
   In summary:
   - Iterate **every** path in `GH_AW_BINLOG_LIST` (newline-separated
     in-container binlog paths from the failed/canceled build jobs, under
     `GH_AW_BINLOG_DIR` = `/data/binlogs`) and query the `binlog-mcp` MCP
     server (`binlog_errors`, `binlog_overview`, `binlog_warnings`, …) with
     `binlog_file` set to each leg's path — a failure usually surfaces in only
     one leg, so do not analyze just the first. `binlog_errors`,
     `binlog_overview`, `binlog_warnings`, … are **MCP tools** provided by the
     `binlog-mcp` server: prefer calling them **directly as MCP tools** (with a
     `binlog_file` argument). A CLI wrapper is also mounted and allowlisted, so
     you may alternatively run `binlog-mcp <tool> --binlog_file <path>` via the
     shell. Take the clean-build branch only after every required query
     completed successfully for every listed binlog; if a query failed, report
     the analysis gap as directed by the analyst playbook instead. If all
     queries succeeded, no leg shows errors, and there is no
     failed-target/process evidence, the build compiled cleanly — the
     pipeline failure is then a **non-build** (test/packaging/publishing) failure,
     which is **out of scope**. This workflow analyzes build failures only, so
     **post nothing**: call `noop` with a short reason and stop. Do **not**
     post a summary comment and do **not** invent fixes.
   - Post exactly one summary via `add_comment` with structured data
     `{"workflow_artifact":"build-failure-analysis","artifact_kind":"analysis"}`
     and any inline
     `suggestion` blocks via `create_pull_request_review_comment`. Both
     workflows bind safe outputs deterministically to `GH_AW_PR_NUMBER`; do
     not attempt to choose or override the target in a safe-output call.
     Safe-output calls are irreversible queued writes, not previews: fully
     construct the final body before the first call, and never send a test,
     placeholder, or draft safe output.
   - `submit_pull_request_review` is **not** a safe output for this workflow;
     inline comments stand alone.

4. When you have posted the analysis for a genuine build failure (or called
   `noop` for a clean-compile / non-build failure), stop.
