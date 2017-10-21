#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

build_configuration=${1:-Debug}

this_dir="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${this_dir}"/build-utils.sh

root_path="$(get_repo_dir)"
binaries_path="${root_path}"/Binaries
unittest_dir="${binaries_path}"/"${build_configuration}"/UnitTests
log_dir="${binaries_path}"/"${build_configuration}"/xUnitResults
nuget_dir="${HOME}"/.nuget/packages
runtime_id="$(dotnet --info | awk '/RID:/{print $2;}')"
target_framework=netcoreapp2.0
xunit_console_version="$(get_package_version dotnet-xunit)"
xunit_console="${nuget_dir}"/dotnet-xunit/"${xunit_console_version}"/tools/"${target_framework}"/xunit.console.dll

echo "Using ${xunit_console}"

# Need to publish projects that have runtime assets before running tests
need_publish=(
    'src/Compilers/CSharp/Test/Symbol/CSharpCompilerSymbolTest.csproj'
)

for project in "${need_publish[@]}"
do
    echo "Publishing ${project}"
    dotnet publish --no-restore "${root_path}"/"${project}" -r "${runtime_id}" -f "${target_framework}" -p:SelfContained=true
done

# Discover and run the tests
mkdir -p "${log_dir}"

for test_path in "${unittest_dir}"/*/"${target_framework}"
do
    publish_test_path="${test_path}"/"${runtime_id}"/publish
    if [ -d "${publish_test_path}" ]
    then
        test_path="${publish_test_path}"
    fi

    file_name=( "${test_path}"/*.UnitTests.dll )
    log_file="${log_dir}"/"$(basename "${file_name%.*}.xml")"
    deps_json="${file_name%.*}".deps.json
    runtimeconfig_json="${file_name%.*}".runtimeconfig.json
    echo Running "${file_name[@]}"
    dotnet exec --depsfile "${deps_json}" --runtimeconfig "${runtimeconfig_json}" "${xunit_console}" "${file_name[@]}" -xml "${log_file}"
    if [[ $? -ne 0 ]]; then
        echo Unit test failed
        exit 1
    fi
done
