#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

usage()
{
    echo "Runs our integration suite on Linux"
    echo "usage: cibuild.sh [options]"
    echo ""
    echo "Options"
    echo "  --debug               Build Debug (default)"
    echo "  --release             Build Release"
}

THIS_DIR="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${THIS_DIR}"/build/scripts/build-utils.sh
ROOT_PATH="$(get_repo_dir)"

BUILD_CONFIGURATION=--debug

# $HOME is unset when running the mac unit tests.
if [[ -z "${HOME+x}" ]]
then
    # Note that while ~ usually refers to $HOME, in the case where $HOME is unset,
    # it looks up the current user's home dir, which is what we want.
    # https://www.gnu.org/software/bash/manual/html_node/Tilde-Expansion.html
    export HOME="$(cd ~ && pwd)"
fi

# There's no reason to send telemetry or prime a local package cach when building
# in CI.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

while [[ $# > 0 ]]
do
    opt="$(echo "$1" | awk '{print tolower($0)}')"
    case "$opt" in
        -h|--help)
        usage
        exit 1
        ;;
        --debug)
        BUILD_CONFIGURATION=--debug
        shift 1
        ;;
        --release)
        BUILD_CONFIGURATION=--release
        shift 1
        ;;
        *)
        usage 
        exit 1
        ;;
    esac
done

echo Building this commit:
git show --no-patch --pretty=raw HEAD

# obtain_dotnet.sh puts the right dotnet on the PATH
FORCE_DOWNLOAD=true
source "${ROOT_PATH}"/build/scripts/obtain_dotnet.sh

"${ROOT_PATH}"/build.sh --restore --bootstrap --build --test "${BUILD_CONFIGURATION}"

echo "Killing VBCSCompiler"
dotnet "${ROOT_PATH}"/Binaries/Bootstrap/bincore/VBCSCompiler.dll -shutdown
