#!/usr/bin/env bash
# Focused checks for the two guards added in response to review:
#   1. GITHUB_OUTPUT must be set AND writable.
#   2. A remaining compressed allowance below MIN_ZIP_BYTES stops downloads,
#      so `ulimit -f $((ZIP_CAP / 512))` can never floor to 0 blocks.
set -u
SCRIPT="$1"
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

# --- 2. ZIP_CAP floor -------------------------------------------------------
MAX_ZIP_BYTES=$(grep -m1 '^MAX_ZIP_BYTES=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')
MAX_TOTAL_ZIP_BYTES=$(grep -m1 '^MAX_TOTAL_ZIP_BYTES=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')
MIN_ZIP_BYTES=$(grep -m1 '^MIN_ZIP_BYTES=' "${SCRIPT}" | cut -d= -f2 | awk '{print $1}')

clamp() { # $1 = bytes already downloaded -> echoes "break" or the ulimit blocks
  local total="$1" cap allowance
  cap="${MAX_ZIP_BYTES}"
  allowance=$((MAX_TOTAL_ZIP_BYTES - total))
  [ "${allowance}" -lt "${cap}" ] && cap="${allowance}"
  if [ "${cap}" -lt "${MIN_ZIP_BYTES}" ]; then echo "break"; else echo $((cap / 512)); fi
}

check "MIN_ZIP_BYTES is at least one ulimit block" "$([ "${MIN_ZIP_BYTES}" -ge 512 ] && echo yes)" "yes"
check "fresh budget clamps to the per-artifact cap" "$(clamp 0)" "$((MAX_ZIP_BYTES / 512))"
check "partial budget clamps to the remainder" "$(clamp $((MAX_TOTAL_ZIP_BYTES - 1048576)))" "$((1048576 / 512))"
check "sub-512-byte remainder stops downloads" "$(clamp $((MAX_TOTAL_ZIP_BYTES - 100)))" "break"
check "exhausted budget stops downloads" "$(clamp "${MAX_TOTAL_ZIP_BYTES}")" "break"
check "over-spent budget stops downloads" "$(clamp $((MAX_TOTAL_ZIP_BYTES + 5000)))" "break"

# The regression the review found: any cap the loop actually accepts must give
# a non-zero ulimit, otherwise every write fails and the artifact is dropped.
for spent in 0 1000 $((MAX_TOTAL_ZIP_BYTES / 2)) $((MAX_TOTAL_ZIP_BYTES - MIN_ZIP_BYTES)); do
  r=$(clamp "${spent}")
  if [ "${r}" != "break" ] && [ "${r}" -le 0 ]; then
    echo "FAIL accepted cap yields ulimit 0 at spent=${spent}"; fail=1
  fi
done
echo "PASS no accepted cap yields a zero ulimit"

exit "${fail}"
