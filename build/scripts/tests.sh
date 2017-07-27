#!/usr/bin/env bash

BUILD_CONFIGURATION=$1

cd Binaries/$BUILD_CONFIGURATION/CoreClrTest

chmod +x ./corerun

mkdir -p xUnitResults

./corerun ./xunit.console.netcore.exe *.UnitTests.dll -parallel all -xml xUnitResults/TestResults.xml

if [ $? -ne 0 ]; then
    echo Unit test failed
    exit 1
fi

