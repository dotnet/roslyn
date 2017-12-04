#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

get_repo_dir()
{
    cd -P "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd
}

# This function will give you the current version number for the specified string in the 
# specified version file.
get_version_core()
{
    local name="${1/./}"
    local name="${name/-/}"
    local version="$(awk -F'[<>]' "/<${name}Version>/{print \$3}" "$2")"
    echo "$version"
}

# This function will give you the current version number for a given nuget package
# based on the contents of Packages.props. 
# 
# Provide the package name in the format shown in the nuget gallery
#   get_package_version dotnet-xunit
#   get_package_version System.Console
get_package_version() 
{
    local repoDir="$(get_repo_dir)"
    local version="$(get_version_core "$1" "${repoDir}"/build/Targets/Packages.props)"
    echo "$version"
}

get_tool_version() 
{
    local repoDir="$(get_repo_dir)"
    local version="$(get_version_core "$1" "${repoDir}"/build/Targets/Tools.props)"
    echo "$version"
}


