#!/usr/bin/env bash

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)

echo "Restoring toolset packages"

RESTORE_ARGS="-v Minimal --disable-parallel"
echo "Restoring BaseToolset.csproj"
dotnet restore ${RESTORE_ARGS} "${THIS_DIR}/../ToolsetPackages/BaseToolset.csproj"
echo "Restoring CoreToolset.csproj"
dotnet restore ${RESTORE_ARGS} "${THIS_DIR}/../ToolsetPackages/CoreToolset.csproj"
echo "Restoring Compilers.sln"
dotnet restore ${RESTORE_ARGS} "${THIS_DIR}/../../Compilers.sln"
