#!/usr/bin/env bash

# Initialize variables if they aren't already defined.

# CI mode - set to true on CI server for PR validation build or official build.
ci=${ci:-false}

# Set to true to use the pipelines logger which will enable Azure logging output.
# https://github.com/Microsoft/azure-pipelines-tasks/blob/master/docs/authoring/commands.md
# This flag is meant as a temporary opt-opt for the feature while validate it across
# our consumers. It will be deleted in the future.
if [[ "$ci" == true ]]; then
  pipelines_log=${pipelines_log:-true}
else
  pipelines_log=${pipelines_log:-false}
fi

# Build configuration. Common values include 'Debug' and 'Release', but the repository may use other names.
configuration=${configuration:-'Debug'}

# Set to true to output binary log from msbuild. Note that emitting binary log slows down the build.
# Binary log must be enabled on CI.
binary_log=${binary_log:-$ci}

# Turns on machine preparation/clean up code that changes the machine state (e.g. kills build processes).
prepare_machine=${prepare_machine:-false}

# True to restore toolsets and dependencies.
restore=${restore:-true}

# Adjusts msbuild verbosity level.
verbosity=${verbosity:-'minimal'}

# Set to true to reuse msbuild nodes. Recommended to not reuse on CI.
if [[ "$ci" == true ]]; then
  node_reuse=${node_reuse:-false}
else
  node_reuse=${node_reuse:-true}
fi

# Configures warning treatment in msbuild.
warn_as_error=${warn_as_error:-true}

# True to attempt using .NET Core already that meets requirements specified in global.json 
# installed on the machine instead of downloading one.
use_installed_dotnet_cli=${use_installed_dotnet_cli:-true}

# True to use global NuGet cache instead of restoring packages to repository-local directory.
if [[ "$ci" == true ]]; then
  use_global_nuget_cache=${use_global_nuget_cache:-false}
else
  use_global_nuget_cache=${use_global_nuget_cache:-true}
fi

# Resolve any symlinks in the given path.
function ResolvePath {
  local path=$1

  while [[ -h $path ]]; do
    local dir="$( cd -P "$( dirname "$path" )" && pwd )"
    path="$(readlink "$path")"

    # if $path was a relative symlink, we need to resolve it relative to the path where the
    # symlink file was located
    [[ $path != /* ]] && path="$dir/$path"
  done

  # return value
  _ResolvePath="$path"
}

# ReadVersionFromJson [json key]
function ReadGlobalVersion {
  local key=$1

  local line=`grep -m 1 "$key" "$global_json_file"`
  local pattern="\"$key\" *: *\"(.*)\""

  if [[ ! $line =~ $pattern ]]; then
    Write-PipelineTelemetryError -category 'InitializeTools' "Error: Cannot find \"$key\" in $global_json_file"
    ExitWithExitCode 1
  fi

  # return value
  _ReadGlobalVersion=${BASH_REMATCH[1]}
}

function InitializeDotNetCli {
  if [[ -n "${_InitializeDotNetCli:-}" ]]; then
    return
  fi

  local install=$1

  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  export DOTNET_MULTILEVEL_LOOKUP=0

  # Disable first run since we want to control all package sources
  export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  # Disable telemetry on CI
  if [[ $ci == true ]]; then
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
  fi

  # LTTNG is the logging infrastructure used by Core CLR. Need this variable set
  # so it doesn't output warnings to the console.
  export LTTNG_HOME="$HOME"

  # Source Build uses DotNetCoreSdkDir variable
  if [[ -n "${DotNetCoreSdkDir:-}" ]]; then
    export DOTNET_INSTALL_DIR="$DotNetCoreSdkDir"
  fi

  # Find the first path on $PATH that contains the dotnet.exe
  if [[ "$use_installed_dotnet_cli" == true && $global_json_has_runtimes == false && -z "${DOTNET_INSTALL_DIR:-}" ]]; then
    local dotnet_path=`command -v dotnet`
    if [[ -n "$dotnet_path" ]]; then
      ResolvePath "$dotnet_path"
      export DOTNET_INSTALL_DIR=`dirname "$_ResolvePath"`
    fi
  fi

  ReadGlobalVersion "dotnet"
  local dotnet_sdk_version=$_ReadGlobalVersion
  local dotnet_root=""

  # Use dotnet installation specified in DOTNET_INSTALL_DIR if it contains the required SDK version,
  # otherwise install the dotnet CLI and SDK to repo local .dotnet directory to avoid potential permission issues.
  if [[ $global_json_has_runtimes == false && -n "${DOTNET_INSTALL_DIR:-}" && -d "$DOTNET_INSTALL_DIR/sdk/$dotnet_sdk_version" ]]; then
    dotnet_root="$DOTNET_INSTALL_DIR"
  else
    dotnet_root="$repo_root/.dotnet"

    export DOTNET_INSTALL_DIR="$dotnet_root"

    if [[ ! -d "$DOTNET_INSTALL_DIR/sdk/$dotnet_sdk_version" ]]; then
      if [[ "$install" == true ]]; then
        InstallDotNetSdk "$dotnet_root" "$dotnet_sdk_version"
      else
        Write-PipelineTelemetryError -category 'InitializeToolset' "Unable to find dotnet with SDK version '$dotnet_sdk_version'"
        ExitWithExitCode 1
      fi
    fi
  fi

  # Add dotnet to PATH. This prevents any bare invocation of dotnet in custom
  # build steps from using anything other than what we've downloaded.
  export PATH="$dotnet_root:$PATH"

  if [[ $ci == true ]]; then
    # Make Sure that our bootstrapped dotnet cli is available in future steps of the Azure Pipelines build
    echo "##vso[task.prependpath]$dotnet_root"
    echo "##vso[task.setvariable variable=DOTNET_MULTILEVEL_LOOKUP]0"
    echo "##vso[task.setvariable variable=DOTNET_SKIP_FIRST_TIME_EXPERIENCE]1"
  fi

  # return value
  _InitializeDotNetCli="$dotnet_root"
}

function InstallDotNetSdk {
  local root=$1
  local version=$2
  local architecture=""
  if [[ $# == 3 ]]; then
    architecture=$3
  fi
  InstallDotNet "$root" "$version" $architecture
}

function InstallDotNet {
  local root=$1
  local version=$2
 
  GetDotNetInstallScript "$root"
  local install_script=$_GetDotNetInstallScript

  local archArg=''
  if [[ -n "${3:-}" ]]; then
    archArg="--architecture $3"
  fi
  local runtimeArg=''
  if [[ -n "${4:-}" ]]; then
    runtimeArg="--runtime $4"
  fi

  local skipNonVersionedFilesArg=""
  if [[ "$#" -ge "5" ]]; then
    skipNonVersionedFilesArg="--skip-non-versioned-files"
  fi
  bash "$install_script" --version $version --install-dir "$root" $archArg $runtimeArg $skipNonVersionedFilesArg || {
    local exit_code=$?
    Write-PipelineTelemetryError -category 'InitializeToolset' "Failed to install dotnet SDK (exit code '$exit_code')."
    ExitWithExitCode $exit_code
  }
}

function GetDotNetInstallScript {
  local root=$1
  local install_script="$root/dotnet-install.sh"
  local install_script_url="https://dot.net/v1/dotnet-install.sh"

  if [[ ! -a "$install_script" ]]; then
    mkdir -p "$root"

    echo "Downloading '$install_script_url'"

    # Use curl if available, otherwise use wget
    if command -v curl > /dev/null; then
      curl "$install_script_url" -sSL --retry 10 --create-dirs -o "$install_script"
    else
      wget -q -O "$install_script" "$install_script_url"
    fi
  fi

  # return value
  _GetDotNetInstallScript="$install_script"
}

function InitializeBuildTool {
  if [[ -n "${_InitializeBuildTool:-}" ]]; then
    return
  fi
  
  InitializeDotNetCli $restore

  # return values
  _InitializeBuildTool="$_InitializeDotNetCli/dotnet"  
  _InitializeBuildToolCommand="msbuild"
  _InitializeBuildToolFramework="netcoreapp2.1"
}

function GetNuGetPackageCachePath {
  if [[ -z ${NUGET_PACKAGES:-} ]]; then
    if [[ "$use_global_nuget_cache" == true ]]; then
      export NUGET_PACKAGES="$HOME/.nuget/packages"
    else
      export NUGET_PACKAGES="$repo_root/.packages"
    fi
  fi

  # return value
  _GetNuGetPackageCachePath=$NUGET_PACKAGES
}

function InitializeNativeTools() {
  if grep -Fq "native-tools" $global_json_file
  then
    local nativeArgs=""
    if [[ "$ci" == true ]]; then
      nativeArgs="-InstallDirectory $tools_dir"
    fi
    "$_script_dir/init-tools-native.sh" $nativeArgs
  fi
}

function InitializeToolset {
  if [[ -n "${_InitializeToolset:-}" ]]; then
    return
  fi

  GetNuGetPackageCachePath

  ReadGlobalVersion "Microsoft.DotNet.Arcade.Sdk"

  local toolset_version=$_ReadGlobalVersion
  local toolset_location_file="$toolset_dir/$toolset_version.txt"

  if [[ -a "$toolset_location_file" ]]; then
    local path=`cat "$toolset_location_file"`
    if [[ -a "$path" ]]; then
      # return value
      _InitializeToolset="$path"
      return
    fi
  fi

  if [[ "$restore" != true ]]; then
    Write-PipelineTelemetryError -category 'InitializeToolset' "Toolset version $toolset_version has not been restored."
    ExitWithExitCode 2
  fi

  local proj="$toolset_dir/restore.proj"

  local bl=""
  if [[ "$binary_log" == true ]]; then
    bl="/bl:$log_dir/ToolsetRestore.binlog"
  fi
  
  echo '<Project Sdk="Microsoft.DotNet.Arcade.Sdk"/>' > "$proj"
  MSBuild-Core "$proj" $bl /t:__WriteToolsetLocation /clp:ErrorsOnly\;NoSummary /p:__ToolsetLocationOutputFile="$toolset_location_file"

  local toolset_build_proj=`cat "$toolset_location_file"`

  if [[ ! -a "$toolset_build_proj" ]]; then
    Write-PipelineTelemetryError -category 'InitializeToolset' "Invalid toolset path: $toolset_build_proj"
    ExitWithExitCode 3
  fi

  # return value
  _InitializeToolset="$toolset_build_proj"
}

function ExitWithExitCode {
  if [[ "$ci" == true && "$prepare_machine" == true ]]; then
    StopProcesses
  fi
  exit $1
}

function StopProcesses {
  echo "Killing running build processes..."
  pkill -9 "dotnet" || true
  pkill -9 "vbcscompiler" || true
  return 0
}

function MSBuild {
  local args=$@
  if [[ "$pipelines_log" == true ]]; then
    InitializeBuildTool
    InitializeToolset
    local toolset_dir="${_InitializeToolset%/*}"
    local logger_path="$toolset_dir/$_InitializeBuildToolFramework/Microsoft.DotNet.Arcade.Sdk.dll"
    args=( "${args[@]}" "-logger:$logger_path" )
  fi

  MSBuild-Core ${args[@]}
}

function MSBuild-Core {
  if [[ "$ci" == true ]]; then
    if [[ "$binary_log" != true ]]; then
      Write-PipelineTaskError "Binary log must be enabled in CI build."
      ExitWithExitCode 1
    fi

    if [[ "$node_reuse" == true ]]; then
      Write-PipelineTaskError "Node reuse must be disabled in CI build."
      ExitWithExitCode 1
    fi
  fi

  InitializeBuildTool

  local warnaserror_switch=""
  if [[ $warn_as_error == true ]]; then
    warnaserror_switch="/warnaserror"
  fi

  "$_InitializeBuildTool" "$_InitializeBuildToolCommand" /m /nologo /clp:Summary /v:$verbosity /nr:$node_reuse $warnaserror_switch /p:TreatWarningsAsErrors=$warn_as_error /p:ContinuousIntegrationBuild=$ci "$@" || {
    local exit_code=$?
    Write-PipelineTaskError "Build failed (exit code '$exit_code')."
    ExitWithExitCode $exit_code
  }
}

. "$scriptroot/pipeline-logging-functions.sh"

ResolvePath "${BASH_SOURCE[0]}"
_script_dir=`dirname "$_ResolvePath"`

eng_root=`cd -P "$_script_dir/.." && pwd`
repo_root=`cd -P "$_script_dir/../.." && pwd`
artifacts_dir="$repo_root/artifacts"
toolset_dir="$artifacts_dir/toolset"
tools_dir="$repo_root/.tools"
log_dir="$artifacts_dir/log/$configuration"
temp_dir="$artifacts_dir/tmp/$configuration"

global_json_file="$repo_root/global.json"
# determine if global.json contains a "runtimes" entry
global_json_has_runtimes=false
dotnetlocal_key=`grep -m 1 "runtimes" "$global_json_file"` || true
if [[ -n "$dotnetlocal_key" ]]; then
  global_json_has_runtimes=true
fi

# HOME may not be defined in some scenarios, but it is required by NuGet
if [[ -z $HOME ]]; then
  export HOME="$repo_root/artifacts/.home/"
  mkdir -p "$HOME"
fi

mkdir -p "$toolset_dir"
mkdir -p "$temp_dir"
mkdir -p "$log_dir"

if [[ $ci == true ]]; then
  export TEMP="$temp_dir"
  export TMP="$temp_dir"
fi
