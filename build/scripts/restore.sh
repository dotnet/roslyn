#!/usr/bin/env bash

ROSLYN_TOOLSET_PATH=$1
DOTNET_PATH=$ROSLYN_TOOLSET_PATH/dotnet-cli/dotnet

export HOME=$(cd ~ && pwd)

# NuGet often exceeds the limit of open files on Mac
# https://github.com/NuGet/Home/issues/2163
if [ "$(uname -s)" == "Darwin" ]
then
    ulimit -n 6500
fi

echo "Restoring toolset packages"

$DOTNET_PATH restore -v Minimal --disable-parallel $(pwd)/build/ToolsetPackages/project.json

echo "Restore CrossPlatform.sln"

chmod +x $ROSLYN_TOOLSET_PATH/corerun

$ROSLYN_TOOLSET_PATH/corerun $ROSLYN_TOOLSET_PATH/MSBuildToolset.dll $(pwd)/CrossPlatform.sln $(pwd)/nuget.exe $DOTNET_PATH
