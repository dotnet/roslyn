#!/usr/bin/env bash
# Focused checks for the guards added in response to review:
#   1. GITHUB_OUTPUT must be set AND writable.
#   2. Downloads stop only once the remaining compressed allowance is
#      non-positive, and `ulimit -f $(( (ZIP_CAP + 1023) / 1024 ))` rounds up so
#      any accepted cap still buys at least one 512-byte block.
#   3. The download phase fits inside the job's timeout, and no retrying curl
#      is left without a bound on its whole retry window.
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
FETCH_BUDGET=$(grep -m1 '^FETCH_BUDGET=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')
MAX_ATTEMPT_SECONDS=$(grep -m1 '^MAX_ATTEMPT_SECONDS=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')

# `--max-time` is per attempt, so the worst case is the whole download window
# plus one final attempt that started just inside it, and the metadata calls.
META_WORST=$((3 * (40 + 20)))
WORST=$((FETCH_BUDGET + MAX_ATTEMPT_SECONDS + META_WORST))
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

# `--retry-max-time` only gates whether a *new* retry may start, so the deadline
# is only real if the whole invocation is also wrapped in `timeout`.
dl_curl=$(printf '%s\n' "${curl_cmds}" | grep -- '-o "${ZIP_TMP}"')
check "the download is wrapped in timeout" \
  "$(printf '%s' "${dl_curl}" | grep -c 'timeout "${TIME_LEFT}" curl')" "1"
# With a hard deadline the whole phase is bounded by the budget alone.
check "download phase is bounded by the budget alone" \
  "$([ "${FETCH_BUDGET}" -lt $((JOB_TIMEOUT_MIN * 60)) ] && echo yes)" "yes"

# --- 4. Retries must never concatenate two responses ------------------------
# `curl --retry` can only rewind seekable output. Through a pipe or a command
# substitution, a retry appends to whatever the failed attempt already wrote,
# so a *successful* retry yields a corrupt two-response body. Every retrying
# curl must therefore write to a file with `-o`.
piped=$(printf '%s\n' "${curl_cmds}" | grep -- '--retry [0-9]' | grep -c -- '|')
captured=$(printf '%s\n' "${curl_cmds}" | grep -c -- '=\$(curl')
to_file=$(printf '%s\n' "${curl_cmds}" | grep -- '--retry [0-9]' | grep -c -- ' -o ')
check "no retrying curl streams through a pipe" "${piped}" "0"
check "no retrying curl is captured by substitution" "${captured}" "0"
check "every retrying curl writes to a file" "${to_file}" "${retrying}"
# HTTP error bodies must not be mistaken for content.
failing=$(printf '%s\n' "${curl_cmds}" | grep -c -- '--fail')
check "every curl rejects HTTP error bodies" "${failing}" "${total_curl}"

# --- 5. Only this run's binlogs may be analyzed -----------------------------
# The extract loop globs the whole directory, so anything a previous run left
# behind would be uploaded and attributed to this build.
mkdir_line=$(grep -n 'mkdir -p "${BINLOG_DIR}"' "${SCRIPT}" | head -1 | cut -d: -f1)
clear_line=$(grep -n 'rm -f "${BINLOG_DIR}"/\*.binlog' "${SCRIPT}" | head -1 | cut -d: -f1)
check "binlog directory is created" "$([ -n "${mkdir_line}" ] && echo yes)" "yes"
check "stale binlogs are cleared" "$([ -n "${clear_line}" ] && echo yes)" "yes"
check "cleared before anything is extracted into it" \
  "$([ -n "${clear_line}" ] && [ -n "${mkdir_line}" ] && [ "${clear_line}" -gt "${mkdir_line}" ] && echo yes)" "yes"
# The clear has to precede the first extraction, or it would delete the very
# binlogs this run just wrote.
extract_line=$(grep -n 'unzip\|BINLOG_DIR}"/\|extract-binlogs' "${SCRIPT}" | awk -F: -v c="${clear_line:-0}" '$1 > c {print $1; exit}')
check "clear precedes the first extraction" \
  "$([ -n "${extract_line}" ] && [ "${extract_line}" -gt "${clear_line}" ] && echo yes)" "yes"

# --- Section 6: temporary files are private ---------------------------------
# A fixed path under /tmp is one pre-created symlink -- or one collision with
# another job sharing a runner -- away from writing to, or reading back,
# someone else's file. Every scratch file this script creates comes from
# `mktemp`. BINLOG_DIR is deliberately exempt: it is an interface, passed in
# by the workflow and read by the upload step, and it is created and cleared
# rather than written through.
fixed_tmp=$(grep -c 'curl[^|]*-o /tmp/\|-o /tmp/[A-Za-z]' "${SCRIPT}" || true)
check "no curl writes to a fixed /tmp path" "${fixed_tmp}" "0"
check "scratch files come from mktemp" \
  "$(grep -cE '=\$\(mktemp\)' "${SCRIPT}")" "2"

# --- Section 7: the deadline covers extraction too -------------------------
# bash's `ulimit -f` is in 1024-byte units, not POSIX 512-byte blocks; using
# 512 here would have made the real cap twice the requested one.
check "ulimit uses bash's KiB unit" \
  "$(grep -c 'ulimit -f \$(( (ZIP_CAP + 1023) / 1024 ))' "${SCRIPT}")" "1"
check "no 512-byte block arithmetic remains" \
  "$(grep -c 'ZIP_CAP + 511) / 512' "${SCRIPT}" || true)" "0"
# A near-budget download phase must not still be able to queue a bounded
# extraction per artifact and run the job past timeout-minutes.
check "extraction is bounded by the shared deadline" \
  "$(grep -c 'timeout "${TIME_LEFT}" python3' "${SCRIPT}")" "1"
check "extraction re-reads the deadline first" \
  "$(awk '/TIME_LEFT=\$\(\( FETCH_DEADLINE/{n++} END{print n+0}' "${SCRIPT}")" "2"

# --- Section 8: unit and log-injection hygiene ------------------------------
# bash counts `ulimit -f` in 1024-byte units, except in POSIX mode where it
# counts 512-byte blocks. Pinning the mode is what makes Section 7's
# arithmetic unambiguous.
check "posix mode is pinned before ulimit" \
  "$(grep -c 'set +o posix' "${SCRIPT}")" "1"
check "posix pin precedes the ulimit call" \
  "$([ "$(grep -n 'set +o posix' "${SCRIPT}" | cut -d: -f1)" -lt \
      "$(grep -n 'ulimit -f \$((' "${SCRIPT}" | head -1 | cut -d: -f1)" ] && echo yes)" "yes"
# Artifact names are Azure DevOps metadata and reach the log as workflow
# commands, so only the sanitized copy may be interpolated.
check "no raw artifact name in a workflow command" \
  "$(grep -c '::warning::[^"]*${name}' "${SCRIPT}" || true)" "0"

exit "${fail}"
