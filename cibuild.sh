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

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)
BINARIES_PATH=${THIS_DIR}/Binaries
BOOTSTRAP_PATH=${BINARIES_PATH}/Bootstrap
SRC_PATH=${THIS_DIR}/src
BUILD_LOG_PATH=${BINARIES_PATH}/Build.log

BUILD_CONFIGURATION=Debug
CLEAN_RUN=false
SKIP_TESTS=false
SKIP_COMMIT_PRINTING=false

# $HOME is unset when running the mac unit tests.
if [[ -z ${HOME+x} ]]
then
    # Note that while ~ usually refers to $HOME, in the case where $HOME is unset,
    # it looks up the current user's home dir, which is what we want.
    # https://www.gnu.org/software/bash/manual/html_node/Tilde-Expansion.html
    export HOME=$(cd ~ && pwd)
fi

# LTTNG is the logging infrastructure used by coreclr.  Need this variable set 
# so it doesn't output warnings to the console.
export LTTNG_HOME=$HOME

# There's no reason to send telemetry or prime a local package cach when building
# in CI.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

while [[ $# > 0 ]]
do
    opt="$(echo $1 | awk '{print tolower($0)}')"
    case $opt in
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
source ${THIS_DIR}/build/scripts/obtain_dotnet.sh

RUNTIME_ID=$(dotnet --info | awk '/RID:/{print $2;}')
echo "Using Runtime Identifier: ${RUNTIME_ID}"

RESTORE_ARGS="-r ${RUNTIME_ID} -v Minimal --disable-parallel"
echo "Restoring BaseToolset.csproj"
dotnet restore ${RESTORE_ARGS} ${THIS_DIR}/build/ToolsetPackages/BaseToolset.csproj
echo "Restoring CrossPlatform.sln"
dotnet restore ${RESTORE_ARGS} ${THIS_DIR}/CrossPlatform.sln

BUILD_ARGS="-c ${BUILD_CONFIGURATION} -r ${RUNTIME_ID} /nologo /consoleloggerparameters:Verbosity=minimal;summary /filelogger /fileloggerparameters:Verbosity=normal;logFile=${BUILD_LOG_PATH} /p:RoslynRuntimeIdentifier=${RUNTIME_ID} /maxcpucount:1"

echo "Building bootstrap CscCore"
dotnet publish ${SRC_PATH}/Compilers/CSharp/CscCore -o ${BOOTSTRAP_PATH}/csc ${BUILD_ARGS}
echo "Building bootstrap VbcCore"
dotnet publish ${SRC_PATH}/Compilers/VisualBasic/VbcCore -o ${BOOTSTRAP_PATH}/vbc ${BUILD_ARGS}
rm -rf ${BINARIES_PATH}/${BUILD_CONFIGURATION}
BUILD_ARGS+=" /p:CscToolPath=${BOOTSTRAP_PATH}/csc /p:CscToolExe=csc /p:VbcToolPath=${BOOTSTRAP_PATH}/vbc /p:VbcToolExe=vbc"

echo "Building CrossPlatform.sln"
dotnet build ${THIS_DIR}/CrossPlatform.sln ${BUILD_ARGS}

if [[ "${SKIP_TESTS}" == false ]]
then
    echo "Running tests"
    ${THIS_DIR}/build/scripts/tests.sh ${BUILD_CONFIGURATION}
fi
