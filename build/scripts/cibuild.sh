#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

root_path="$(cd -P "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

# $HOME is unset when running the mac unit tests.
if [[ -z "${HOME+x}" ]]
then
    # Note that while ~ usually refers to $HOME, in the case where $HOME is unset,
    # it looks up the current user's home dir, which is what we want.
    # https://www.gnu.org/software/bash/manual/html_node/Tilde-Expansion.html
    export HOME="$(cd ~ && pwd)"
fi

# There's no reason to send telemetry or prime a local package cach when building
# in CI.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

echo "Building this commit:"
git show --no-patch --pretty=raw HEAD

"${root_path}"/build.sh --restore --bootstrap --build --test "$@"

echo "Killing VBCSCompiler"
dotnet "${root_path}"/Binaries/Bootstrap/bincore/VBCSCompiler.dll -shutdown
