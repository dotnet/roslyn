---
name: "Build Failure Analysis (command)"
description: >-
  Rerun the build-failure analysis on a pull request when a maintainer comments
  `/analyze-build-failure`. Same body as `build-failure-analysis.agent.md` — it does
  NOT rebuild: it inspects the PR's **latest** Azure Pipelines `roslyn-CI`
  build and, **only when that latest build has failed** (it stops if the
  newest build is still running or has succeeded), downloads the binary logs
  from that build's failed or canceled jobs and delegates to the
  `build-failure-analyst` agent (which queries the binlogs live via the
  containerized `binlog-mcp` MCP server). Useful when a previous run was
  cancelled, the analysis comment was dismissed, or the agent needs another
  pass. Like the auto workflow it performs **no build**; the generated jobs do
  check out the repository (and, for the slash-command event, the PR branch)
  for agent tooling only — the PR's code is never built or executed.

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

# Least-privilege for the workflow/agent jobs. The agent runs read-only; it
# does NOT post directly. All PR writes it produces (summary comment + inline
# review suggestions) go through gh-aw **safe-outputs**, which the compiler
# emits as a separate `safe_outputs` job granted `pull-requests: write` +
# `issues: write` in the generated lock. (The slash-command trigger also adds
# an acknowledgement reaction to the command comment; gh-aw emits that in its
# own generated job with the scope it needs — it is not driven by this agent
# job.) Keep `pull-requests: read` here so the AI agent job stays
# least-privilege — do NOT raise it to `write`, that would hand PR-write scope
# to the agent job unnecessarily.
#
# Do NOT add `copilot-requests: write` here. That permission switches gh-aw's
# generated lock from `COPILOT_GITHUB_TOKEN: ${{ secrets.COPILOT_GITHUB_TOKEN }}`
# to `${{ github.token }}`, and the ephemeral Actions token is not entitled for
# inference against api.githubcopilot.com in this org — every agent run then
# dies in ~2s with "Authentication failed with provider ... (HTTP 403)" on both
# /models and /chat/completions, before it reads the prompt or opens a binlog.
# `ai-artifact-audit.md` omits it and works; keep this consistent.
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
# rationale. The fetch-binlog job downloads failed-job binlogs from Azure
# DevOps into a directory and uploads them; the agent job downloads them to
# `/tmp/binlogs` and the gh-aw MCP gateway mounts it read-only at
# `/data/binlogs`.
#
# The digest is pinned in `.github/aw/actions-lock.json` because this container
# processes artifacts from untrusted PRs. Refresh/inspect the current digest with:
#   docker buildx imagetools inspect \
#     mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64
mcp-servers:
  binlog-mcp:
    container: "mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64"
    mounts:
      - "/tmp/binlogs:/data/binlogs:ro"
    allowed: ["*"]

# Custom job that reuses the binlogs from the PR's most recent failed Azure
# DevOps `roslyn-CI` build instead of rebuilding. Mirrors the fetch-binlog job
# in build-failure-analysis.agent.md; it locates the build by the PR's merge branch
# (no `check_run` payload is available on a slash command).
jobs:
  fetch-binlog:
    name: Fetch binlogs (Azure Pipelines)
    # Cheap pre-gate. This job is a dependency of gh-aw's `pre_activation`, so it
    # runs BEFORE the role / command-position check. Without a guard it would
    # download hundreds of MB of binlogs on *every* comment in the repository,
    # which any public commenter could trigger repeatedly. This expression is
    # only the free first filter — `author_association` is coarse (in an
    # org-owned repo every org member reports MEMBER regardless of the
    # permission they actually hold here), so the step below resolves the
    # commenter's real repository permission before anything is downloaded.
    # `pre_activation` remains the authoritative role + command-position check,
    # and `activation` additionally requires `binlog-found == 'true'`.
    #
    # KEEP IN SYNC with `roles:` in the frontmatter above. The author_association
    # list here and the permission step below are hand-written restatements of
    # that policy; editing `roles:` does NOT update them, because only
    # `pre_activation` is generated from the frontmatter.
    #
    # `github.event.issue.pull_request` is what keeps plain issue comments out:
    # gh-aw emits no such filter of its own despite `events: [pull_request_comment]`
    # (checked in the generated lock), so PR-only scoping is a property of this
    # hand-written expression rather than something the compiler enforces. It
    # degrades safely without it — `repos/.../pulls/<issue#>` 404s and the script
    # emits no binlog — but it would pay for a runner first.
    #
    # `contains(..., '/analyze-build-failure')` is a substring match anywhere in
    # the body, whereas the authoritative `check_command_position` requires the
    # command to be in a valid position. So a write-access user merely mentioning
    # the command, or editing an old comment that quotes it (`types:` includes
    # `edited`), still starts this job. Workflow `if:` expressions have no
    # regex, and `startsWith` would reject the leading whitespace/newlines gh-aw
    # accepts, so this stays a deliberate over-approximation — but it is now
    # only a cheap pre-filter: the first step of the job reproduces gh-aw's real
    # first-token check and bails out before anything is downloaded.
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
      # `author_association` in the job-level `if:` cannot tell an org member
      # with read-only access apart from a maintainer, so resolve the real
      # repository permission here — before any download — and match it against
      # the same `roles: [admin, maintainer, write]` this command declares.
      # KEEP IN SYNC with that list.
      #
      # `.permission` is the field to test. The REST docs for this endpoint say
      # it returns the legacy base roles admin|write|read|none, "where the
      # maintain role is mapped to write and the triage role is mapped to read",
      # so `admin|write` is exactly "has push access or better" — precisely the
      # set `roles: [admin, maintainer, write]` describes, with maintainers
      # included.
      #
      # `.role_name` is deliberately NOT consulted. It reports "the name of the
      # assigned role, including custom roles", and a custom organization role
      # only has to avoid the base names read/triage/write/maintain/admin — so
      # matching on it would let a role merely *named* like a privileged one
      # (e.g. a custom `maintainer` inheriting read) pass this gate with no push
      # access at all.
      #
      # On any API failure the response carries no `.permission`, so `perm` ends
      # up empty and the check falls into the deny branch; failing closed is the
      # safe direction for a pre-gate.
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
          # The job-level `if:` can only use `contains()`, a plain substring
          # test, so a comment that merely mentions the command — or an edited
          # old comment quoting it — still reaches this job and pays for the
          # download before `pre_activation` throws the result away. That check
          # runs too late by construction, so reproduce it here.
          #
          # gh-aw trims the body and requires the command to be the FIRST token:
          # `/^\/([a-zA-Z0-9][a-zA-Z0-9._-]*)(?=$|\s)/` over the trimmed text,
          # then an equality comparison on the captured name
          # (actions/setup/js/slash_command_matcher.cjs). `awk 'NF {print $1;
          # exit}'` is the same rule: skip leading whitespace/blank lines, take
          # the first whitespace-delimited token. The token is delimited by
          # whitespace or end-of-input, exactly the `(?=$|\s)` lookahead, so
          # `/analyze-build-failure-now` correctly does NOT match. `tr -d '\r'`
          # is needed because JS `.trim()` and `\s` treat CR as whitespace while
          # awk's default field splitting does not.
          # KEEP IN SYNC with `on.command.name` below.
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
          # `COMMENTER` is interpolated into an API path and into log output, so
          # give it the same shape check `PR_NUMBER` and `BUILD_ID` get below.
          # GitHub logins are alphanumerics and hyphens; anything else (a bot
          # login such as `github-actions[bot]`, or an empty value) is rejected
          # here instead of being sent to the API.
          if ! printf '%s' "${COMMENTER}" | grep -qE '^[A-Za-z0-9-]+$'; then
            echo "::warning::Commenter login is missing or malformed; skipping the binlog download."
            echo "authorized=false" >> "$GITHUB_OUTPUT"
            exit 0
          fi
          # Read the response first and extract with `jq` rather than using
          # `gh api --jq`: on a non-2xx response `gh` prints the error document
          # to stdout, which `--jq` does not filter, so the raw JSON would end
          # up in `perm` and get echoed into the log. Extracting the field
          # ourselves yields an empty string for any error shape.
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
          PR_NUMBER: ${{ github.event.issue.number || fromJSON(github.event.inputs.aw_context || github.event.client_payload.aw_context || '{}').item_number }}
        run: |
          # Advisory + best-effort. On any gap emit binlog-found=false so the
          # agent pipeline stays inert.
          set +e
          set +o pipefail
          emit_none() { echo "binlog-found=false" >> "$GITHUB_OUTPUT"; exit 0; }

          [ -z "${PR_NUMBER}" ] && { echo "::warning::No PR number resolved from the slash-command event / aw_context."; emit_none; }
          # PR_NUMBER feeds GitHub API paths and the `refs/pull/<n>/merge`
          # branch query; require it numeric so a malformed event/aw_context
          # payload can't reach those URLs with unexpected content.
          if ! printf '%s' "${PR_NUMBER}" | grep -qE '^[0-9]+$'; then
            echo "::warning::Resolved PR number '${PR_NUMBER}' is not numeric; refusing."; emit_none
          fi

          # --- Scope check: only analyze PRs targeted by roslyn-CI ---------
          PR_JSON=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
          BASE_REF=$(printf '%s' "${PR_JSON}" | jq -r '.base.ref // empty')
          # An empty BASE_REF means the `gh api` call failed or returned no
          # data (rate limit / transient error), NOT that the PR targets an
          # out-of-scope branch. Treat it as a data-resolution failure so a
          # valid PR isn't silently skipped and misreported as base '' out of
          # scope.
          [ -z "${BASE_REF}" ] && { echo "::warning::Could not resolve the base ref for PR #${PR_NUMBER} (GitHub API returned no data); treating as a data-resolution failure, not an out-of-scope branch."; emit_none; }
          HEAD_SHA=$(printf '%s' "${PR_JSON}" | jq -r '.head.sha // empty')
          case "${BASE_REF}" in
            main|main-vs-deps|community|release/*|features/*|demos/*) echo "PR #${PR_NUMBER} base '${BASE_REF}' is in scope." ;;
            *) echo "::warning::PR #${PR_NUMBER} base '${BASE_REF}' is not targeted by roslyn-CI; skipping."; emit_none ;;
          esac

          # --- Find the PR's most recent roslyn-CI build (merge ref) ---------
          # Query the newest build REGARDLESS of status (queue-time desc). If
          # the newest build is still queued/running — e.g. right after a
          # force-push — skip: analyzing an older completed failure now would
          # pair a stale binlog with the PR's current head. Only proceed when
          # the newest build is completed AND failed. The head SHA is then
          # anchored to that build's own revision (below), so links/suggestions
          # always match the analyzed binlog.
          builds_json=$(curl -sSL --retry 3 \
            "${ADO_API}/build/builds?definitions=${ADO_BUILD_DEFINITION_ID}&branchName=refs/pull/${PR_NUMBER}/merge&queryOrder=queueTimeDescending&\$top=1&api-version=7.1")
          BUILD_ID=$(printf '%s' "${builds_json}" | jq -r '.value // [] | .[0].id // empty')
          BUILD_STATUS=$(printf '%s' "${builds_json}" | jq -r '.value // [] | .[0].status // empty')
          BUILD_RESULT=$(printf '%s' "${builds_json}" | jq -r '.value // [] | .[0].result // empty')
          echo "Newest roslyn-CI build for PR #${PR_NUMBER}: id='${BUILD_ID}' status='${BUILD_STATUS}' result='${BUILD_RESULT}'"
          [ -z "${BUILD_ID}" ] && { echo "::warning::No roslyn-CI build found for PR #${PR_NUMBER}."; emit_none; }
          # Require a numeric build id before it feeds subsequent ADO API URLs,
          # so a malformed query response can't inject unexpected path/query.
          if ! printf '%s' "${BUILD_ID}" | grep -qE '^[0-9]+$'; then
            echo "::warning::ADO build id '${BUILD_ID}' is not numeric; refusing."; emit_none
          fi
          if [ "${BUILD_STATUS}" != "completed" ]; then
            echo "::warning::PR #${PR_NUMBER}'s newest roslyn-CI build (${BUILD_ID}) is still '${BUILD_STATUS}'; wait for it to finish before analyzing."
            emit_none
          fi
          if [ "${BUILD_RESULT}" != "failed" ]; then
            echo "::warning::PR #${PR_NUMBER}'s newest roslyn-CI build (${BUILD_ID}) result is '${BUILD_RESULT}', not failed — the failure looks resolved; nothing to analyze."
            emit_none
          fi

          # Require the build's analyzed revision to equal the PR's CURRENT
          # head. Inline safe outputs are pinned to the verified head, and all
          # queued writes are revision-gated again before application. The PR
          # can advance between selecting the build and downloading artifacts,
          # and right after a force-push this query can still return the
          # previous failed build — so re-read the head here and skip if it
          # moved.
          build_json=$(curl -sSL --retry 3 "${ADO_API}/build/builds/${BUILD_ID}?api-version=7.1")
          BUILD_PR_SHA=$(printf '%s' "${build_json}" | jq -r '.triggerInfo["pr.sourceSha"] // empty')
          BUILD_MERGE_SHA=$(printf '%s' "${build_json}" | jq -r '.sourceVersion // empty')
          PR_JSON2=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
          CURRENT_HEAD=$(printf '%s' "${PR_JSON2}" | jq -r '.head.sha // empty')
          CURRENT_MERGE=$(printf '%s' "${PR_JSON2}" | jq -r '.merge_commit_sha // empty')
          # Fail CLOSED unless both head and merge revisions are known. The
          # merge revision is required to detect a moved base branch even when
          # the PR head itself is unchanged.
          if [ -z "${BUILD_PR_SHA}" ] || [ -z "${CURRENT_HEAD}" ] || [ -z "${BUILD_MERGE_SHA}" ] || [ -z "${CURRENT_MERGE}" ]; then
            echo "::warning::Could not resolve all build/current head and merge revisions; skipping."
            emit_none
          fi
          if [ "${BUILD_PR_SHA}" != "${CURRENT_HEAD}" ]; then
            echo "::warning::Build ${BUILD_ID} analyzed revision '${BUILD_PR_SHA}' but PR #${PR_NUMBER} head is now '${CURRENT_HEAD}'; skipping stale build (a newer build will cover the current revision)."
            emit_none
          fi
          # ADO builds GitHub's `refs/pull/<n>/merge` ref, so build_json.sourceVersion
          # is that merge commit; if the base branch advanced it differs from the
          # PR's current merge_commit_sha even with the head unchanged. Skip stale merges.
          if [ "${BUILD_MERGE_SHA}" != "${CURRENT_MERGE}" ]; then
            echo "::warning::Build ${BUILD_ID} merge revision '${BUILD_MERGE_SHA}' but PR #${PR_NUMBER} current merge is '${CURRENT_MERGE}' (base branch advanced); skipping stale merge."
            emit_none
          fi
          HEAD_SHA="${CURRENT_HEAD}"
          echo "Analyzing build ${BUILD_ID} at PR head revision '${HEAD_SHA}'."

          # --- Download failed-job build-log artifacts and binlogs ---------
          # Roslyn publishes "<job> Attempt <N> Logs" for most jobs, with
          # explicit exceptions for Source Build and the bootstrap-correctness
          # leg. Match artifact bases exactly and keep every retry artifact.
          timeline_json=$(curl -sSL --fail --retry 3 \
            "${ADO_API}/build/builds/${BUILD_ID}/timeline?api-version=7.1")
          mapfile -t failed_job_names < <(
            printf '%s' "${timeline_json}" |
              jq -r '.records // [] | map(select(.type == "Job" and (.result == "failed" or .result == "canceled"))) | .[].name' |
              awk 'NF && !seen[$0]++'
          )
          [ "${#failed_job_names[@]}" -eq 0 ] && { echo "::warning::No failed or canceled jobs found in the timeline for build ${BUILD_ID}."; emit_none; }

          artifacts_json=$(curl -sSL --fail --retry 3 "${ADO_API}/build/builds/${BUILD_ID}/artifacts?api-version=7.1")
          mapfile -t all_names < <(
            printf '%s' "${artifacts_json}" |
              jq -r '.value // [] | map(select(.name | test(" Attempt [0-9]+ Logs$") or test("^BuildLogs_SourceBuild_Managed_Attempt[0-9]+$"))) | .[].name'
          )
          mapfile -t names < <(
            for job_name in "${failed_job_names[@]}"; do
              expected="${job_name}"
              source_build=false
              case "${job_name}" in
                "Source-Build (Managed)") source_build=true ;;
                Correctness_Bootstrap_Build_Default) expected="Correctness_Bootstrap_Build - Default" ;;
              esac
              for name in "${all_names[@]}"; do
                if [ "${source_build}" = true ] && [[ "${name}" =~ ^BuildLogs_SourceBuild_Managed_Attempt[0-9]+$ ]]; then
                  printf '%s\n' "${name}"
                  continue
                fi
                if [[ "${name}" =~ ^(.+)\ Attempt\ ([0-9]+)\ Logs$ ]] && [ "${BASH_REMATCH[1]}" = "${expected}" ]; then
                  printf '%s\n' "${name}"
                fi
              done
            done |
              awk 'NF && !seen[$0]++'
          )
          [ "${#names[@]}" -eq 0 ] && { echo "::warning::No build-log artifacts matched the failed or canceled jobs in build ${BUILD_ID}; the failure is likely outside a build leg."; emit_none; }
          echo "Selected ${#names[@]} of ${#all_names[@]} build-log artifacts for ${#failed_job_names[@]} failed or canceled jobs."

          # Guards for untrusted PR-produced archives: cap the compressed
          # download and the reported uncompressed size per artifact, bound
          # extraction time, AND enforce a cumulative uncompressed budget across
          # all legs so many individually-small artifacts can't collectively
          # exhaust the runner's disk.
          MAX_ZIP_BYTES=524288000       # 500 MB compressed per artifact
          MAX_UNZIP_BYTES=2147483648    # 2 GB uncompressed per artifact
          MAX_TOTAL_BYTES=4294967296    # 4 GB uncompressed across all artifacts
          TOTAL_BYTES=0
          mkdir -p /tmp/binlogs
          count=0
          staged_legs=0
          ai=0
          for name in "${names[@]}"; do
            # `name` is PR-controlled ADO artifact metadata and the allowlist
            # above still originates in metadata, so sanitize it
            # before using it in any on-disk path or workflow command (guards
            # against path traversal and command injection); keep the original
            # `name` only for the artifacts_json lookup.
            safe_name=$(printf '%s' "${name}" | tr -c 'A-Za-z0-9._-' '_')
            ai=$((ai + 1))
            url=$(printf '%s' "${artifacts_json}" | jq -r --arg n "${name}" '.value[] | select(.name==$n) | .resource.downloadUrl // empty')
            [ -z "${url}" ] && continue
            rm -rf /tmp/ax /tmp/a.zip
            mkdir -p /tmp/ax
            # Download to a file, never a pipe: curl retries transient
            # 5xx/429/timeouts but can only rewind seekable output, so through
            # a pipe the retried body is APPENDED — a 503 error page followed
            # by a retry yields a corrupt `<error page><zip>` that still exits
            # 0. `--fail` keeps error bodies off disk.
            # `ulimit -f` is only a disk backstop for a response that declares
            # no Content-Length; the `-ge MAX_ZIP_BYTES` guard below is
            # authoritative. Divide by 512 so the cap is >= MAX_ZIP_BYTES under
            # either block-size reading (bash uses 1024, POSIX says 512).
            # SIGXFSZ is ignored so hitting the cap is an ordinary write error
            # (23) rather than a "File size limit exceeded (core dumped)" log.
            (
              ulimit -f $((MAX_ZIP_BYTES / 512))
              trap '' XFSZ
              curl -sSL --fail --retry 3 --retry-delay 2 --max-time 600 -o /tmp/a.zip "${url}"
            ) 2>/dev/null
            curl_rc=$?
            ZIP_BYTES=$(stat -c%s /tmp/a.zip 2>/dev/null || echo 0)
            if [ "${ZIP_BYTES}" -eq 0 ]; then
              echo "::warning::Skipping ${safe_name}: empty or failed download."; continue
            fi
            if [ "${ZIP_BYTES}" -ge "${MAX_ZIP_BYTES}" ]; then
              echo "::warning::Skipping ${safe_name}: download reached the ${MAX_ZIP_BYTES}-byte cap."; continue
            fi
            # After the size guards: hitting the ulimit cap is reported as an
            # oversized artifact above, not as a generic transfer failure.
            if [ "${curl_rc}" -ne 0 ]; then
              echo "::warning::Skipping ${safe_name}: download failed or was truncated (curl exit ${curl_rc})."; continue
            fi
            # `unzip -Zt` prints ONE summary line ("<n> files, <x> bytes
            # uncompressed, ..."), so the total comes from a fixed column
            # instead of the shifting last row of `unzip -l`. Use `END{}`:
            # Info-ZIP prepends warnings on STDOUT for a recoverable archive,
            # and a multi-line value would still pass the `grep -qE` check
            # below, since `grep -q` matches if ANY line matches. `timeout`
            # bounds a hostile archive; pipefail + fail-closed because a killed
            # probe's partial output can end in a numeric column and undercount.
            UNCOMP=$(set -o pipefail; timeout 60 unzip -Zt /tmp/a.zip 2>/dev/null | awk 'END{print $3}') \
              || { echo "::warning::Skipping ${safe_name}: 'unzip -Zt' failed or timed out; cannot verify uncompressed size."; continue; }
            # Fail safe: a non-numeric size (corrupt zip, unexpected or
            # timed-out output) can't be verified, so skip rather than let it
            # bypass the guards below.
            if ! printf '%s' "${UNCOMP}" | grep -qE '^[0-9]+$'; then
              echo "::warning::Skipping ${safe_name}: could not determine uncompressed size (unparseable/timed-out unzip output)."; continue
            fi
            # ZIP64 sizes can reach ~20 digits, overflowing Bash's signed
            # 64-bit `-gt` (and the `$((...))` below), which under `set +e`
            # would let an oversized archive through. More digits than the
            # limit is unambiguously larger, so reject on length first.
            if [ "${#UNCOMP}" -gt "${#MAX_UNZIP_BYTES}" ]; then
              echo "::warning::Skipping ${safe_name}: uncompressed size has ${#UNCOMP} digits, exceeding the ${MAX_UNZIP_BYTES} guard (possible zip bomb)."; continue
            fi
            if [ "${UNCOMP}" -gt "${MAX_UNZIP_BYTES}" ]; then
              echo "::warning::Skipping ${safe_name}: uncompressed size ${UNCOMP} exceeds ${MAX_UNZIP_BYTES} guard (possible zip bomb)."; continue
            fi
            if [ $((TOTAL_BYTES + UNCOMP)) -gt "${MAX_TOTAL_BYTES}" ]; then
              echo "::warning::Cumulative uncompressed budget ${MAX_TOTAL_BYTES} reached at ${safe_name}; stopping extraction."; break
            fi
            # Inspect every central-directory entry before reading any payload.
            # Selected binlogs are copied as bytes into fresh, generated regular
            # files; archive paths and file-type metadata are never materialized.
            # This rejects traversal and link/device entries and prevents a
            # symlink or hardlink entry from redirecting a later write.
            EXTRACTED=$(
              timeout 120 env ARCHIVE=/tmp/a.zip DESTINATION=/tmp/ax MAX_UNZIP_BYTES="${MAX_UNZIP_BYTES}" python3 -c '
          import os
          from pathlib import PurePosixPath
          import resource
          import stat
          import struct
          import zipfile

          archive = os.environ["ARCHIVE"]
          destination = os.environ["DESTINATION"]
          max_unzip_bytes = int(os.environ["MAX_UNZIP_BYTES"])

          # Limit address space before parsing attacker-controlled ZIP metadata.
          resource.setrlimit(resource.RLIMIT_AS, (1024 * 1024 * 1024,) * 2)

          def preflight_central_directory(path):
              eocd_struct = struct.Struct("<4s4H2LH")
              zip64_locator_struct = struct.Struct("<4sLQL")
              zip64_eocd_struct = struct.Struct("<4sQ2H2L4Q")
              file_size = os.path.getsize(path)
              if file_size < eocd_struct.size:
                  raise ValueError("archive is too short")

              with open(path, "rb") as archive_file:
                  tail_size = min(file_size, eocd_struct.size + 65_535)
                  archive_file.seek(file_size - tail_size)
                  tail = archive_file.read(tail_size)
                  relative_eocd = tail.rfind(b"PK\x05\x06")
                  if relative_eocd < 0:
                      raise ValueError("archive has no end-of-central-directory record")
                  eocd_offset = file_size - tail_size + relative_eocd
                  eocd = eocd_struct.unpack_from(tail, relative_eocd)
                  (
                      _,
                      disk_number,
                      central_directory_disk,
                      entries_on_disk,
                      entry_count,
                      central_directory_size,
                      central_directory_offset,
                      comment_length,
                  ) = eocd
                  if eocd_offset + eocd_struct.size + comment_length != file_size:
                      raise ValueError("archive has a malformed end record")

                  central_directory_end = eocd_offset
                  if (
                      entries_on_disk == 0xFFFF
                      or entry_count == 0xFFFF
                      or central_directory_size == 0xFFFFFFFF
                      or central_directory_offset == 0xFFFFFFFF
                  ):
                      locator_offset = eocd_offset - zip64_locator_struct.size
                      if locator_offset < 0:
                          raise ValueError("ZIP64 locator is missing")
                      archive_file.seek(locator_offset)
                      locator = archive_file.read(zip64_locator_struct.size)
                      signature, zip64_disk, zip64_offset, disk_count = zip64_locator_struct.unpack(locator)
                      if signature != b"PK\x06\x07" or zip64_disk != 0 or disk_count != 1:
                          raise ValueError("multi-disk ZIP64 archives are unsupported")
                      archive_file.seek(zip64_offset)
                      record = archive_file.read(zip64_eocd_struct.size)
                      (
                          signature,
                          record_size,
                          _,
                          _,
                          disk_number,
                          central_directory_disk,
                          entries_on_disk,
                          entry_count,
                          central_directory_size,
                          central_directory_offset,
                      ) = zip64_eocd_struct.unpack(record)
                      if (
                          signature != b"PK\x06\x06"
                          or record_size < 44
                          or zip64_offset + 12 + record_size != locator_offset
                      ):
                          raise ValueError("ZIP64 end record is malformed")
                      central_directory_end = zip64_offset

                  if disk_number != 0 or central_directory_disk != 0 or entries_on_disk != entry_count:
                      raise ValueError("multi-disk ZIP archives are unsupported")
                  if entry_count > 100_000:
                      raise ValueError("archive has too many entries")
                  if central_directory_offset + central_directory_size > central_directory_end:
                      raise ValueError("central directory extends beyond its end record")

                  archive_file.seek(central_directory_offset)
                  remaining = central_directory_size
                  for entry_index in range(entry_count):
                      if remaining < 46:
                          raise ValueError(f"central-directory entry {entry_index} is truncated")
                      header = archive_file.read(46)
                      if len(header) != 46 or header[:4] != b"PK\x01\x02":
                          raise ValueError(f"central-directory entry {entry_index} is malformed")
                      name_length, extra_length, entry_comment_length = struct.unpack_from("<3H", header, 28)
                      variable_length = name_length + extra_length + entry_comment_length
                      if variable_length > remaining - 46:
                          raise ValueError(f"central-directory entry {entry_index} is truncated")
                      archive_file.seek(variable_length, os.SEEK_CUR)
                      remaining -= 46 + variable_length
                  if remaining != 0:
                      raise ValueError("central-directory entry count is inconsistent")
              return entry_count

          expected_entry_count = preflight_central_directory(archive)
          with zipfile.ZipFile(archive) as zip_file:
              entries = zip_file.infolist()
              if len(entries) != expected_entry_count:
                  raise ValueError("central-directory entry count changed during parsing")

              selected = []
              for entry_index, entry in enumerate(entries):
                  raw_name = entry.filename
                  normalized_name = raw_name.replace("\\", "/")
                  path = PurePosixPath(normalized_name)
                  if (
                      "\0" in raw_name
                      or path.is_absolute()
                      or ".." in path.parts
                      or (path.parts and len(path.parts[0]) == 2 and path.parts[0][1] == ":")
                  ):
                      raise ValueError(f"archive entry {entry_index} has an unsafe path")

                  mode = (entry.external_attr >> 16) & 0xFFFF
                  file_type = stat.S_IFMT(mode)
                  if file_type not in (0, stat.S_IFREG, stat.S_IFDIR):
                      raise ValueError(f"archive entry {entry_index} has an unsupported type")

                  if not entry.is_dir() and normalized_name.lower().endswith(".binlog"):
                      selected.append(entry)

              os.makedirs(destination, exist_ok=True)
              extracted_bytes = 0
              for index, entry in enumerate(selected):
                  target = os.path.join(destination, f"{index}.binlog")
                  with zip_file.open(entry, "r") as source, open(target, "xb") as output:
                      while chunk := source.read(1024 * 1024):
                          extracted_bytes += len(chunk)
                          if extracted_bytes > max_unzip_bytes:
                              raise ValueError("extracted binlogs exceed the per-artifact limit")
                          output.write(chunk)

          print(len(selected))
          '
            )
            extract_rc=$?
            if [ "${extract_rc}" -ne 0 ]; then
              rm -rf /tmp/ax
              echo "::warning::Skipping ${safe_name}: secure extraction failed or timed out (exit ${extract_rc})."; continue
            fi
            if ! printf '%s' "${EXTRACTED}" | grep -qE '^[0-9]+$' || [ "${EXTRACTED}" -eq 0 ]; then
              rm -rf /tmp/ax
              echo "::warning::Skipping ${safe_name}: secure extraction produced no binlogs."; continue
            fi
            # Consume the budget only once the archive actually extracted, so a
            # skipped leg can't exhaust it and force later legs to be dropped.
            TOTAL_BYTES=$((TOTAL_BYTES + UNCOMP))
            i=0
            leg_staged=0
            count_before_leg="${count}"
            while IFS= read -r bl; do
              [ -f "${bl}" ] || continue
              # Prefixing with the artifact index (`ai`) and per-file counter
              # (`i`) keeps destinations unique, so neither a cross-artifact
              # sanitize collision nor same-basename entries can overwrite a
              # staged binlog. `safe_name` is kept only for readability.
              dest="/tmp/binlogs/${ai}_${i}_${safe_name}.binlog"
              # Count only a successful copy — `set +e` is on, so a failed `cp`
              # must not inflate the counts.
              if cp "${bl}" "${dest}"; then
                count=$((count + 1))
                i=$((i + 1))
                leg_staged=$((leg_staged + 1))
              else
                echo "::warning::Failed to stage ${bl}; skipping."
              fi
            done < <(find /tmp/ax -type f -name '*.binlog')
            if [ "${leg_staged}" -ne "${EXTRACTED}" ]; then
              find /tmp/binlogs -maxdepth 1 -type f -name "${ai}_*_${safe_name}.binlog" -delete
              count="${count_before_leg}"
              echo "::warning::Skipping ${safe_name}: staged ${leg_staged} of ${EXTRACTED} extracted binlogs."; continue
            fi
            staged_legs=$((staged_legs + 1))
          done
          echo "Extracted ${count} binlog(s) from ${staged_legs}/${#names[@]} selected artifacts into /tmp/binlogs:"
          ls -la /tmp/binlogs || true
          [ "${count}" -eq 0 ] && { echo "::warning::No *.binlog found in the selected build-log artifacts of build ${BUILD_ID}."; emit_none; }
          # Fail CLOSED on a partial selected set: a missing artifact could be
          # the failed attempt that contains the root cause.
          if [ "${staged_legs}" -ne "${#names[@]}" ]; then
            echo "::warning::Only ${staged_legs} of ${#names[@]} selected build-log artifacts produced a usable binlog; skipping incomplete failed-job data."
            emit_none
          fi

          # The download/extract loop above can take minutes. Re-read the PR
          # head right before activating and fail CLOSED if it moved or can't
          # be resolved: a force-push during that window would otherwise leave
          # the analyzed binlog stale relative to the current diff (queued
          # writes are gated again and inline comments pin this head).
          LATEST_PR=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
          LATEST_HEAD=$(printf '%s' "${LATEST_PR}" | jq -r '.head.sha // empty')
          LATEST_MERGE=$(printf '%s' "${LATEST_PR}" | jq -r '.merge_commit_sha // empty')
          if [ -z "${LATEST_HEAD}" ] || [ "${LATEST_HEAD}" != "${HEAD_SHA}" ]; then
            echo "::warning::PR #${PR_NUMBER} head changed during artifact download ('${HEAD_SHA}' -> '${LATEST_HEAD}') or could not be re-resolved; skipping to avoid posting stale-build suggestions against the new diff."
            emit_none
          fi
          if [ -z "${LATEST_MERGE}" ]; then
            echo "::warning::Could not re-resolve PR #${PR_NUMBER}'s merge revision after artifact download; skipping."
            emit_none
          fi
          # The base branch may also have advanced during the download.
          if [ "${LATEST_MERGE}" != "${BUILD_MERGE_SHA}" ]; then
            echo "::warning::PR #${PR_NUMBER} merge revision changed during artifact download ('${BUILD_MERGE_SHA}' -> '${LATEST_MERGE}'); skipping stale merge."
            emit_none
          fi

          {
            echo "binlog-found=true"
            echo "pr-number=${PR_NUMBER}"
            echo "pr-head-sha=${HEAD_SHA}"
            echo "pr-merge-sha=${BUILD_MERGE_SHA}"
            echo "ado-build-id=${BUILD_ID}"
            echo "ado-build-url=${ADO_BUILD_UI}?buildId=${BUILD_ID}"
          } >> "$GITHUB_OUTPUT"

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
