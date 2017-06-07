#!/usr/bin/env bash

# Workaround, see https://github.com/dotnet/roslyn/issues/10210
export HOME=$(cd ~ && pwd)

UNIX_RID_TO_RESTORE=${1:-ubuntu.14.04-x64}

echo "Restoring toolset packages"

dotnet restore -v Minimal --disable-parallel -r $UNIX_RID_TO_RESTORE $(pwd)/build/ToolsetPackages/BaseToolset.csproj

echo "Restore CrossPlatform.sln"

dotnet restore -r $UNIX_RID_TO_RESTORE $(pwd)/CrossPlatform.sln
