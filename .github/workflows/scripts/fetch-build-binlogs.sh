#!/usr/bin/env bash
# Collect the binary logs of a completed, failed Azure Pipelines `roslyn-CI` PR
# build so the analysis agent can read them. Nothing here builds or executes PR
# code — it only downloads published artifacts.
#
# Advisory and best-effort: any gap emits `binlog-found=false`, which leaves the
# rest of the workflow inert. The one thing it will not do is analyze a partial
# or stale picture, so a missing failed-job artifact or a moved PR revision
# fails closed instead.
#
# Build resolution depends on RESOLVE_MODE:
#   check_run  parse the build id out of CHECK_DETAILS_URL
#   dispatch   take DISPATCH_BUILD_ID verbatim
#   latest     query the PR's newest build and require it to be completed
#
# Required environment: RESOLVE_MODE, PR_NUMBER, GH_TOKEN, GH_AW_REPO, ADO_API,
# ADO_BUILD_UI, ADO_BUILD_DEFINITION_ID, BINLOG_DIR, SCRIPT_DIR, GITHUB_OUTPUT.

set +e
set +o pipefail

if [ -z "${GITHUB_OUTPUT}" ] || ! printf '' >> "${GITHUB_OUTPUT}" 2>/dev/null; then
  echo "::error::GITHUB_OUTPUT is unset or not writable; refusing to run without a way to emit step outputs." >&2
  exit 1
fi

emit_none() { echo "binlog-found=false" >> "$GITHUB_OUTPUT"; exit 0; }

# Fetch an Azure DevOps API document into ADO_DOC. A network failure or a
# non-JSON body is a data-resolution failure, not evidence that there is
# nothing to analyze, so it is reported as such instead of falling through to
# an empty `.records`/`.value` and a misleading "no failed jobs" warning.
# Sets a non-zero return rather than calling emit_none directly, because a call
# in a command substitution would only exit the subshell.
ado_get() {
  local what="$1" url="$2" rc tmp
  # `mktemp` rather than a fixed /tmp name: a predictable path is one
  # pre-created symlink -- or one collision with another job sharing the
  # runner -- away from being someone else's file. It costs nothing to not
  # care whether this box is ephemeral.
  tmp=$(mktemp) || {
    echo "::warning::Could not create a temporary file for the ${what}; treating as a data-resolution failure."
    return 1
  }
  # These are small JSON documents; cap them so a stalled endpoint fails in
  # seconds rather than hanging the job until its overall timeout. `--max-time`
  # is per attempt, so `--retry-max-time` is what actually bounds the call:
  # without it these few metadata fetches could cumulatively consume the job's
  # `timeout-minutes` on their own. The artifact download below sets its own,
  # much larger, budget.
  # Write to a file rather than capturing stdout: `curl --retry` can only rewind
  # seekable output, and command-substitution stdout is a pipe. A retry after a
  # partial or error body would append to it, so a *successful* retry would
  # yield two concatenated documents, `jq` would reject them, and the run would
  # be reported as a data-resolution failure. With `-o` curl truncates the file
  # before each attempt, so only the last response survives.
  timeout 60 curl -sSL --fail --retry 3 --connect-timeout 10 \
    --max-time 20 --retry-max-time 40 -o "${tmp}" "${url}"
  rc=$?
  ADO_DOC=$(cat "${tmp}" 2>/dev/null)
  rm -f "${tmp}"
  if [ "${rc}" -ne 0 ] || [ -z "${ADO_DOC}" ]; then
    echo "::warning::Could not fetch the ${what} from Azure DevOps (curl exit ${rc}); treating as a data-resolution failure."
    return 1
  fi
  if ! printf '%s' "${ADO_DOC}" | jq -e . >/dev/null 2>&1; then
    echo "::warning::Azure DevOps returned a non-JSON ${what}; treating as a data-resolution failure."
    return 1
  fi
  return 0
}

# --- 1. Validate the PR number ---------------------------------------------
# It is interpolated into GitHub API paths and into the `refs/pull/<n>/merge`
# comparison, and on dispatch and slash commands it is free-form input.
if ! printf '%s' "${PR_NUMBER}" | grep -qE '^[0-9]+$'; then
  echo "::warning::Resolved PR number '${PR_NUMBER}' is not numeric or empty; refusing."; emit_none
fi

# --- 2. Scope check: only PRs that roslyn-CI targets ------------------------
PR_JSON=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
BASE_REF=$(printf '%s' "${PR_JSON}" | jq -r '.base.ref // empty')
# An empty base ref means the API call failed, not that the PR is out of scope.
[ -z "${BASE_REF}" ] && { echo "::warning::Could not resolve the base ref for PR #${PR_NUMBER}; treating as a data-resolution failure."; emit_none; }
case "${BASE_REF}" in
  main|main-vs-deps|community|release/*|features/*|demos/*) echo "PR #${PR_NUMBER} base '${BASE_REF}' is in scope." ;;
  *) echo "::warning::PR #${PR_NUMBER} base '${BASE_REF}' is not targeted by roslyn-CI; skipping."; emit_none ;;
esac

# --- 3. Resolve and validate the Azure DevOps build id ----------------------
case "${RESOLVE_MODE}" in
  dispatch)
    BUILD_ID="${DISPATCH_BUILD_ID}"
    ;;
  check_run)
    # details_url looks like: .../_build/results?buildId=NNN&view=...
    BUILD_ID=$(printf '%s' "${CHECK_DETAILS_URL}" | grep -oE 'buildId=[0-9]+' | head -1 | cut -d= -f2)
    ;;
  latest)
    # Take the newest build regardless of status. If it is still running — e.g.
    # right after a force-push — skip rather than pair an older failure with the
    # PR's current head.
    ado_get "build list for PR #${PR_NUMBER}" \
      "${ADO_API}/build/builds?definitions=${ADO_BUILD_DEFINITION_ID}&branchName=refs/pull/${PR_NUMBER}/merge&queryOrder=queueTimeDescending&\$top=1&api-version=7.1" || emit_none
    builds_json="${ADO_DOC}"
    BUILD_ID=$(printf '%s' "${builds_json}" | jq -r '.value // [] | .[0].id // empty')
    BUILD_STATUS=$(printf '%s' "${builds_json}" | jq -r '.value // [] | .[0].status // empty')
    echo "Newest roslyn-CI build for PR #${PR_NUMBER}: id='${BUILD_ID}' status='${BUILD_STATUS}'"
    if [ -n "${BUILD_ID}" ] && [ "${BUILD_STATUS}" != "completed" ]; then
      echo "::warning::PR #${PR_NUMBER}'s newest roslyn-CI build (${BUILD_ID}) is still '${BUILD_STATUS}'; wait for it to finish."
      emit_none
    fi
    ;;
  *)
    echo "::warning::Unknown RESOLVE_MODE '${RESOLVE_MODE}'; refusing."; emit_none
    ;;
esac
# The id is interpolated into ADO API URLs, so require it to be purely numeric.
if ! printf '%s' "${BUILD_ID}" | grep -qE '^[0-9]+$'; then
  echo "::warning::Resolved ADO build id '${BUILD_ID}' is not numeric or empty; refusing."; emit_none
fi

# --- 4. Validate the build on every trigger path ---------------------------
# On `check_run` the build id comes from a payload we don't fully trust; on
# dispatch the build id and PR number are independent inputs. Either way the
# build must be roslyn-CI, must have failed, and must belong to this PR.
ado_get "details of build ${BUILD_ID}" "${ADO_API}/build/builds/${BUILD_ID}?api-version=7.1" || emit_none
build_json="${ADO_DOC}"
RESULT=$(printf '%s' "${build_json}" | jq -r '.result // empty')
DEF_ID=$(printf '%s' "${build_json}" | jq -r '.definition.id // empty')
SRC_BRANCH=$(printf '%s' "${build_json}" | jq -r '.sourceBranch // empty')
echo "ADO build ${BUILD_ID}: result='${RESULT}' definition='${DEF_ID}' sourceBranch='${SRC_BRANCH}'"
if [ "${DEF_ID}" != "${ADO_BUILD_DEFINITION_ID}" ]; then
  echo "::warning::ADO build ${BUILD_ID} is definition '${DEF_ID}', not roslyn-CI (${ADO_BUILD_DEFINITION_ID}); refusing."; emit_none
fi
if [ "${RESULT}" != "failed" ]; then
  echo "::warning::ADO build ${BUILD_ID} did not fail (result='${RESULT}'); nothing to analyze."; emit_none
fi
if [ "${SRC_BRANCH}" != "refs/pull/${PR_NUMBER}/merge" ]; then
  echo "::warning::ADO build ${BUILD_ID} sourceBranch '${SRC_BRANCH}' does not match PR #${PR_NUMBER}; refusing to avoid posting to the wrong PR."; emit_none
fi

# --- 5. Require the build to describe the PR's current revision ------------
# ADO builds GitHub's `refs/pull/<n>/merge`, so `sourceVersion` is the merge
# commit as of build time. Comparing it as well as the head catches a base
# branch that advanced while the PR head stayed put.
BUILD_PR_SHA=$(printf '%s' "${build_json}" | jq -r '.triggerInfo["pr.sourceSha"] // empty')
BUILD_MERGE_SHA=$(printf '%s' "${build_json}" | jq -r '.sourceVersion // empty')
CURRENT_HEAD=$(printf '%s' "${PR_JSON}" | jq -r '.head.sha // empty')
CURRENT_MERGE=$(printf '%s' "${PR_JSON}" | jq -r '.merge_commit_sha // empty')
if [ -z "${BUILD_PR_SHA}" ] || [ -z "${CURRENT_HEAD}" ] || [ -z "${BUILD_MERGE_SHA}" ] || [ -z "${CURRENT_MERGE}" ]; then
  echo "::warning::Could not resolve all build/current head and merge revisions; skipping to avoid analyzing a stale binlog."; emit_none
fi
if [ "${BUILD_PR_SHA}" != "${CURRENT_HEAD}" ]; then
  echo "::warning::Build ${BUILD_ID} analyzed '${BUILD_PR_SHA}' but PR #${PR_NUMBER} head is now '${CURRENT_HEAD}'; skipping stale build."; emit_none
fi
if [ "${BUILD_MERGE_SHA}" != "${CURRENT_MERGE}" ]; then
  echo "::warning::Build ${BUILD_ID} merge revision '${BUILD_MERGE_SHA}' but PR #${PR_NUMBER} current merge is '${CURRENT_MERGE}' (base advanced); skipping stale merge."; emit_none
fi
HEAD_SHA="${CURRENT_HEAD}"
echo "Analyzing build ${BUILD_ID} at PR head revision '${HEAD_SHA}'."

# --- 6. Select the log artifacts of failed or canceled jobs ----------------
# Roslyn publishes "<job> Attempt <N> Logs" for most jobs, with explicit
# exceptions for Source Build and the bootstrap-correctness leg. Bases are
# matched exactly, and every retry attempt is kept.
ado_get "timeline of build ${BUILD_ID}" "${ADO_API}/build/builds/${BUILD_ID}/timeline?api-version=7.1" || emit_none
timeline_json="${ADO_DOC}"
mapfile -t failed_job_names < <(
  printf '%s' "${timeline_json}" |
    jq -r '.records // [] | map(select(.type == "Job" and (.result == "failed" or .result == "canceled"))) | .[].name' |
    awk 'NF && !seen[$0]++'
)
[ "${#failed_job_names[@]}" -eq 0 ] && { echo "::warning::No failed or canceled jobs in the timeline for build ${BUILD_ID}."; emit_none; }

ado_get "artifact list of build ${BUILD_ID}" "${ADO_API}/build/builds/${BUILD_ID}/artifacts?api-version=7.1" || emit_none
artifacts_json="${ADO_DOC}"
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

# --- 7. Download and extract each selected artifact ------------------------
# Per-artifact compressed cap. Roslyn's `Correctness_Analyzers` log artifact is
# routinely ~600 MB, so this has to be well clear of that or the workflow
# silently skips exactly the correctness legs it exists to diagnose. Only one
# archive is on disk at a time (each is deleted before the next download), so
# this bounds peak zip disk use, not the sum across artifacts.
MAX_ZIP_BYTES=2147483648    # 2 GB compressed per artifact
MAX_TOTAL_BYTES=4294967296  # 4 GB extracted across all artifacts
# Raising the per-artifact cap would otherwise raise the worst-case number of
# bytes pulled over the network by the same factor, since nothing else bounds
# the sum across artifacts. Cap the total download too, and charge it *before*
# each transfer (see ZIP_CAP below) rather than after, so the last artifact
# can't start just under the limit and still pull a full MAX_ZIP_BYTES.
# This is a budget, not a byte-exact ceiling: `ulimit -f` works in 512-byte
# blocks and rounds up, so a transfer can overshoot its cap by up to 511 bytes
# before the size check below rejects it. That slack is under 512 bytes per
# selected artifact, so at any plausible artifact count it stays in the
# kilobytes — irrelevant next to a 3 GB budget, and far cheaper than the
# alternative of treating a small remaining allowance as exhausted and
# dropping a leg that would still fit.
MAX_TOTAL_ZIP_BYTES=3221225472  # 3 GB compressed downloaded across all artifacts
TOTAL_ZIP_BYTES=0
# One private scratch file for every download. A fixed /tmp name is a
# pre-created symlink, or a second job on the same runner, away from being
# someone else's file.
ZIP_TMP=$(mktemp) || { echo "::warning::Could not create a temporary file for downloads."; emit_none; }
# `--max-time` is per attempt, so `--retry N` multiplies it: the whole download
# phase, not one transfer, is what has to fit inside this job's
# `timeout-minutes: 15`. Give the loop a wall-clock deadline and derive every
# transfer's budget from what is left of it, so no combination of slow
# artifacts and retries can take the job down before the controlled no-op.
# `timeout` around each transfer makes the deadline hard, so the whole phase
# really is bounded by DOWNLOAD_BUDGET rather than by it plus a last attempt.
DOWNLOAD_BUDGET=420             # 7 minutes for all artifact transfers
MAX_ATTEMPT_SECONDS=120         # per attempt; the full set really takes ~30s
DOWNLOAD_DEADLINE=$(( $(date +%s) + DOWNLOAD_BUDGET ))
REMAINING_BYTES="${MAX_TOTAL_BYTES}"
mkdir -p "${BINLOG_DIR}"
# Only binlogs extracted by this run may be analyzed. Anything left in
# the directory by an earlier run on the same runner would otherwise be
# uploaded and attributed to this build.
rm -f "${BINLOG_DIR}"/*.binlog
count=0
staged_legs=0
ai=0
for name in "${names[@]}"; do
  ai=$((ai + 1))
  # `name` is PR-controlled artifact metadata; keep a sanitized copy for log
  # output and use the original only as a jq lookup key.
  safe_name=$(printf '%s' "${name}" | tr -c 'A-Za-z0-9._-' '_')
  url=$(printf '%s' "${artifacts_json}" | jq -r --arg n "${name}" '.value[] | select(.name==$n) | .resource.downloadUrl // empty')
  [ -z "${url}" ] && { echo "::warning::Skipping ${safe_name}: no download URL."; continue; }

  : > "${ZIP_TMP}"
  # Bound this transfer by whatever is left of the cumulative budget as well as
  # by the per-artifact cap, so the two limits together are a real ceiling on
  # bytes pulled rather than `MAX_TOTAL_ZIP_BYTES + MAX_ZIP_BYTES`.
  ZIP_CAP="${MAX_ZIP_BYTES}"
  ZIP_ALLOWANCE=$((MAX_TOTAL_ZIP_BYTES - TOTAL_ZIP_BYTES))
  [ "${ZIP_ALLOWANCE}" -lt "${ZIP_CAP}" ] && ZIP_CAP="${ZIP_ALLOWANCE}"
  if [ "${ZIP_CAP}" -le 0 ]; then
    echo "::warning::Cumulative compressed download budget ${MAX_TOTAL_ZIP_BYTES} is exhausted before ${safe_name}; stopping downloads."
    break
  fi
  # Bound this transfer by the time left as well, and never start one with no
  # time to finish in.
  TIME_LEFT=$(( DOWNLOAD_DEADLINE - $(date +%s) ))
  if [ "${TIME_LEFT}" -le 0 ]; then
    echo "::warning::Download time budget ${DOWNLOAD_BUDGET}s exhausted before ${safe_name}; stopping downloads."
    break
  fi
  ATTEMPT_SECONDS="${MAX_ATTEMPT_SECONDS}"
  [ "${TIME_LEFT}" -lt "${ATTEMPT_SECONDS}" ] && ATTEMPT_SECONDS="${TIME_LEFT}"
  # Download to a file, never a pipe: curl can only rewind seekable output, so
  # through a pipe a retried body is appended and a 503 page followed by a retry
  # yields a corrupt `<error page><zip>` that still exits 0.
  # `ulimit -f` is a disk backstop for responses that declare no Content-Length;
  # the size check below is authoritative. Round the block count UP so any
  # positive ZIP_CAP yields at least one block: dividing down would give a
  # 0-block limit for a sub-512-byte remainder and fail every write.
  # SIGXFSZ is ignored so hitting the cap is an ordinary write error.
  # `--retry-max-time` only gates whether curl may *start* another retry, so a
  # retry begun just inside it can still run a further `--max-time`. `timeout`
  # around the whole invocation is what makes DOWNLOAD_DEADLINE a real deadline
  # rather than a scheduling hint; a killed transfer is treated like any other
  # failed one and the leg is reported as missing, which fails closed.
  (
    ulimit -f $(( (ZIP_CAP + 511) / 512 ))
    trap '' XFSZ
    timeout "${TIME_LEFT}" curl -sSL --fail --retry 3 --retry-delay 2 \
      --connect-timeout 15 --max-time "${ATTEMPT_SECONDS}" \
      --retry-max-time "${TIME_LEFT}" -o "${ZIP_TMP}" "${url}"
  ) 2>/dev/null
  curl_rc=$?
  ZIP_BYTES=$(stat -c%s "${ZIP_TMP}" 2>/dev/null || echo 0)
  # Charge the budget with the bytes that actually crossed the wire, including
  # those of an artifact that is about to be skipped.
  TOTAL_ZIP_BYTES=$((TOTAL_ZIP_BYTES + ZIP_BYTES))
  if [ "${ZIP_BYTES}" -eq 0 ]; then
    echo "::warning::Skipping ${safe_name}: empty or failed download."; continue
  fi
  if [ "${ZIP_BYTES}" -ge "${ZIP_CAP}" ]; then
    echo "::warning::Skipping ${safe_name}: download reached the ${ZIP_CAP}-byte cap."; continue
  fi
  if [ "${curl_rc}" -ne 0 ]; then
    echo "::warning::Skipping ${safe_name}: download failed or was truncated (curl exit ${curl_rc})."; continue
  fi

  # The extractor writes generated `<ai>_<n>.binlog` names straight into
  # BINLOG_DIR and stops once it has written REMAINING_BYTES, so it bounds both
  # where bytes land and how many there are.
  extract_out=$(timeout 300 python3 "${SCRIPT_DIR}/extract-binlogs.py" \
    "${ZIP_TMP}" "${BINLOG_DIR}" "${ai}" "${REMAINING_BYTES}")
  extract_rc=$?
  if [ "${extract_rc}" -ne 0 ]; then
    # A failed or timed-out extraction may have left partial files behind.
    find "${BINLOG_DIR}" -maxdepth 1 -type f -name "${ai}_*.binlog" -delete
    echo "::warning::Skipping ${safe_name}: extraction failed or timed out (exit ${extract_rc})."; continue
  fi
  extracted=$(printf '%s' "${extract_out}" | awk '{print $1}')
  written=$(printf '%s' "${extract_out}" | awk '{print $2}')
  if ! printf '%s' "${extracted}" | grep -qE '^[0-9]+$' || [ "${extracted}" -eq 0 ]; then
    find "${BINLOG_DIR}" -maxdepth 1 -type f -name "${ai}_*.binlog" -delete
    echo "::warning::Skipping ${safe_name}: no binlogs found in the artifact."; continue
  fi

  # Charge the budget by bytes actually written rather than by any size the
  # archive declares about itself.
  REMAINING_BYTES=$((REMAINING_BYTES - written))
  [ "${REMAINING_BYTES}" -lt 0 ] && REMAINING_BYTES=0
  count=$((count + extracted))
  staged_legs=$((staged_legs + 1))
  echo "Extracted ${extracted} binlog(s) (${written} bytes) from ${safe_name}."
done
rm -f "${ZIP_TMP}"

echo "Extracted ${count} binlog(s) from ${staged_legs}/${#names[@]} selected artifacts into ${BINLOG_DIR}:"
ls -la "${BINLOG_DIR}" || true
[ "${count}" -eq 0 ] && { echo "::warning::No *.binlog found in the selected build-log artifacts of build ${BUILD_ID}."; emit_none; }
# Fail closed on a partial set: the artifact that failed to yield a binlog could
# be the attempt holding the root cause.
if [ "${staged_legs}" -ne "${#names[@]}" ]; then
  echo "::warning::Only ${staged_legs} of ${#names[@]} selected artifacts produced a usable binlog; skipping incomplete failed-job data."
  emit_none
fi

# --- 8. Re-check the revision after a download that can take minutes -------
# A force-push or base advance during the download would leave the analyzed
# binlogs stale relative to the diff that inline comments are pinned to.
LATEST_PR=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
LATEST_HEAD=$(printf '%s' "${LATEST_PR}" | jq -r '.head.sha // empty')
LATEST_MERGE=$(printf '%s' "${LATEST_PR}" | jq -r '.merge_commit_sha // empty')
if [ -z "${LATEST_HEAD}" ] || [ "${LATEST_HEAD}" != "${HEAD_SHA}" ]; then
  echo "::warning::PR #${PR_NUMBER} head changed during download ('${HEAD_SHA}' -> '${LATEST_HEAD}') or could not be re-resolved; skipping."
  emit_none
fi
if [ -z "${LATEST_MERGE}" ] || [ "${LATEST_MERGE}" != "${BUILD_MERGE_SHA}" ]; then
  echo "::warning::PR #${PR_NUMBER} merge revision changed during download ('${BUILD_MERGE_SHA}' -> '${LATEST_MERGE}') or could not be re-resolved; skipping."
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
