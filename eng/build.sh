#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if subcommand fails
set -e

usage()
{
  echo "Common settings:"
  echo "  --configuration <value>    Build configuration: 'Debug' or 'Release' (short: -c)"
  echo "  --verbosity <value>        Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic] (short: -v)"
  echo "  --binaryLog                Create MSBuild binary log (short: -bl)"
  echo ""
  echo "Actions:"
  echo "  --restore                  Restore projects required to build (short: -r)"
  echo "  --build                    Build all projects (short: -b)"
  echo "  --rebuild                  Rebuild all projects"
  echo "  --pack                     Build nuget packages"
  echo "  --publish                  Publish build artifacts"
  echo "  --sign                     Sign build artifacts"
  echo "  --help                     Print help and exit"
  echo ""
  echo "Test actions:"
  echo "  --testFramework <value>    Test framework to run: core, desktop, or both (short: --test, -t implies core)"
  echo "  --testMono                 Run unit tests on Mono (deprecated, will be removed in .NET 11)"
  echo "  --testPlatform <value>     Architecture to test on: x86, x64 or arm64 (default: x64)"
  echo "  --testSet <value>          Test set to run: compiler"
  echo "  --testKind <value>         Test kind: ioperation, runtimeasync, usedassemblies"
  echo ""
  echo "Advanced settings:"
  echo "  --ci                       Building in CI"
  echo "  --bootstrap                Build using a bootstrap compilers"
  echo "  --runAnalyzers             Run analyzers during build operations"
  echo "  --skipDocumentation        Skip generation of XML documentation files"
  echo "  --prepareMachine           Prepare machine for CI run, clean up processes after build"
  echo "  --warnAsError              Treat all warnings as errors"
  echo "  --warnNotAsError <codes>   Suppress specific warnings from being treated as errors (semi-colon delimited)"
  echo "  --sourceBuild              Build the repository in source-only mode"
  echo "  --productBuild             Build the repository in product-build mode."
  echo "  --fromVMR                  Build the repository in product-build mode."
  echo "  --solution                 Solution to build (default is Roslyn.slnx)"
  echo ""
  echo "Command line arguments starting with '/p:' are passed through to MSBuild."
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

restore=false
build=false
rebuild=false
pack=false
sign=false
publish=false
test_framework=""
test_mono=false
test_platform="x64"
test_set=""
test_kind=""

configuration="Debug"
verbosity='minimal'
binary_log=false
ci=false
bootstrap=false
run_analyzers=false
skip_documentation=false
prepare_machine=false
warn_as_error=false
warn_not_as_error=""
properties=()
source_build=false
product_build=false
from_vmr=false
solution_to_build="Roslyn.slnx"

args=""

if [[ $# = 0 ]]
then
  usage
  exit 1
fi

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
    --verbosity|-v)
      verbosity=$2
      args="$args $1"
      shift
      ;;
    --binarylog|-bl)
      binary_log=true
      ;;
    --restore|-r)
      restore=true
      ;;
    --build|-b)
      build=true
      ;;
    --rebuild)
      rebuild=true
      ;;
    --pack)
      pack=true
      ;;
    --publish)
      publish=true
      ;;
    --sign)
      sign=true
      ;;
    --testframework)
      test_framework=$2
      shift
      ;;
    --test|-t)
      test_framework="core"
      ;;
    --testmono)
      test_mono=true
      ;;
    --testplatform)
      test_platform=$2
      shift
      ;;
    --testset)
      test_set=$2
      shift
      ;;
    --testkind)
      test_kind=$2
      shift
      ;;
    --ci)
      ci=true
      ;;
    --bootstrap)
      bootstrap=true
      # Bootstrap requires restore
      restore=true
      ;;
    --runanalyzers)
      run_analyzers=true
      ;;
    --skipdocumentation)
      skip_documentation=true
      ;;
    --preparemachine)
      prepare_machine=true
      ;;
    --warnaserror)
      warn_as_error=true
      ;;
    --warnnotaserror)
      warn_not_as_error=$2
      args="$args $1"
      shift
      ;;
    --sourcebuild|--source-build|-sb)
      source_build=true
      product_build=true
      ;;
    --productbuild|--product-build|-pb)
      product_build=true
      ;;
    --fromvmr|--from-vmr)
      from_vmr=true
      ;;
    --solution)
      solution_to_build=$2
      args="$args $1"
      shift
      ;;
    /p:*)
      properties+=("$1")
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

# Import Arcade functions
. "$scriptroot/common/tools.sh"

function MakeBootstrapBuild {
  echo "Building bootstrap compiler"

  local dir="$artifacts_dir/Bootstrap"

  rm -rf $dir
  mkdir -p $dir

  local package_name="Microsoft.Net.Compilers.Toolset"
  local project_path=src/NuGet/$package_name/AnyCpu/$package_name.Package.csproj

  dotnet pack -nologo "$project_path" -p:ContinuousIntegrationBuild=$ci -p:DotNetUseShippingVersions=true -p:InitialDefineConstants=BOOTSTRAP -p:PackageOutputPath="$dir" -bl:"$log_dir/Bootstrap.binlog"
  unzip "$dir/$package_name.*.nupkg" -d "$dir"
  chmod -R 755 "$dir"

  echo "Cleaning Bootstrap compiler artifacts"
  dotnet clean "$project_path"

  if [[ "$node_reuse" == true ]]; then
    dotnet build-server shutdown
  fi

  # return value
  _MakeBootstrapBuild=$dir
}

function BuildSolution {
  local solution=$solution_to_build
  echo "$solution:"

  InitializeToolset
  local toolset_build_proj=$_InitializeToolset

  local bl=""
  if [[ "$binary_log" = true ]]; then
    bl="/bl:\"$log_dir/Build.binlog\""
    export RoslynCommandLineLogFile="$log_dir/vbcscompiler.log"
  fi

  local projects="$repo_root/$solution"

  UNAME="$(uname)"
  # NuGet often exceeds the limit of open files on Mac and Linux
  # https://github.com/NuGet/Home/issues/2163
  if [[ "$UNAME" == "Darwin" || "$UNAME" == "Linux" ]]; then
    ulimit -n 6500 || echo "Cannot change ulimit"
  fi

  local test=false
  local test_runtime=""
  local mono_tool=""
  local test_runtime_args=""
  if [[ "$test_mono" == true ]]; then
    mono_path=`command -v mono`
    # Echo out the mono version to the command line so it's visible in CI logs. It's not fixed
    # as we're using a feed vs. a hard coded package.
    if [[ "$ci" == true ]]; then
      mono --version
    fi

    test=true
    test_runtime="/p:TestRuntime=Mono"
    mono_tool="/p:MonoTool=\"$mono_path\""
    test_runtime_args="--debug"
  fi

  local msbuild_warn_as_error=""
  if [[ "$warn_as_error" == true ]]; then
    msbuild_warn_as_error="/warnAsError"
  fi

  local msbuild_warn_not_as_error=""
  if [[ "$warn_not_as_error" != "" && "$warn_as_error" == true ]]; then
    msbuild_warn_not_as_error="/warnNotAsError:$warn_not_as_error"
  fi

  local generate_documentation_file=""
  if [[ "$skip_documentation" == true ]]; then
    generate_documentation_file="/p:GenerateDocumentationFile=false"
  fi

  local roslyn_use_hard_links=""
  if [[ "$ci" == true ]]; then
    roslyn_use_hard_links="/p:ROSLYNUSEHARDLINKS=true"
  fi

  MSBuild $toolset_build_proj \
    $bl \
    /p:Configuration=$configuration \
    /p:Projects="$projects" \
    /p:RepoRoot="$repo_root" \
    /p:Restore=$restore \
    /p:Build=$build \
    /p:Rebuild=$rebuild \
    /p:Test=$test \
    /p:Pack=$pack \
    /p:Publish=$publish \
    /p:Sign=$sign \
    /p:RunAnalyzersDuringBuild=$run_analyzers \
    /p:BootstrapBuildPath="$bootstrap_dir" \
    /p:ContinuousIntegrationBuild=$ci \
    /p:TreatWarningsAsErrors=$warn_as_error \
    /p:TestRuntimeAdditionalArguments=$test_runtime_args \
    /p:DotNetBuildSourceOnly=$source_build \
    /p:DotNetBuild=$product_build \
    /p:DotNetBuildFromVMR=$from_vmr \
    $test_runtime \
    $mono_tool \
    $msbuild_warn_as_error \
    $msbuild_warn_not_as_error \
    $generate_documentation_file \
    $roslyn_use_hard_links \
    ${properties[@]+"${properties[@]}"}
}

install=false
if [[ "$restore" == true || -n "$test_framework" ]]; then
  install=true
fi
InitializeDotNetCli $install
# Source only builds would not have 'dotnet' ambiently available.
if [[ "$restore" == true && "$source_build" != true ]]; then
  dotnet tool restore
fi

bootstrap_dir=""
if [[ "$bootstrap" == true ]]; then
  MakeBootstrapBuild
  bootstrap_dir=$_MakeBootstrapBuild
fi

if [[ "$restore" == true || "$build" == true || "$rebuild" == true || "$test_mono" == true ]]; then
  BuildSolution
fi

if [[ -n "$test_framework" ]]; then
  runtests_args="--testFramework:$test_framework"
  runtests_args="$runtests_args --testConfiguration $configuration"
  runtests_args="$runtests_args --logs ${log_dir}"
  runtests_args="$runtests_args --dotnet ${_InitializeDotNetCli}/dotnet"
  runtests_args="$runtests_args --testPlatform $test_platform"

  if [[ -n "$test_set" ]]; then
    runtests_args="$runtests_args --testSet:$test_set"
  fi

  if [[ -n "$test_kind" ]]; then
    runtests_args="$runtests_args --testKind:$test_kind"
  fi

  if [[ "$ci" == true ]]; then
    runtests_args="$runtests_args --ci"
    runtests_args="$runtests_args --timeout 90"
  else
    runtests_args="$runtests_args --html"
  fi

  dotnet exec "$scriptroot/../artifacts/bin/RunTests/${configuration}/net10.0/RunTests.dll" $runtests_args
fi
ExitWithExitCode 0
