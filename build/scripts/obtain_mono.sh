#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Source this script to ensure mono is installed and on the path.

# This is a function to keep variable assignments out of the parent script (that is sourcing this file)
install_mono () {
    # Download and install `mono` locally
    local THIS_DIR="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    source "${THIS_DIR}"/build-utils.sh

    local MONO_VERSION="$(get_tool_version mono)"
    # the tar file has `mono` as the root directory
    local MONO_PATH="${THIS_DIR}"/../../Binaries/Tools

    if [[ ! -x "${MONO_PATH}/mono/bin/mono" ]]
    then
        echo "Downloading mono ${MONO_VERSION}"
        mkdir -p "${MONO_PATH}"
        curl -L https://roslyninfra.blob.core.windows.net/jenkins/mono/mono-${MONO_VERSION}.tar.gz | tar xz -C "${MONO_PATH}"
    fi

    export PATH="${MONO_PATH}/mono/bin:${PATH}"
}
install_mono
