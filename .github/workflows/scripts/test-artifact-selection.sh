#!/usr/bin/env bash
# Replays the artifact selector in fetch-build-binlogs.sh against real
# roslyn-CI builds, so a refactor cannot silently change which failed-job
# artifacts get analyzed. Network-dependent; run manually.
set -uo pipefail

ADO_API="https://dev.azure.com/dnceng-public/public/_apis"
SCRIPT_PATH="$(cd "$(dirname "$0")" && pwd)/fetch-build-binlogs.sh"

# Extract the selector from the real script so this test cannot drift from it.
# It is sourced rather than eval'd: eval would strip the backslash-escaped
# spaces in the `[[ =~ ]]` pattern and silently change the regex.
SELECTOR_FILE=$(mktemp)
trap 'rm -f "${SELECTOR_FILE}"' EXIT
awk '/^# --- 6\. Select/,/^# --- 7\. Download/' "${SCRIPT_PATH}" | sed '$d' > "${SELECTOR_FILE}"

failures=0

# On Windows (Git Bash) jq writes CRLF, which would leave a stray CR on every
# artifact name and break the selector's `... Logs$` anchor. Runners are Linux,
# where jq writes LF, so strip CR here rather than in the production script.
REAL_JQ=$(command -v jq)
jq() { "${REAL_JQ}" "$@" | tr -d '\r'; }

replay() {
  local build_id="$1" expected="$2" label="$3"
  local result_file got
  result_file=$(mktemp)
  (
    BUILD_ID="${build_id}"
    # These are consumed by the sourced selector, which shellcheck can't see.
    # shellcheck disable=SC2034
    timeline_json=$(curl -sSL --fail --retry 3 "${ADO_API}/build/builds/${BUILD_ID}/timeline?api-version=7.1")
    # shellcheck disable=SC2034
    artifacts_json=$(curl -sSL --fail --retry 3 "${ADO_API}/build/builds/${BUILD_ID}/artifacts?api-version=7.1")
    # In the real script `emit_none` exits after reporting no usable data. The
    # count goes to a file because the selector's own stdout is discarded.
    emit_none() { echo 0 > "${result_file}"; exit 0; }
    # shellcheck disable=SC1090
    source "${SELECTOR_FILE}" >/dev/null 2>&1
    # `names` is defined by the sourced selector.
    # shellcheck disable=SC2154
    echo "${#names[@]}" > "${result_file}"
  )
  got=$(cat "${result_file}" 2>/dev/null)
  rm -f "${result_file}"

  if [ "${got}" = "${expected}" ]; then
    echo "PASS  build ${build_id} (${label}): selected ${got}"
  else
    echo "FAIL  build ${build_id} (${label}): selected '${got}', expected ${expected}"
    failures=$((failures + 1))
  fi
}

# Real build with retried correctness/determinism legs: eight exact artifacts.
replay 1567331 8 "build-leg failures with retries"
# Helix-monitor-only failures: no build-leg artifact should be selected.
replay 1567702 0 "Monitor Helix Jobs only"
replay 1567757 0 "Monitor Helix Jobs only"

echo
if [ "${failures}" -ne 0 ]; then
  echo "${failures} selector replay(s) FAILED"
  exit 1
fi
echo "all selector replays passed"
