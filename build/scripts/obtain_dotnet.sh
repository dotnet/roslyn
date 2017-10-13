#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Source this script to ensure dotnet is installed and on the path.
# If the FORCE_DOWNLOAD environment variable is set to "true", the system's dotnet install is ignored,
# and dotnet is downloaded and installed locally.

set -e
set -u

# check if `dotnet` is already on the PATH
if command -v dotnet >/dev/null 2>&1
then
    if [[ "${FORCE_DOWNLOAD:-false}" != true ]]
    then
        exit 0
    fi
fi

# This is a function to keep variable assignments out of the parent script (that is sourcing this file)
install_dotnet () {
    # Download and install `dotnet` locally
    local THIS_DIR="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    source "${THIS_DIR}"/build-utils.sh

    local DOTNET_VERSION="$(get_tool_version dotnetSdk)"
    local DOTNET_PATH="${THIS_DIR}"/../../Binaries/dotnet-cli

    if [[ ! -x "${DOTNET_PATH}/dotnet" ]]
    then
        echo "Downloading and installing .NET CLI version ${DOTNET_VERSION} to ${DOTNET_PATH}"
        curl https://dot.net/v1/dotnet-install.sh | \
            /usr/bin/env bash -s -- --version "${DOTNET_VERSION}" --install-dir "${DOTNET_PATH}"
    else
        echo "Skipping download of .NET CLI: Already installed at ${DOTNET_PATH}"
    fi

    export PATH="${DOTNET_PATH}:${PATH}"
}
install_dotnet
