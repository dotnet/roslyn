#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if subcommand fails
set -e

source="${BASH_SOURCE[0]}"

configuration="Debug"
is_unix=false

while [[ $# > 0 ]]; do
  opt="$(echo "$1" | awk '{print tolower($0)}')"
  case "$opt" in
    --configuration|-c)
      configuration=$2
      shift
      ;;
    --unix)
      is_unix=true
      ;;
    *)
      echo "Invalid argument: $1"
      usage
      exit 1
      ;;
  esac
  shift
done

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

# Import Arcade functions
. "$scriptroot/common/tools.sh"

InitializeDotNetCli true

# permissions issues make this a pain to do in PrepareTests itself.
rm -rf "$repo_root/artifacts/testPayload"

if [[ "$is_unix" == true ]]; then
  dotnet "$repo_root/artifacts/bin/PrepareTests/$configuration/net6.0/PrepareTests.dll" --source "$repo_root" --destination "$repo_root/artifacts/testPayload" --unix --dotnetPath ${_InitializeDotNetCli}/dotnet
fi

if [[ "$is_unix" == false ]]; then
  dotnet "$repo_root/artifacts/bin/PrepareTests/$configuration/net6.0/PrepareTests.dll" --source "$repo_root" --destination "$repo_root/artifacts/testPayload" --dotnetPath ${_InitializeDotNetCli}/dotnet
fi
