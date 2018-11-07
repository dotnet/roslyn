#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

if [[ "${1}" =~ ^(-h|-help|--help|-\?|/\?)$ ]]; then
    echo "USAGE:"
    echo "build/scripts/tests.sh (Debug|Release) (dotnet|mono|mono-debug) [test assembly name] [xunit args]"
    echo "If specified, the test assembly name must be a substring match for one or more test assemblies."
    echo "Note that it's a substring match so '.dll' would match all unit test DLLs and run them all."
    echo "Any xunit args specified after the assembly name will be passed directly to the test runner so you can run individual tests, i.e. -method \"*.Query_01\""

    exit 1
fi

build_configuration=${1:-Debug}
runtime=${2:-dotnet}
single_test_assembly=${3:-}
xunit_args=(${@:4})

was_argv_specified=0
[[ "${single_test_assembly}" != "" ]] && was_argv_specified=1

this_dir="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${this_dir}"/build-utils.sh

root_path="$(get_repo_dir)"
binaries_path="${root_path}"/Binaries
unittest_dir="${binaries_path}"/"${build_configuration}"/UnitTests
log_dir="${binaries_path}"/"${build_configuration}"/xUnitResults
nuget_dir="${HOME}"/.nuget/packages
xunit_console_version="$(get_package_version xunitrunnerconsole)"
dotnet_runtime_version="$(get_tool_version dotnetRuntime)"

if [[ "${runtime}" == "dotnet" ]]; then
    file_list=( "${unittest_dir}"/*/netcoreapp2.1/*.UnitTests.dll )
    file_skiplist=(
        # Disable the VB Semantic tests while we investigate the core dump issue
        # https://github.com/dotnet/roslyn/issues/29660
        "Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests.dll"
    )
    xunit_console="${nuget_dir}"/xunit.runner.console/"${xunit_console_version}"/tools/netcoreapp2.0/xunit.console.dll
elif [[ "${runtime}" =~ ^(mono|mono-debug)$ ]]; then
    file_list=( "${unittest_dir}"/*/net472/*.UnitTests.dll )
    file_skiplist=(
        # Omitted because we appear to be missing things necessary to compile vb.net.
        # See https://github.com/mono/mono/issues/10679
        'Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests.dll'
        'Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests.dll'
        # PortablePdb and lots of other problems
        'Microsoft.CodeAnalysis.VisualBasic.Scripting.UnitTests.dll'
        # Many test failures
        'Microsoft.CodeAnalysis.UnitTests.dll'
        # Multiple test failures
        'Microsoft.Build.Tasks.CodeAnalysis.UnitTests.dll'
        # Disabling on assumption
        'Microsoft.CodeAnalysis.VisualBasic.Emit.UnitTests.dll'
        # A zillion test failures + crash
        # See https://github.com/mono/mono/issues/10756
        'Microsoft.CodeAnalysis.VisualBasic.Symbol.UnitTests.dll'
        # Currently fails on CI against old versions of mono
        # See https://github.com/dotnet/roslyn/pull/30166#issuecomment-425571629
        'VBCSCompiler.UnitTests.dll'
        # Mono serialization errors breaking tests that have traits
        # https://github.com/mono/mono/issues/10945
        'Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.dll'
    )
    xunit_console="${nuget_dir}"/xunit.runner.console/"${xunit_console_version}"/tools/net452/xunit.console.exe
else
    echo "Unknown runtime: ${runtime}"
    exit 1
fi

echo "Using ${xunit_console}"

# Discover and run the tests
mkdir -p "${log_dir}"

exit_code=0
for file_name in "${file_list[@]}"
do
    file_base_name=$(basename "${file_name}")
    log_file="${log_dir}/${file_base_name%.*}.xml"
    deps_json="${file_name%.*}".deps.json
    runtimeconfig_json="${file_name%.*}".runtimeconfig.json

    is_argv_match=0
    [[ "${file_name}" =~ "${single_test_assembly}" ]] && is_argv_match=1

    is_skiplist_match=0
    [[ "${file_skiplist[@]}" =~ "${file_base_name}" ]] && is_skiplist_match=1

    if (( is_skiplist_match && ! (is_argv_match && was_argv_specified) ))
    then
        echo "Skipping listed ${file_base_name}"
        continue
    fi

    if (( was_argv_specified && ! is_argv_match ))
    then
        echo "Skipping ${file_base_name} to run single test"
        continue
    fi

    echo Running "${runtime} ${file_name}"
    if [[ "${runtime}" == "dotnet" ]]; then
        # Disable the VB Semantic tests while we investigate the core dump issue
        # https://github.com/dotnet/roslyn/issues/29660
        if [[ "${file_name[@]}" == *'Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests.dll' ||
              "${file_name[@]}" == *'Microsoft.CodeAnalysis.CSharp.WinRT.UnitTests.dll' ]]
        then
            echo "Skipping ${file_name[@]}"
            continue
        fi
        runner="dotnet exec --fx-version ${dotnet_runtime_version} --depsfile ${deps_json} --runtimeconfig ${runtimeconfig_json}"
    elif [[ "${runtime}" == "mono" ]]; then
        runner=mono
    elif [[ "${runtime}" == "mono-debug" ]]; then
        runner="mono --debug"
    fi

    # https://github.com/dotnet/roslyn/issues/29380
    if ${runner} "${xunit_console}" "${file_name}" -xml "${log_file}" -parallel none ${xunit_args[@]+"${xunit_args[@]}"}
    then
        echo "Assembly ${file_name} passed"
    else
        echo "Assembly ${file_name} failed"
        exit_code=1
    fi
done
exit ${exit_code}
