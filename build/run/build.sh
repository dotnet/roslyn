#!/usr/bin/env bash
set -euo pipefail

RoslynDir="$(dirname $0)/../../"

make --makefile "$RoslynDir/Makefile" 
