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
    echo "  --cleanrun            Clean the project before building"
    echo "  --skiptest            Do not run tests"
    echo "  --skipcommitprinting  Do not print commit information"
}

THIS_DIR="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${THIS_DIR}"/build/scripts/build-utils.sh
ROOT_PATH="$(get_repo_dir)"
BINARIES_PATH="${ROOT_PATH}"/Binaries
BOOTSTRAP_PATH="${BINARIES_PATH}"/Bootstrap
SRC_PATH="${ROOT_PATH}"/src
TARGET_FRAMEWORK=netcoreapp2.0

BUILD_CONFIGURATION=Debug
CLEAN_RUN=false
SKIP_RESTORE=false
SKIP_TESTS=false
SKIP_COMMIT_PRINTING=false

# $HOME is unset when running the mac unit tests.
if [[ -z "${HOME+x}" ]]
then
    # Note that while ~ usually refers to $HOME, in the case where $HOME is unset,
    # it looks up the current user's home dir, which is what we want.
    # https://www.gnu.org/software/bash/manual/html_node/Tilde-Expansion.html
    export HOME="$(cd ~ && pwd)"
fi

# LTTNG is the logging infrastructure used by coreclr.  Need this variable set 
# so it doesn't output warnings to the console.
export LTTNG_HOME="$HOME"

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
        BUILD_CONFIGURATION=Debug
        shift 1
        ;;
        --release)
        BUILD_CONFIGURATION=Release
        shift 1
        ;;
        --cleanrun)
        CLEAN_RUN=true
        shift 1
        ;;
        --skiprestore)
        SKIP_RESTORE=true
        shift 1
        ;;
        --skiptests)
        SKIP_TESTS=true
        shift 1
        ;;
        --skipcommitprinting)
        SKIP_COMMIT_PRINTING=true
        shift 1
        ;;
        *)
        usage 
        exit 1
        ;;
    esac
done

if [ "$CLEAN_RUN" == "true" ]; then
    echo Clean out the enlistment
    git clean -dxf . 
fi

if [ "$SKIP_COMMIT_PRINTING" == "false" ]; then
    echo Building this commit:
    git show --no-patch --pretty=raw HEAD
fi

# obtain_dotnet.sh puts the right dotnet on the PATH
FORCE_DOWNLOAD=true
source "${ROOT_PATH}"/build/scripts/obtain_dotnet.sh

if [[ "${SKIP_RESTORE}" == false ]]
then
    RESTORE_ARGS="-v Minimal --disable-parallel"
    echo "Restoring BaseToolset.csproj"
    dotnet restore ${RESTORE_ARGS} "${ROOT_PATH}"/build/ToolsetPackages/BaseToolset.csproj
    echo "Restoring CoreToolset.csproj"
    dotnet restore ${RESTORE_ARGS} "${ROOT_PATH}"/build/ToolsetPackages/CoreToolset.csproj
    echo "Restoring CrossPlatform.sln"
    dotnet restore ${RESTORE_ARGS} "${ROOT_PATH}"/CrossPlatform.sln
fi

BUILD_ARGS="--no-restore -c ${BUILD_CONFIGURATION} /nologo /maxcpucount:1"
BOOTSTRAP_BUILD_ARGS="${BUILD_ARGS} /p:UseShippingAssemblyVersion=true /p:InitialDefineConstants=BOOTSTRAP"

echo "Building bootstrap toolset"
dotnet publish "${ROOT_PATH}"/src/Compilers/CSharp/csc -o "${BOOTSTRAP_PATH}/bincore" --framework ${TARGET_FRAMEWORK} ${BOOTSTRAP_BUILD_ARGS} "/bl:${BINARIES_PATH}/BootstrapCsc.binlog"
dotnet publish "${ROOT_PATH}"/src/Compilers/VisualBasic/vbc -o "${BOOTSTRAP_PATH}/bincore" --framework ${TARGET_FRAMEWORK} ${BOOTSTRAP_BUILD_ARGS} "/bl:${BINARIES_PATH}/BootstrapVbc.binlog"
dotnet publish "${ROOT_PATH}"/src/Compilers/Server/VBCSCompiler -o "${BOOTSTRAP_PATH}/bincore" --framework ${TARGET_FRAMEWORK} ${BOOTSTRAP_BUILD_ARGS} "/bl:${BINARIES_PATH}/BootstrapVBCSCompiler.binlog"
dotnet publish "${ROOT_PATH}"/src/Compilers/Core/MSBuildTask -o "${BOOTSTRAP_PATH}" ${BOOTSTRAP_BUILD_ARGS} "/bl:${BINARIES_PATH}/BoostrapMSBuildTask.binlog"

BUILD_ARGS+=" /bl:${BINARIES_PATH}/Build.binlog /p:BootstrapBuildPath=${BOOTSTRAP_PATH}"

echo "Building CrossPlatform.sln"
dotnet build "${ROOT_PATH}"/CrossPlatform.sln ${BUILD_ARGS}

if [[ "${SKIP_TESTS}" == false ]]
then
    echo "Running tests"
    "${ROOT_PATH}"/build/scripts/tests.sh "${BUILD_CONFIGURATION}"
fi

echo "Killing VBCSCompiler"
dotnet exec "${BOOTSTRAP_PATH}"/bincore/VBCSCompiler.dll -shutdown
