#!/usr/bin/env bash

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)

echo "Restoring toolset packages"

if [[ "$1" == "--mono" ]] ; then
    restore_cmd="nuget restore"
    restore_args="-Verbosity quiet -DisableParallelProcessing"
else
    restore_cmd="dotnet restore"
    restore_args="-v Minimal --disable-parallel"
fi

echo "Restoring RoslynToolset.csproj"
$restore_cmd ${restore_args} "${THIS_DIR}/../ToolsetPackages/RoslynToolset.csproj"
echo "Restoring Compilers.sln"
$restore_cmd ${restore_args} "${THIS_DIR}/../../Compilers.sln"
