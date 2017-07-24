#!/usr/bin/env bash

echo "Restoring toolset packages"

dotnet restore -v Minimal --disable-parallel $(pwd)/build/ToolsetPackages/BaseToolset.csproj

echo "Restore CrossPlatform.sln"

dotnet restore -v Minimal --disable-parallel $(pwd)/CrossPlatform.sln
