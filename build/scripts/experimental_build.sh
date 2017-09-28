#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

ROOT_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
POWERSHELL_VERSION=6.0.0-beta.7
POWERSHELL_APPIMAGE=${ROOT_DIR}/Binaries/Tools/PowerShell-${POWERSHELL_VERSION}-x86_64.AppImage
POWERSHELL_EXE=${ROOT_DIR}/Binaries/Tools/powershell/squashfs-root/AppRun
POWERSHELL_URL=https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/PowerShell-${POWERSHELL_VERSION}-x86_64.AppImage

if [[ ! -x "${POWERSHELL_EXE}" ]]
then
    if [[ ! -x "${POWERSHELL_APPIMAGE}" ]]
    then
        echo "Downloading ${POWERSHELL_URL} -> ${POWERSHELL_APPIMAGE}"
        mkdir -p $(dirname "${POWERSHELL_APPIMAGE}")
        curl -L "${POWERSHELL_URL}" -o "${POWERSHELL_APPIMAGE}"
        chmod +x "${POWERSHELL_APPIMAGE}"
    fi

    # The AppImage needs to be extracted (instead of directly run) because, on
    # WSL, directly running it results in "kernel module 'fuse' not found".
    EXTRACT_DIR=$(dirname $(dirname "${POWERSHELL_EXE}"))
    EXTRACT_LOGFILE="${EXTRACT_DIR}/appimage_extract.log"
    mkdir -p "${EXTRACT_DIR}"
    echo "Extracting ${POWERSHELL_APPIMAGE} -> ${EXTRACT_DIR}"
    # The powershell AppImage 6.0.0-beta.7 is broken - usr/bin fails to be
    # created with "TODO: Implement inode.base.inode_type 8"
    mkdir -p "${EXTRACT_DIR}/squashfs-root/usr/bin"
    cd "${EXTRACT_DIR}" && "${POWERSHELL_APPIMAGE}" --appimage-extract > "${EXTRACT_LOGFILE}"
fi

if [[ $# -eq 0 ]]
then
    ${POWERSHELL_EXE} -noprofile -executionPolicy RemoteSigned -file "${ROOT_DIR}/build/scripts/build.ps1" -build
else
    ${POWERSHELL_EXE} -noprofile -executionPolicy RemoteSigned -file "${ROOT_DIR}/build/scripts/build.ps1" "$@"
fi
