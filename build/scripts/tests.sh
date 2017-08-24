#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

BUILD_CONFIGURATION=${1:-Debug}

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)
BINARIES_PATH=${THIS_DIR}/../../Binaries
SRC_PATH=${THIS_DIR}/../..
TEST_DIR=${BINARIES_PATH}/${BUILD_CONFIGURATION}/UnitTests
RUNTIME_ID=$(dotnet --info | awk '/RID:/{print $2;}')
TARGET_FRAMEWORK=netcoreapp2.0

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
pushd ${TEST_DIR}

for d in *
do
    TEST_PATH=${TEST_DIR}/${d}/${TARGET_FRAMEWORK}
    PUBLISH_TEST_PATH=${TEST_PATH}/${RUNTIME_ID}/publish
    if [ -d ${PUBLISH_TEST_PATH} ]
    then
        TEST_PATH=${PUBLISH_TEST_PATH}
    fi

    pushd $TEST_PATH
    FILE_NAME=$(ls *.UnitTests.dll)
    echo Running ${TEST_PATH}/${FILE_NAME}
    dotnet vstest $FILE_NAME
    if [ $? -ne 0 ]; then
        echo Unit test failed
        exit 1
    fi
    popd
done

popd


#BUILD_ARGS="-c ${BUILD_CONFIGURATION} -r ${RUNTIME_ID} /consoleloggerparameters:Verbosity=minimal;summary /p:RoslynRuntimeIdentifier=${RUNTIME_ID}"
#dotnet publish ${SRC_PATH}/Test/DeployCoreClrTestRuntime -o ${TEST_DIR} ${BUILD_ARGS}

#cd ${TEST_DIR}

#mkdir -p xUnitResults

#dotnet exec ./xunit.console.netcore.exe *.UnitTests.dll -parallel all -xml xUnitResults/TestResults.xml

#if [ $? -ne 0 ]; then
#    echo Unit test failed
#    exit 1
#fi
