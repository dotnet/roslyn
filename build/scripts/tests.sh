#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

BUILD_CONFIGURATION=${1:-Debug}

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)
BINARIES_PATH=${THIS_DIR}/../../Binaries
SRC_PATH=${THIS_DIR}/../../src
TEST_DIR=${BINARIES_PATH}/${BUILD_CONFIGURATION}/CoreClrTest

RUNTIME_ID=$(dotnet --info | awk '/RID:/{print $2;}')

BUILD_ARGS="-c ${BUILD_CONFIGURATION} -r ${RUNTIME_ID} /consoleloggerparameters:Verbosity=minimal;summary /p:RoslynRuntimeIdentifier=${RUNTIME_ID}"
dotnet publish ${SRC_PATH}/Test/DeployCoreClrTestRuntime -o ${TEST_DIR} ${BUILD_ARGS}

cd ${TEST_DIR}

mkdir -p xUnitResults

dotnet exec ./xunit.console.netcore.exe *.UnitTests.dll -parallel all -xml xUnitResults/TestResults.xml

if [ $? -ne 0 ]; then
    echo Unit test failed
    exit 1
fi
