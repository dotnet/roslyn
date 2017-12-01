#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -e
set -u

usage()
{
    echo "Main interface to running builds on Mac/Linux"
    echo "Usage: build.sh [options]"
    echo ""
    echo "Options"
    echo "  --debug               Build Debug (default)"
    echo "  --release             Build Release"
    echo "  --restore             Restore projects required to build"
    echo "  --build               Build all projects"
    echo "  --test                Run unit tests"
    echo "  --build-bootstrap     Build the bootstrap compilers"
    echo "  --use-bootstrap       Use the built bootstrap compilers when running main build"
    echo "  --bootstrap           Implies --build-bootstrap and --use-bootstrap"
}

root_path="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
binaries_path="${root_path}"/Binaries
bootstrap_path="${binaries_path}"/Bootstrap
bootstrap_framework=netcoreapp2.0

build_configuration=Debug
restore=false
build=false
test_=false
build_bootstrap=false
use_bootstrap=false
stop_vbcscompiler=false

# LTTNG is the logging infrastructure used by coreclr.  Need this variable set
# so it doesn't output warnings to the console.
export LTTNG_HOME="$HOME"

while [[ $# > 0 ]]
do
    opt="$(echo "$1" | awk '{print tolower($0)}')"
    case "$opt" in
        -h|--help)
        usage
        exit 1
        ;;
        --debug)
        build_configuration=Debug
        shift 1
        ;;
        --release)
        build_configuration=Release
        shift 1
        ;;
        --restore|-r)
        restore=true
        shift 1
        ;;
        --build|-b)
        build=true
        shift 1
        ;;
        --test|-t)
        test_=true
        shift 1
        ;;
        --build-bootstrap)
        build_bootstrap=true
        shift 1
        ;;
        --use-bootstrap)
        use_bootstrap=true
        shift 1
        ;;
        --bootstrap)
        build_bootstrap=true
        use_bootstrap=true
        shift 1
        ;;
        --stop-vbcscompiler)
        stop_vbcscompiler=true
        shift 1
        ;;
        *)
        usage
        exit 1
        ;;
    esac
done

source "${root_path}"/build/scripts/obtain_dotnet.sh

if [[ "$restore" == true ]]
then
    "${root_path}"/build/scripts/restore.sh
fi

build_args="--no-restore -c ${build_configuration} /nologo /maxcpucount:1"

if [[ "$build_bootstrap" == true ]]
then
    echo "Building bootstrap toolset"
    bootstrap_build_args="${build_args} /p:UseShippingAssemblyVersion=true /p:InitialDefineConstants=BOOTSTRAP"
    dotnet publish "${root_path}"/src/Compilers/CSharp/csc -o "${bootstrap_path}/bincore" --framework ${bootstrap_framework} ${bootstrap_build_args} "/bl:${binaries_path}/BootstrapCsc.binlog"
    dotnet publish "${root_path}"/src/Compilers/VisualBasic/vbc -o "${bootstrap_path}/bincore" --framework ${bootstrap_framework} ${bootstrap_build_args} "/bl:${binaries_path}/BootstrapVbc.binlog"
    dotnet publish "${root_path}"/src/Compilers/Server/VBCSCompiler -o "${bootstrap_path}/bincore" --framework ${bootstrap_framework} ${bootstrap_build_args} "/bl:${binaries_path}/BootstrapVBCSCompiler.binlog"
    dotnet publish "${root_path}"/src/Compilers/Core/MSBuildTask -o "${bootstrap_path}" ${bootstrap_build_args} "/bl:${binaries_path}/BoostrapMSBuildTask.binlog"
fi

if [[ "${use_bootstrap}" == true ]]
then
    build_args+=" /p:BootstrapBuildPath=${bootstrap_path}"
fi

if [[ "${build}" == true ]]
then
    echo "Building Compilers.sln"
    dotnet build "${root_path}"/Compilers.sln ${build_args} "/bl:${binaries_path}/Build.binlog"
fi

if [[ "${stop_vbcscompiler}" == true ]]
then
    echo "Stopping VBCSCompiler"
    dotnet "${bootstrap_path}"/bincore/VBCSCompiler.dll -shutdown
fi

if [[ "${test_}" == true ]]
then
    "${root_path}"/build/scripts/tests.sh "${build_configuration}"
fi
