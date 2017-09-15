#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

BUILD_CONFIGURATION=${1:-Debug}

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)
source ${THIS_DIR}/build-utils.sh

ROOT_PATH=$(get_repo_dir)
BINARIES_PATH=${ROOT_PATH}/Binaries
UNITTEST_DIR=${BINARIES_PATH}/${BUILD_CONFIGURATION}/UnitTests
LOG_DIR=${BINARIES_PATH}/${BUILD_CONFIGURATION}/xUnitResults
NUGET_DIR=${HOME}/.nuget/packages
RUNTIME_ID=$(dotnet --info | awk '/RID:/{print $2;}')
TARGET_FRAMEWORK=netcoreapp2.0
XUNIT_CONSOLE_VERSION=$(get_package_version dotnet-xunit)
XUNIT_CONSOLE=${NUGET_DIR}/dotnet-xunit/${XUNIT_CONSOLE_VERSION}/tools/${TARGET_FRAMEWORK}/xunit.console.dll

echo "Using ${XUNIT_CONSOLE}"

# Need to publish projects that have runtime assets before running tests
NEED_PUBLISH=(
    'src/Compilers/CSharp/Test/Symbol/CSharpCompilerSymbolTest.csproj'
)

for project in $NEED_PUBLISH
do
    echo "Publishing ${project}"
    dotnet publish --no-restore ${ROOT_PATH}/${project} -r ${RUNTIME_ID} -f ${TARGET_FRAMEWORK} -p:SelfContained=true
done

# Discover and run the tests
mkdir -p "${LOG_DIR}"

for TEST_PATH in ${UNITTEST_DIR}/*/${TARGET_FRAMEWORK}
do
    PUBLISH_TEST_PATH=${TEST_PATH}/${RUNTIME_ID}/publish
    if [ -d ${PUBLISH_TEST_PATH} ]
    then
        TEST_PATH=${PUBLISH_TEST_PATH}
    fi

    FILE_NAME=( ${TEST_PATH}/*.UnitTests.dll )
    LOG_FILE="${LOG_DIR}/$(basename "${FILE_NAME%.*}.xml")"
    DEPS_JSON="${FILE_NAME%.*}.deps.json"
    RUNTIMECONFIG_JSON="${FILE_NAME%.*}.runtimeconfig.json"
    echo Running ${FILE_NAME}
    dotnet exec --depsfile "${DEPS_JSON}" --runtimeconfig "${RUNTIMECONFIG_JSON}" ${XUNIT_CONSOLE} "$FILE_NAME" -xml "${LOG_FILE}"
    if [[ $? -ne 0 ]]; then
        echo Unit test failed
        exit 1
    fi
done
