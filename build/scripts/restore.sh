#!/usr/bin/env bash

# Workaround, see https://github.com/dotnet/roslyn/issues/10210
export HOME="$(cd ~ && pwd)"

echo "Restoring toolset packages"

dotnet restore -v Minimal --disable-parallel "$(pwd)"/build/ToolsetPackages/BaseToolset.csproj

echo "Restore CrossPlatform.sln"

dotnet restore "$(pwd)"/CrossPlatform.sln
