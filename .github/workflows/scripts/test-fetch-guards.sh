#!/usr/bin/env bash
# Focused checks for the two guards added in response to review:
#   1. GITHUB_OUTPUT must be set AND writable.
#   2. A remaining compressed allowance below MIN_ZIP_BYTES stops downloads,
#      so `ulimit -f $((ZIP_CAP / 512))` can never floor to 0 blocks.
set -u
SCRIPT="$1"
WORKFLOW="${2:-.github/workflows/build-failure-analysis.agent.md}"
fail=0
check() { if [ "$2" = "$3" ]; then echo "PASS $1"; else echo "FAIL $1: got '$2' want '$3'"; fail=1; fi; }

# --- 1. GITHUB_OUTPUT guard -------------------------------------------------
out=$(GITHUB_OUTPUT="" bash "${SCRIPT}" 2>&1); rc=$?
check "unset GITHUB_OUTPUT exits 1" "${rc}" "1"
case "${out}" in *"not writable"*) echo "PASS unset message";; *) echo "FAIL unset message: ${out}"; fail=1;; esac

ro="/nonexistent-dir-$$/out.txt"
out=$(GITHUB_OUTPUT="${ro}" bash "${SCRIPT}" 2>&1); rc=$?
check "unwritable GITHUB_OUTPUT exits 1" "${rc}" "1"
case "${out}" in *"not writable"*) echo "PASS unwritable message";; *) echo "FAIL unwritable message: ${out}"; fail=1;; esac

writable="/tmp/ok_$$"; : > "${writable}"
out=$(GITHUB_OUTPUT="${writable}" bash "${SCRIPT}" 2>&1); rc=$?
if [ "${rc}" -eq 1 ] && printf '%s' "${out}" | grep -q "not writable"; then
  echo "FAIL writable GITHUB_OUTPUT was rejected"; fail=1
else
  echo "PASS writable GITHUB_OUTPUT accepted (exit ${rc})"
fi
# The guard probes with a zero-byte append, so the only content in the outputs
# file comes from the script's own emit path, never from the probe itself.
case "$(cat "${writable}")" in
  ""|*"binlog-found="*) echo "PASS guard leaves no probe residue";;
  *) echo "FAIL unexpected outputs content: $(cat "${writable}")"; fail=1;;
esac
rm -f "${writable}"

# --- 2. ZIP_CAP clamp and the ulimit block count ----------------------------
MAX_ZIP_BYTES=$(grep -m1 '^MAX_ZIP_BYTES=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')
MAX_TOTAL_ZIP_BYTES=$(grep -m1 '^MAX_TOTAL_ZIP_BYTES=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')

clamp() { # $1 = bytes already downloaded -> "break" or the ulimit block count
  local total="$1" cap allowance
  cap="${MAX_ZIP_BYTES}"
  allowance=$((MAX_TOTAL_ZIP_BYTES - total))
  [ "${allowance}" -lt "${cap}" ] && cap="${allowance}"
  if [ "${cap}" -le 0 ]; then echo "break"; else echo $(( (cap + 511) / 512 )); fi
}

check "fresh budget clamps to the per-artifact cap" "$(clamp 0)" "$(((MAX_ZIP_BYTES + 511) / 512))"
check "partial budget clamps to the remainder" "$(clamp $((MAX_TOTAL_ZIP_BYTES - 1048576)))" "$((1048576 / 512))"
check "exhausted budget stops downloads" "$(clamp "${MAX_TOTAL_ZIP_BYTES}")" "break"
check "over-spent budget stops downloads" "$(clamp $((MAX_TOTAL_ZIP_BYTES + 5000)))" "break"

# A small archive must still be attempted while any budget remains: stopping
# early would drop a leg that fits and disable the analysis for it.
check "512 KB remaining still downloads" "$(clamp $((MAX_TOTAL_ZIP_BYTES - 524288)))" "$((524288 / 512))"
check "1 KB remaining still downloads" "$(clamp $((MAX_TOTAL_ZIP_BYTES - 1024)))" "2"

# The regression the review found: every accepted cap must give a non-zero
# ulimit, or every write fails and the artifact is dropped after being paid for.
for spent in 0 1000 $((MAX_TOTAL_ZIP_BYTES / 2)) $((MAX_TOTAL_ZIP_BYTES - 1)) \
             $((MAX_TOTAL_ZIP_BYTES - 100)) $((MAX_TOTAL_ZIP_BYTES - 511)); do
  r=$(clamp "${spent}")
  if [ "${r}" != "break" ] && [ "${r}" -le 0 ]; then
    echo "FAIL accepted cap yields ulimit 0 at spent=${spent}"; fail=1
  fi
done
echo "PASS no accepted cap yields a zero ulimit"

# --- 3. The download phase must fit inside the job timeout ------------------
JOB_TIMEOUT_MIN=$(grep -m1 -E '^\s+timeout-minutes:' "${WORKFLOW}" | awk '{print $2}')
DOWNLOAD_BUDGET=$(grep -m1 '^DOWNLOAD_BUDGET=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')
MAX_ATTEMPT_SECONDS=$(grep -m1 '^MAX_ATTEMPT_SECONDS=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')

# `--max-time` is per attempt, so the worst case is the whole download window
# plus one final attempt that started just inside it, and the metadata calls.
META_WORST=$((3 * (40 + 20)))
WORST=$((DOWNLOAD_BUDGET + MAX_ATTEMPT_SECONDS + META_WORST))
check "job declares a timeout" "$([ -n "${JOB_TIMEOUT_MIN}" ] && echo yes)" "yes"
check "worst-case fetch fits in the job timeout" \
  "$([ "${WORST}" -lt $((JOB_TIMEOUT_MIN * 60)) ] && echo yes)" "yes"
echo "  (worst case ${WORST}s vs job timeout $((JOB_TIMEOUT_MIN * 60))s)"

# Every retrying curl must bound its whole retry window, not just one attempt.
# Strip comments and join line continuations so each curl is one logical line.
curl_cmds=$(sed 's/^[[:space:]]*#.*$//' "${SCRIPT}" | sed ':a;/\\$/{N;s/\\\n//;ba}' | grep 'curl -')
total_curl=$(printf '%s\n' "${curl_cmds}" | grep -c 'curl -')
bounded=$(printf '%s\n' "${curl_cmds}" | grep -c -- '--retry-max-time')
retrying=$(printf '%s\n' "${curl_cmds}" | grep -c -- '--retry [0-9]')
check "every curl invocation was found" "$([ "${total_curl}" -ge 2 ] && echo yes)" "yes"
check "every retrying curl bounds its retry window" "${bounded}" "${retrying}"
check "every curl retries" "${retrying}" "${total_curl}"

exit "${fail}"
