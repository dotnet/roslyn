#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

ROOT_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
POWERSHELL_EXE=${ROOT_DIR}/Binaries/Tools/powershell.AppImage
POWERSHELL_URL=https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-beta.7/PowerShell-6.0.0-beta.7-x86_64.AppImage

if [[ ! -x "${POWERSHELL_EXE}" ]]
then
    echo "Downloading ${POWERSHELL_URL} -> ${POWERSHELL_EXE}"
    mkdir -p $(dirname "${POWERSHELL_EXE}")
    curl -L "${POWERSHELL_URL}" -o "${POWERSHELL_EXE}"
    chmod +x "${POWERSHELL_EXE}"
fi

if [[ $# -eq 0 ]]
then
    ${POWERSHELL_EXE} -noprofile -executionPolicy RemoteSigned -file "${ROOT_DIR}/build/scripts/build.ps1" -build
else
    ${POWERSHELL_EXE} -noprofile -executionPolicy RemoteSigned -file "${ROOT_DIR}/build/scripts/build.ps1" "$@"
fi
