#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if subcommand fails
set -e

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

# Import Arcade functions
. "$scriptroot/common/tools.sh"

InitializeDotNetCli true

# permissions issues make this a pain to do in PrepareTests itself.
rm -rf "$repo_root/artifacts/testPayload"

dotnet "$repo_root/artifacts/bin/PrepareTests/Debug/net8.0/PrepareTests.dll" --source "$repo_root" --destination "$repo_root/artifacts/testPayload" --unix --dotnetPath ${_InitializeDotNetCli}/dotnet