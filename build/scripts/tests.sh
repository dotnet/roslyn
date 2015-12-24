#!/usr/bin/env bash

MONO_PATH=$1
BUILD_CONFIGURATION=$2
XUNIT_VERSION=$3
MONO_DIR="$(dirname $MONO_PATH)"

export MONO_THREADS_PER_CPU=50
export PATH=$MONO_DIR:$PATH

# This function will update the PATH variable to put the desired
# version of Mono ahead of the system one. 

xunit_runner=~/.nuget/packages/xunit.runner.console/$XUNIT_VERSION/tools/xunit.console.x86.exe
test_binaries=(
	Roslyn.Compilers.CSharp.CommandLine.UnitTests
	Roslyn.Compilers.CSharp.Syntax.UnitTests
	Roslyn.Compilers.CSharp.Semantic.UnitTests
	Roslyn.Compilers.CSharp.Symbol.UnitTests
	Roslyn.Compilers.VisualBasic.Syntax.UnitTests)
any_failed=false

# Need to copy over the execution dependencies.  This isn't being done correctly
# by msbuild at the moment. 
cp ~/.nuget/packages/xunit.extensibility.execution/$XUNIT_VERSION/lib/net45/xunit.execution.desktop.* Binaries/$BUILD_CONFIGURATION

for i in "${test_binaries[@]}"
do
	mkdir -p Binaries/$BUILD_CONFIGURATION/xUnitResults/
	mono $MONO_ARGS $xunit_runner Binaries/$BUILD_CONFIGURATION/$i.dll -xml Binaries/$BUILD_CONFIGURATION/xUnitResults/$i.dll.xml -noshadow
	if [ $? -ne 0 ]; then
		any_failed=true
	fi
done

if [ "$any_failed" = "true" ]; then
	echo Unit test failed
	exit 1
fi

