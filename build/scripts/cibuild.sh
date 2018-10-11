#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where 
  # the symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd)"

# $HOME is unset when running the mac unit tests.
if [[ -z "${HOME+x}" ]]
then
    # Note that while ~ usually refers to $HOME, in the case where $HOME is unset,
    # it looks up the current user's home dir, which is what we want.
    # https://www.gnu.org/software/bash/manual/html_node/Tilde-Expansion.html
    export HOME="$(cd ~ && pwd)"
fi

# There's no reason to send telemetry or prime a local package cache when building
# in CI.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

echo "Building this commit:"
git show --no-patch --pretty=raw HEAD


. "$scriptroot/build.sh" --restore --bootstrap --build --pack --stop-vbcscompiler --test --ci "$@"