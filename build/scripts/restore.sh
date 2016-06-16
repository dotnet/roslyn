#!/usr/bin/env bash

ROSLYN_TOOLSET_PATH=$1
DOTNET_PATH=$ROSLYN_TOOLSET_PATH/dotnet-cli/dotnet

# Workaround, see https://github.com/dotnet/roslyn/issues/10210
export HOME=$(cd ~ && pwd)

echo "Restoring toolset packages"

$DOTNET_PATH restore -v Minimal --disable-parallel $(pwd)/build/ToolsetPackages/project.json

echo "Restore CrossPlatform.sln"

$ROSLYN_TOOLSET_PATH/RoslynRestore $(pwd)/CrossPlatform.sln $(pwd)/nuget.exe $DOTNET_PATH
