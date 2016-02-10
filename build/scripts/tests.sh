#!/usr/bin/env bash

BUILD_CONFIGURATION=$1

# This function will update the PATH variable to put the desired
# version of Mono ahead of the system one. 

cd Binaries/$BUILD_CONFIGURATION/CoreClrTest

chmod +x ./corerun

mkdir -p ../xUnitResults/

./corerun ./xunit.console.netcore.exe *.UnitTests.dll -xml ../xUnitResults/CoreClrUnitTestResults.xml

if [ $? -ne 0 ]; then
	echo Unit test failed
	exit 1
fi

