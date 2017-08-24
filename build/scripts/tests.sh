#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

# This function will give you the current version number for a given nuget package
# based on the contents of Packages.props. 
# 
# Provide the package name in the format shown in the nuget gallery
#   get_package_version dotnet-xunit
#   get_package_version System.Console
get_package_version() 
{
    local name=${1/./}
    local name=${name/-/}
    local version=$(awk -F'[<>]' "/<${name}Version>/{print \$3}" ${SRC_PATH}/build/Targets/Packages.props)
    echo $version
}

BUILD_CONFIGURATION=${1:-Debug}

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)
BINARIES_PATH=${THIS_DIR}/../../Binaries
SRC_PATH=${THIS_DIR}/../..
UNITTEST_DIR=${BINARIES_PATH}/${BUILD_CONFIGURATION}/UnitTests
LOG_DIR=${BINARIES_PATH}/${BUILD_CONFIGURATION}/xUnitResults
NUGET_DIR=${HOME}/.nuget/packages
RUNTIME_ID=$(dotnet --info | awk '/RID:/{print $2;}')
TARGET_FRAMEWORK=netcoreapp2.0
XUNIT_CONSOLE_VERSION=$(get_package_version dotnet-xunit)
XUNIT_CONSOLE=${NUGET_DIR}/dotnet-xunit/${XUNIT_CONSOLE_VERSION}/tools/${TARGET_FRAMEWORK}/xunit.console.dll

echo Using $XUNIT_CONSOLE

# Need to publish projects that have runtime assets before running tests
NEED_PUBLISH=(
    'src\Compilers\CSharp\Test\Symbol\CSharpCompilerSymbolTest.csproj'
)
BUILD_ARGS="--no-restore -c ${BUILD_CONFIGURATION} -consoleloggerparameters:Verbosity=minimal;summary -p:RuntimeIdentifier=${RUNTIME_ID} -p:TargetFramework=${TARGET_FRAMEWORK}"
for p in $NEED_PUBLISH
do
    echo Publishing ${p}
    dotnet publish --no-restore ${SRC_PATH}/${p} -p:RoslynRuntimeIdentifier=${RUNTIME_ID} -p:RuntimeIdentifier=${RUNTIME_ID} -p:TargetFramework=${TARGET_FRAMEWORK}
done

# Discover and run the tests
mkdir -p ${LOG_DIR}
pushd ${UNITTEST_DIR}

for d in *
do
    TEST_PATH=${UNITTEST_DIR}/${d}/${TARGET_FRAMEWORK}
    PUBLISH_TEST_PATH=${TEST_PATH}/${RUNTIME_ID}/publish
    if [ -d ${PUBLISH_TEST_PATH} ]
    then
        TEST_PATH=${PUBLISH_TEST_PATH}
    fi

    pushd $TEST_PATH
    FILE_NAME=$(ls *.UnitTests.dll)
    LOG_NAME="${FILE_NAME%.*}.xml"
    echo Running ${TEST_PATH}/${FILE_NAME}
    dotnet exec --depsfile "${FILE_NAME%.*}.deps.json" --runtimeconfig "${FILE_NAME%.*}.runtimeconfig.json" ${XUNIT_CONSOLE} $FILE_NAME -xml ${LOG_DIR}/${LOG_NAME}
    if [ $? -ne 0 ]; then
        echo Unit test failed
        exit 1
    fi
    popd
done

popd


