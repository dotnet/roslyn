#!/usr/bin/env bash
set -euo pipefail

RoslynDir="$(dirname $0)"
ToolsetPath="$RoslynDir/Binaries/toolset"

rm $ToolsetPath/restore.semaphore
make --makefile "$RoslynDir/Makefile" restore
