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
  echo "  --testCompilerOnly         Run only the compiler unit tests"
  echo "  --testIOperation           Run unit tests with the IOperation test hook"
  echo ""
  echo "Advanced settings:"
  echo "  --ci                       Building in CI"
  echo "  --bootstrap                Build using a bootstrap compilers"
  echo "  --runAnalyzers             Run analyzers during build operations"
  echo "  --skipDocumentation        Skip generation of XML documentation files"
  echo "  --prepareMachine           Prepare machine for CI run, clean up processes after build"
  echo "  --warnAsError              Treat all warnings as errors"
  echo "  --sourceBuild              Simulate building for source-build"
  echo "  --solution                 Soluton to build (Default is Compilers.slnf)"
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
test_ioperation=false
test_compiler_only=false

configuration="Debug"
verbosity='minimal'
binary_log=false
ci=false
helix=false
helix_queue_name=""
helix_api_access_token=""
bootstrap=false
run_analyzers=false
skip_documentation=false
prepare_machine=false
warn_as_error=false
properties=""
source_build=false
restoreUseStaticGraphEvaluation=true
solution_to_build="Compilers.slnf"
runtime_source_feed=''
runtime_source_feed_key=''

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
    --testcompileronly)
      test_compiler_only=true
      ;;
    --testioperation)
      test_ioperation=true
      ;;
    --ci)
      ci=true
      ;;
    --helix)
      helix=true
      ;;
    --helixqueuename)
      helix_queue_name=$2
      args="$args $1"
      shift
      ;;
    --helixapiaccesstoken)
      helix_api_access_token=$2
      args="$args $1"
      shift
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
    --sourcebuild|/p:arcadebuildfromsource=true)
      # Arcade specifies /p:ArcadeBuildFromSource=true instead of --sourceBuild, but that's not developer friendly so we
      # have an alias.
      source_build=true
      # RestoreUseStaticGraphEvaluation will cause prebuilts
      restoreUseStaticGraphEvaluation=false
      ;;
    --solution)
      solution_to_build=$2
      args="$args $1"
      shift
      ;;
    # runtimesourcefeed and runtimesourcefeedkey are here to eat parameters passed by source-build when its run with enableInternalSources. These
    # parameters are not required by roslyn as it does not install private runtimes (just uses private packages).
    --runtimesourcefeed)
      runtime_source_feed=$2
      shift
      ;;
    --runtimesourcefeedkey)
      runtime_source_feed_key=$2
      shift
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

  if [[ "$test_ioperation" == true ]]; then
    export ROSLYN_TEST_IOPERATION="true"

    if [[ "$test_mono" != true && "$test_core_clr" != true ]]; then
      test_core_clr=true
    fi
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

  local generate_documentation_file=""
  if [[ "$skip_documentation" == true ]]; then
    generate_documentation_file="/p:GenerateDocumentationFile=false"
  fi

  local roslyn_use_hard_links=""
  if [[ "$ci" == true ]]; then
    roslyn_use_hard_links="/p:ROSLYNUSEHARDLINKS=true"
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
    /p:RunAnalyzersDuringBuild=$run_analyzers \
    /p:RestoreUseStaticGraphEvaluation=$restoreUseStaticGraphEvaluation \
    /p:BootstrapBuildPath="$bootstrap_dir" \
    /p:ContinuousIntegrationBuild=$ci \
    /p:TreatWarningsAsErrors=true \
    /p:TestRuntimeAdditionalArguments=$test_runtime_args \
    /p:ArcadeBuildFromSource=$source_build \
    $test_runtime \
    $mono_tool \
    $generate_documentation_file \
    $roslyn_use_hard_links \
    $properties
}

function GetCompilerTestAssembliesIncludePaths {
  assemblies="--include '^Microsoft\.CodeAnalysis\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CompilerServer\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CSharp\.Syntax\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CSharp\.Symbol\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CSharp\.Semantic\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CSharp\.Emit\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CSharp\.Emit2\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CSharp\.Emit3\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CSharp\.IOperation\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.CSharp\.CommandLine\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.VisualBasic\.Syntax\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.VisualBasic\.Symbol\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.VisualBasic\.Semantic\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.VisualBasic\.Emit\.UnitTests$'"
  assemblies+=" --include '^Roslyn\.Compilers\.VisualBasic\.IOperation\.UnitTests$'"
  assemblies+=" --include '^Microsoft\.CodeAnalysis\.VisualBasic\.CommandLine\.UnitTests$'"
  echo "$assemblies"
}

install=false
if [[ "$restore" == true || "$test_core_clr" == true ]]; then
  install=true
fi
InitializeDotNetCli $install
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

if [[ "$test_core_clr" == true ]]; then
  runtests_args=""

  if [[ -n "$test_compiler_only" ]]; then
    runtests_args="$runtests_args $(GetCompilerTestAssembliesIncludePaths)"
  fi

  if [[ -n "$helix_queue_name" ]]; then
    runtests_args="$runtests_args --helixQueueName $helix_queue_name"
  fi

  if [[ -n "$helix_api_access_token" ]]; then
    runtests_args="$runtests_args --helixApiAccessToken $helix_api_access_token"
  fi

  if [[ "$helix" == true ]]; then
    runtests_args="$runtests_args --helix"
  fi

  if [[ "$ci" != true ]]; then
    runtests_args="$runtests_args --html"
  fi
  dotnet exec "$scriptroot/../artifacts/bin/RunTests/${configuration}/net8.0/RunTests.dll" --runtime core --configuration ${configuration} --logs ${log_dir} --dotnet ${_InitializeDotNetCli}/dotnet $runtests_args
fi
ExitWithExitCode 0
