#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# This runs the tests using RunTests on the jobs build.

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if subcommand fails
set -e 

usage()
{
  echo "Common settings:"
  echo "  --configuration <value>    Build configuration to test: 'Debug' or 'Release' (short: -c)"
  echo ""
}

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

ci=true
configuration="Debug"

args=""

while [[ $# > 0 ]]; do
  opt="$(echo "$1" | awk '{print tolower($0)}')"
  case "$opt" in
    --help|-h)
      usage
      exit 0
      ;;
    --configuration|-c)
      configuration=$2
      args="$args $1"
      shift
      ;;
    *)
      echo "Invalid argument: $1"
      usage
      exit 1
      ;;
  esac
  args="$args $1"
  shift
done

. "$scriptroot/common/tools.sh"
InitializeDotNetCli true

dotnet --list-sdks
dotnet exec artifacts/bin/RunTests/${configuration}/netcoreapp3.1/RunTests.dll --sequential --tfm netcoreapp3.1 --tfm net5.0 --configuration ${configuration} --dotnet ${_InitializeDotNetCli}/dotnet 