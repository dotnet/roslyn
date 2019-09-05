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
  echo "  --help                     Print help and exit"
  echo ""
  echo "Test actions:"     
  echo "  --testCoreClr              Run unit tests on .NET Core (short: --test, -t)"
  echo "  --testMono                 Run unit tests on Mono"
  echo ""
  echo "Advanced settings:"
  echo "  --ci                       Building in CI"
  echo "  --docker                   Run in a docker container if applicable"
  echo "  --bootstrap                Build using a bootstrap compilers"
  echo "  --skipAnalyzers            Do not run analyzers during build operations"
  echo "  --prepareMachine           Prepare machine for CI run, clean up processes after build"
  echo "  --warnAsError              Treat all warnings as errors"
  echo "  --sourceBuild              Simulate building for source-build"
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
publish=false
test_core_clr=false
test_mono=false

configuration="Debug"
verbosity='minimal'
binary_log=false
ci=false
bootstrap=false
skip_analyzers=false
prepare_machine=false
warn_as_error=false
properties=""
disable_parallel_restore=false
source_build=false

docker=false
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
    --testcoreclr|--test|-t)
      test_core_clr=true
      ;;
    --testmono)
      test_mono=true
      ;;
    --ci)
      ci=true
      ;;
    --bootstrap)
      bootstrap=true
      # Bootstrap requires restore
      restore=true
      ;;
    --skipanalyzers)
      skip_analyzers=true
      ;;
    --preparemachine)
      prepare_machine=true
      ;;
    --warnaserror)
      warn_as_error=true
      ;;
    --docker)
      docker=true
      shift
      continue
      ;;
    --sourcebuild)
      source_build=true
      ;;
    /p:*)
      properties="$properties $1"
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

if [[ "$docker" == true ]]
then
  echo "Docker exec: $args"

  # Run this script with the same arguments (except for --docker) in a container that has Mono installed.
  BUILD_COMMAND=/opt/code/eng/build.sh "$scriptroot"/docker/mono.sh $args
  lastexitcode=$?
  if [[ $lastexitcode != 0 ]]; then
    echo "Docker build failed (exit code '$lastexitcode')." >&2
    exit $lastexitcode
  fi

  # Ensure that all docker containers are stopped.
  # Hence exit with true even if "kill" failed as it will fail if they stopped gracefully
  if [[ "$prepare_machine" == true ]]; then
    docker kill $(docker ps -q) || true
  fi

  exit
fi

# Import Arcade functions
. "$scriptroot/common/tools.sh"

function MakeBootstrapBuild {
  echo "Building bootstrap compiler"

  local dir="$artifacts_dir/Bootstrap"

  rm -rf $dir
  mkdir -p $dir

  local package_name="Microsoft.Net.Compilers.Toolset"
  local project_path=src/NuGet/$package_name/$package_name.Package.csproj

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
  local solution="Compilers.sln"
  if [[ "$source_build" == true ]]; then
    solution="SourceBuild.sln"
  fi
  echo "$solution:"

  InitializeToolset
  local toolset_build_proj=$_InitializeToolset
  
  local bl=""
  if [[ "$binary_log" = true ]]; then
    bl="/bl:\"$log_dir/Build.binlog\""
  fi
  
  local projects="$repo_root/$solution" 
  
  # https://github.com/dotnet/roslyn/issues/23736
  local enable_analyzers=!$skip_analyzers
  UNAME="$(uname)"
  if [[ "$UNAME" == "Darwin" ]]; then
    enable_analyzers=false
  fi

  # NuGet often exceeds the limit of open files on Mac and Linux
  # https://github.com/NuGet/Home/issues/2163
  if [[ "$UNAME" == "Darwin" || "$UNAME" == "Linux" ]]; then
    disable_parallel_restore=true
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
  elif [[ "$test_core_clr" == true ]]; then
    test=true
    test_runtime="/p:TestRuntime=Core /p:TestTargetFrameworks=netcoreapp3.0%3Bnetcoreapp2.1"
    mono_tool=""
  fi

  # Setting /p:TreatWarningsAsErrors=true is a workaround for https://github.com/Microsoft/msbuild/issues/3062.
  # We don't pass /warnaserror to msbuild (warn_as_error is set to false by default above), but set 
  # /p:TreatWarningsAsErrors=true so that compiler reported warnings, other than IDE0055 are treated as errors. 
  # Warnings reported from other msbuild tasks are not treated as errors for now.
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
    /p:UseRoslynAnalyzers=$enable_analyzers \
    /p:BootstrapBuildPath="$bootstrap_dir" \
    /p:ContinuousIntegrationBuild=$ci \
    /p:TreatWarningsAsErrors=true \
    /p:RestoreDisableParallel=$disable_parallel_restore \
    /p:TestRuntimeAdditionalArguments=$test_runtime_args \
    /p:DotNetBuildFromSource=$source_build \
    $test_runtime \
    $mono_tool \
    $properties
}

InitializeDotNetCli $restore

# Make sure we have a 2.1 runtime available for running our tests
InstallDotNetSdk $_InitializeDotNetCli 2.1.503

bootstrap_dir=""
if [[ "$bootstrap" == true ]]; then
  MakeBootstrapBuild
  bootstrap_dir=$_MakeBootstrapBuild
fi

BuildSolution
ExitWithExitCode 0
