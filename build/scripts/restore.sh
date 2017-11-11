#!/usr/bin/env bash

THIS_DIR=$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)

# Workaround, see https://github.com/dotnet/roslyn/issues/10210
# $HOME is unset when running the mac unit tests.
if [[ -z ${HOME+x} ]]
then
    # Note that while ~ usually refers to $HOME, in the case where $HOME is unset,
    # it looks up the current user's home dir, which is what we want.
    # https://www.gnu.org/software/bash/manual/html_node/Tilde-Expansion.html
    export HOME=$(cd ~ && pwd)
fi

echo "Restoring toolset packages"

RESTORE_ARGS="-v Minimal --disable-parallel"
echo "Restoring BaseToolset.csproj"
dotnet restore ${RESTORE_ARGS} ${THIS_DIR}/../ToolsetPackages/BaseToolset.csproj
echo "Restoring CoreToolset.csproj"
dotnet restore ${RESTORE_ARGS} ${THIS_DIR}/../ToolsetPackages/CoreToolset.csproj
echo "Restoring Compilers.sln"
dotnet restore ${RESTORE_ARGS} ${THIS_DIR}/../../Compilers.sln
