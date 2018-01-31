#!/usr/bin/env bash

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)

echo "Restoring toolset packages"

RESTORE_ARGS="-v Minimal --disable-parallel"
echo "Restoring RoslynToolset.csproj"
dotnet restore ${RESTORE_ARGS} "${THIS_DIR}/../ToolsetPackages/RoslynToolset.csproj"
echo "Restoring Compilers.sln"
dotnet restore ${RESTORE_ARGS} "${THIS_DIR}/../../Compilers.sln"
