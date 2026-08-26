---
name: "Build Failure Analysis"
description: >-
  When the Azure Pipelines PR build (`roslyn-CI`) fails, downloads the binary
  logs from its failed or canceled jobs — it does NOT rebuild — and delegates
  to the `build-failure-analyst` agent, which queries the binlogs live via the
  containerized `binlog-mcp` MCP server to identify root causes, post a PR
  comment summarizing them, and attach inline `suggestion` blocks tied to the
  diff.

# This workflow is **advisory**, not gating, and it performs **no build of its
# own**. Roslyn's authoritative PR build runs on Azure DevOps
# (dnceng-public/public, pipeline "roslyn-CI", definitionId 95) and publishes
# each build job's binary logs in a leg-specific build-log artifact. When
# that build's GitHub check reports failure, this workflow uses the Azure
# DevOps timeline to select the artifacts for failed or canceled jobs
# (anonymously — dnceng-public/public is a public project), then the agent
# analyses whichever selected leg(s) contain errors. Reusing the binlogs avoids
# a duplicate build: the analysis pipeline only downloads build artifacts
# (data) and reads them — it does **not** build or execute PR code. (gh-aw's
# generated agent job **does** check out the repository — via
# `actions/checkout` — to load the workflow's own agent configuration; that
# checkout is for tooling only and uses the event's ref, **not** the PR head,
# so no PR code is built or executed.)

on:
  # `check_run` fires for every check on a commit, so the `fetch-binlog` job
  # below filters tightly to the `roslyn-CI` build check reporting failure.
  check_run:
    types: [completed]
  # Advisory analysis should run for **every** failing PR — including external
  # contributors' PRs, which are the most likely to break the build. Disable
  # gh-aw's default author-association gate (which would otherwise skip
  # non-write-access actors, and on `check_run` the actor is the pipeline app
  # anyway). This is safe here: the workflow only reads a public binlog and
  # posts advisory comments — it never builds or executes PR code.
  roles: all
  # Manual entry point for reruns / testing: analyse a specific Azure DevOps
  # build id and post to a specific PR.
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

# Activate (and run the agent) only when the fetch job retrieved at least one
# binlog. When `check_run` fires for an unrelated / passing check the
# fetch-binlog job is skipped, its output is empty, and this cascades into a
# skipped agent — no AI calls on anything but a real `roslyn-CI` failure whose
# PR targets an in-scope base branch.
if: needs.fetch-binlog.outputs.binlog-found == 'true'

# Least-privilege for the workflow/agent jobs. The agent runs read-only; it
# does NOT post directly. All PR writes (summary comment + inline review
# suggestions) go through gh-aw **safe-outputs**, which the compiler emits as
# a separate `safe_outputs` job granted `pull-requests: write` + `issues:
# write` in the generated lock. Keep `pull-requests: read` here so the AI
# agent job stays least-privilege — do NOT raise it to `write`, that would
# hand PR-write scope to the agent job unnecessarily.
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
  # Only real `roslyn-CI` check_run events (and manual dispatch for a PR) use a
  # PR/head-scoped group, so a newer analysis supersedes an in-progress one for
  # the same PR. Every OTHER completed check_run on the PR would otherwise land
  # in the same group and — with cancel-in-progress — abort the running real
  # analysis, so those get a unique per-run group that collides with nothing.
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

# Live binlog access for the agent. The build-leg binlogs are downloaded from
# Azure DevOps by the fetch-binlog job into a directory, uploaded as an
# artifact, downloaded by the agent job to `/tmp/binlogs`, and mounted
# read-only into this container at `/data/binlogs` by the gh-aw MCP gateway.
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

# Custom job that reuses the binlogs from the failed Azure DevOps build instead
# of rebuilding. It resolves the ADO build id (from the check details URL or
# the dispatch input), verifies the PR targets an in-scope base branch,
# selects build-log artifacts matching failed or canceled timeline jobs,
# extracts each selected leg's `*.binlog`, and uploads them for the agent job.
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
      - name: Download binlogs from the failed Azure Pipelines build
        id: fetch
        shell: bash
        env:
          GH_TOKEN: ${{ github.token }}
          GH_AW_REPO: ${{ github.repository }}
          ADO_API: "https://dev.azure.com/dnceng-public/public/_apis"
          ADO_BUILD_UI: "https://dev.azure.com/dnceng-public/public/_build/results"
          # roslyn-CI pipeline definition id in dnceng-public/public (used to
          # validate a dispatched build id belongs to the right pipeline).
          ADO_BUILD_DEFINITION_ID: "95"
          EVENT_NAME: ${{ github.event_name }}
          CHECK_DETAILS_URL: ${{ github.event.check_run.details_url }}
          CHECK_HEAD_SHA: ${{ github.event.check_run.head_sha }}
          CHECK_PR_NUMBER: ${{ github.event.check_run.pull_requests[0].number }}
          DISPATCH_BUILD_ID: ${{ inputs['ado-build-id'] }}
          DISPATCH_PR_NUMBER: ${{ inputs['pr-number'] }}
        run: |
          # Advisory + best-effort: on any gap emit binlog-found=false and the
          # agent pipeline stays inert.
          set +e
          set +o pipefail
          emit_none() { echo "binlog-found=false" >> "$GITHUB_OUTPUT"; exit 0; }

          # --- 1. Resolve the Azure DevOps build id ---
          if [ "${EVENT_NAME}" = "workflow_dispatch" ]; then
            BUILD_ID="${DISPATCH_BUILD_ID}"
          else
            # details_url looks like: .../_build/results?buildId=NNN&view=...
            BUILD_ID=$(printf '%s' "${CHECK_DETAILS_URL}" | grep -oE 'buildId=[0-9]+' | head -1 | cut -d= -f2)
          fi
          echo "Azure DevOps build id: '${BUILD_ID}'"
          [ -z "${BUILD_ID}" ] && { echo "::warning::Could not resolve an ADO build id."; emit_none; }
          # The build id feeds directly into ADO API URLs below; require it to
          # be purely numeric (esp. on workflow_dispatch, where it is free-form
          # input) so a malformed value can't alter the request path/query.
          if ! printf '%s' "${BUILD_ID}" | grep -qE '^[0-9]+$'; then
            echo "::warning::Resolved ADO build id '${BUILD_ID}' is not numeric; refusing."; emit_none
          fi

          # Fetch the build metadata once, up front: it is the authoritative
          # source for the definition/result/revision validated in step 4.
          # The PR number remains event-owned so safe outputs can be bound to
          # the same trusted value before the fetch job runs.
          build_json=$(curl -sSL --retry 3 "${ADO_API}/build/builds/${BUILD_ID}?api-version=7.1")
          RESULT=$(printf '%s' "${build_json}" | jq -r '.result // empty')
          DEF_ID=$(printf '%s' "${build_json}" | jq -r '.definition.id // empty')
          SRC_BRANCH=$(printf '%s' "${build_json}" | jq -r '.sourceBranch // empty')

          # --- 2. Resolve the PR number + head SHA ---
          if [ "${EVENT_NAME}" = "workflow_dispatch" ]; then
            PR_NUMBER="${DISPATCH_PR_NUMBER}"
            HEAD_SHA=""
          else
            # Safe outputs are bound to check_run.pull_requests[0] below. Use
            # that same event-owned PR number here and fail closed when it is
            # absent; the sourceBranch validation in step 4 ensures the ADO
            # build belongs to this exact PR before any analysis can run.
            PR_NUMBER="${CHECK_PR_NUMBER}"
            HEAD_SHA="${CHECK_HEAD_SHA}"
          fi
          [ -z "${PR_NUMBER}" ] && { echo "::warning::Could not resolve a PR number."; emit_none; }
          # PR_NUMBER feeds `gh api .../pulls/<n>` and the `refs/pull/<n>/merge`
          # comparison; require it numeric so a malformed value can't reach the
          # GitHub API path (traversal-like input) or skew the branch match.
          if ! printf '%s' "${PR_NUMBER}" | grep -qE '^[0-9]+$'; then
            echo "::warning::Resolved PR number '${PR_NUMBER}' is not numeric; refusing."; emit_none
          fi

          # --- 3. Scope check: only analyse PRs targeted by roslyn-CI -------
          PR_JSON=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
          BASE_REF=$(printf '%s' "${PR_JSON}" | jq -r '.base.ref // empty')
          # An empty BASE_REF means the `gh api` call failed or returned no
          # data (rate limit / transient error), NOT that the PR targets an
          # out-of-scope branch. Treat it as a data-resolution failure so a
          # valid PR isn't silently skipped and misreported as base '' out of
          # scope.
          [ -z "${BASE_REF}" ] && { echo "::warning::Could not resolve the base ref for PR #${PR_NUMBER} (GitHub API returned no data); treating as a data-resolution failure, not an out-of-scope branch."; emit_none; }
          [ -z "${HEAD_SHA}" ] && HEAD_SHA=$(printf '%s' "${PR_JSON}" | jq -r '.head.sha // empty')
          case "${BASE_REF}" in
            main|main-vs-deps|community|release/*|features/*|demos/*) echo "PR #${PR_NUMBER} base '${BASE_REF}' is in scope." ;;
            *) echo "::warning::PR #${PR_NUMBER} base '${BASE_REF}' is not targeted by roslyn-CI; skipping."; emit_none ;;
          esac

          # --- 4. Validate the build for EVERY trigger (not just dispatch):
          #        it must be the roslyn-CI definition (95), have failed, and
          #        belong to this PR (sourceBranch == refs/pull/<PR>/merge).
          #        For `check_run` the build id is parsed from a check payload
          #        we don't fully trust; for dispatch the build id and PR
          #        number are independent inputs. Validating on both paths
          #        prevents downloading an unrelated build or posting its
          #        analysis to the wrong PR.
          echo "ADO build ${BUILD_ID}: result='${RESULT}' definition='${DEF_ID}' sourceBranch='${SRC_BRANCH}'"
          if [ "${DEF_ID}" != "${ADO_BUILD_DEFINITION_ID}" ]; then
            echo "::warning::ADO build ${BUILD_ID} is definition '${DEF_ID}', not roslyn-CI (${ADO_BUILD_DEFINITION_ID}); refusing."; emit_none
          fi
          if [ "${RESULT}" != "failed" ]; then
            echo "::warning::ADO build ${BUILD_ID} did not fail (result='${RESULT}'); nothing to analyze."; emit_none
          fi
          if [ "${SRC_BRANCH}" != "refs/pull/${PR_NUMBER}/merge" ]; then
            echo "::warning::ADO build ${BUILD_ID} sourceBranch '${SRC_BRANCH}' does not match PR #${PR_NUMBER} (refs/pull/${PR_NUMBER}/merge); refusing to avoid posting to the wrong PR."; emit_none
          fi

          # Require the build's analyzed revision to equal the PR's CURRENT
          # head. Inline safe outputs are pinned to the verified head, and all
          # queued writes are revision-gated again before application. Skip a
          # stale revision rather than report obsolete results.
          BUILD_PR_SHA=$(printf '%s' "${build_json}" | jq -r '.triggerInfo["pr.sourceSha"] // empty')
          CURRENT_HEAD=$(printf '%s' "${PR_JSON}" | jq -r '.head.sha // empty')
          # ADO builds GitHub's `refs/pull/<n>/merge` ref, so build_json.sourceVersion
          # is the merge commit GitHub produced at build time and equals the PR's
          # `merge_commit_sha` then. If the base branch advances (even with the PR
          # head unchanged) GitHub recomputes that merge and merge_commit_sha
          # changes, so this catches base-advance staleness the head check misses.
          BUILD_MERGE_SHA=$(printf '%s' "${build_json}" | jq -r '.sourceVersion // empty')
          CURRENT_MERGE=$(printf '%s' "${PR_JSON}" | jq -r '.merge_commit_sha // empty')
          # Fail CLOSED unless both head and merge revisions are known. The
          # merge revision is required to detect a moved base branch even when
          # the PR head itself is unchanged.
          if [ -z "${BUILD_PR_SHA}" ] || [ -z "${CURRENT_HEAD}" ] || [ -z "${BUILD_MERGE_SHA}" ] || [ -z "${CURRENT_MERGE}" ]; then
            echo "::warning::Could not resolve all build/current head and merge revisions; skipping to avoid analyzing a stale binlog against the current diff."
            emit_none
          fi
          if [ "${BUILD_PR_SHA}" != "${CURRENT_HEAD}" ]; then
            echo "::warning::Build ${BUILD_ID} analyzed revision '${BUILD_PR_SHA}' but PR #${PR_NUMBER} head is now '${CURRENT_HEAD}'; skipping stale build (a newer build/check will cover the current revision)."
            emit_none
          fi
          # A difference means the base branch moved since the build.
          if [ "${BUILD_MERGE_SHA}" != "${CURRENT_MERGE}" ]; then
            echo "::warning::Build ${BUILD_ID} merge revision '${BUILD_MERGE_SHA}' but PR #${PR_NUMBER} current merge is '${CURRENT_MERGE}' (base branch advanced); skipping stale merge."
            emit_none
          fi
          # Consistent now: build revision == current PR head. Use it for
          # permalinks so they line up with the inline comments' diff target.
          HEAD_SHA="${CURRENT_HEAD}"
          echo "Analyzing build ${BUILD_ID} at PR head revision '${HEAD_SHA}'."

          # --- 5. Download failed-job build-log artifacts and binlogs -------
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

# Steps that run in the agent job. Because the top-level `if:` gates activation
# on `needs.fetch-binlog.outputs.binlog-found == 'true'`, these only run once
# binlogs have been retrieved from the failed Azure DevOps build.
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
  # Bind writes to the PR number in the trusted trigger rather than allowing
  # untrusted binlog/source content to choose an arbitrary repository target.
  # The fetch job uses the same value and verifies that the ADO build's
  # sourceBranch belongs to it before the agent can run.
  report-failure-as-issue: false
  add-comment:
    max: 1
    target: ${{ github.event.check_run.pull_requests[0].number || inputs['pr-number'] }}
    hide-older-comments: true
  create-pull-request-review-comment:
    max: 25
    target: ${{ github.event.check_run.pull_requests[0].number || inputs['pr-number'] }}
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
