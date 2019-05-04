#!/usr/bin/env bash

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

usage()
{
  echo "Common settings:"
  echo "  --configuration <value>    Build configuration: 'Debug' or 'Release' (short: -c)"
  echo "  --verbosity <value>        Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic] (short: -v)"
  echo "  --binaryLog                Create MSBuild binary log (short: -bl)"
  echo "  --help                     Print help and exit (short: -h)"
  echo ""

  echo "Actions:"
  echo "  --restore                  Restore dependencies (short: -r)"
  echo "  --build                    Build solution (short: -b)"
  echo "  --rebuild                  Rebuild solution"
  echo "  --test                     Run all unit tests in the solution (short: -t)"
  echo "  --integrationTest          Run all integration tests in the solution"
  echo "  --performanceTest          Run all performance tests in the solution"
  echo "  --pack                     Package build outputs into NuGet packages and Willow components"
  echo "  --sign                     Sign build outputs"
  echo "  --publish                  Publish artifacts (e.g. symbols)"
  echo ""

  echo "Advanced settings:"
  echo "  --projects <value>       Project or solution file(s) to build"
  echo "  --ci                     Set when running on CI server"
  echo "  --prepareMachine         Prepare machine for CI run, clean up processes after build"
  echo "  --nodeReuse <value>      Sets nodereuse msbuild parameter ('true' or 'false')"
  echo "  --warnAsError <value>    Sets warnaserror msbuild parameter ('true' or 'false')"
  echo ""
  echo "Command line arguments starting with '/p:' are passed through to MSBuild."
  echo "Arguments can also be passed in with a single hyphen."
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
test=false
integration_test=false
performance_test=false
pack=false
publish=false
sign=false
public=false
ci=false

warn_as_error=true
node_reuse=true
binary_log=false

projects=''
configuration='Debug'
prepare_machine=false
verbosity='minimal'

properties=''

while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | awk '{print tolower($0)}')"
  case "$opt" in
    -help|-h)
      usage
      exit 0
      ;;
    -configuration|-c)
      configuration=$2
      shift
      ;;
    -verbosity|-v)
      verbosity=$2
      shift
      ;;
    -binarylog|-bl)
      binary_log=true
      ;;
    -restore|-r)
      restore=true
      ;;
    -build|-b)
      build=true
      ;;
    -rebuild)
      rebuild=true
      ;;
    -pack)
      pack=true
      ;;
    -test|-t)
      test=true
      ;;
    -integrationtest)
      integration_test=true
      ;;
    -performancetest)
      performance_test=true
      ;;
    -sign)
      sign=true
      ;;
    -publish)
      publish=true
      ;;
    -preparemachine)
      prepare_machine=true
      ;;
    -projects)
      projects=$2
      shift
      ;;
    -ci)
      ci=true
      ;;
    -warnaserror)
      warn_as_error=$2
      shift
      ;;
    -nodereuse)
      node_reuse=$2
      shift
      ;;
    -p:*|/p:*)
      properties="$properties $1"
      ;;
    -m:*|/m:*)
      properties="$properties $1"
      ;;
    -bl:*|/bl:*)
      properties="$properties $1"
      ;;
    -dl:*|/dl:*)
      properties="$properties $1"
      ;;
    *)
      echo "Invalid argument: $1"
      usage
      exit 1
      ;;
  esac

  shift
done

if [[ "$ci" == true ]]; then
  binary_log=true
  node_reuse=false
fi

. "$scriptroot/tools.sh"

function InitializeCustomToolset {
  local script="$eng_root/restore-toolset.sh"

  if [[ -a "$script" ]]; then
    . "$script"
  fi
}

function Build {
  InitializeToolset
  InitializeCustomToolset

  if [[ ! -z "$projects" ]]; then
    properties="$properties /p:Projects=$projects"
  fi

  local bl=""
  if [[ "$binary_log" == true ]]; then
    bl="/bl:\"$log_dir/Build.binlog\""
  fi

  MSBuild $_InitializeToolset \
    $bl \
    /p:Configuration=$configuration \
    /p:RepoRoot="$repo_root" \
    /p:Restore=$restore \
    /p:Build=$build \
    /p:Rebuild=$rebuild \
    /p:Test=$test \
    /p:Pack=$pack \
    /p:IntegrationTest=$integration_test \
    /p:PerformanceTest=$performance_test \
    /p:Sign=$sign \
    /p:Publish=$publish \
    $properties

  ExitWithExitCode 0
}

# Import custom tools configuration, if present in the repo.
configure_toolset_script="$eng_root/configure-toolset.sh"
if [[ -a "$configure_toolset_script" ]]; then
  . "$configure_toolset_script"
fi

# TODO: https://github.com/dotnet/arcade/issues/1468
# Temporary workaround to avoid breaking change.
# Remove once repos are updated.
if [[ -n "${useInstalledDotNetCli:-}" ]]; then
  use_installed_dotnet_cli="$useInstalledDotNetCli"
fi

if [[ "$restore" == true && -z ${DisableNativeToolsetInstalls:-} ]]; then
  InitializeNativeTools
fi

Build
