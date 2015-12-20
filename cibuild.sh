#!/usr/bin/env bash

usage()
{
    echo "Runs our integration suite on Linux"
    echo "usage: cibuild.sh [options]"
    echo ""
    echo "Options"
    echo "  --mono-path <path>  Path to the mono installation to use for the run" 
    echo "  --os <os>           OS to run (Linux / Darwin)"
}

XUNIT_VERSION=2.1.0
BUILD_CONFIGURATION=Debug
OS_NAME=$(uname -s)
USE_CACHE=true
MONO_ARGS='--debug=mdb-optimizations --attach=disable'
MSBUILD_ADDITIONALARGS='/v:m /consoleloggerparameters:Verbosity=minimal /filelogger /fileloggerparameters:Verbosity=normal'

# LTTNG is the logging infrastructure used by coreclr.  Need this variable set 
# so it doesn't output warnings to the console.
export LTTNG_HOME=$HOME
export MONO_THREADS_PER_CPU=50

# There are some stability issues that are causing Jenkins builds to fail at an 
# unacceptable rate.  To temporarily work around that we are going to retry the 
# unstable tasks a number of times.  
RETRY_COUNT=5

while [[ $# > 0 ]]
do
    opt="$1"
    case $opt in
        -h|--help)
        usage
        exit 1
        ;;
        --mono-path)
        CUSTOM_MONO_PATH=$2
        shift 2
        ;;
        --os)
        OS_NAME=$2
        shift 2
        ;;
        --debug)
        BUILD_CONFIGURATION=Debug
        shift 1
        ;;
        --release)
        BUILD_CONFIGURATION=Release
        shift 1
        ;;
        --nocache)
        USE_CACHE=false
        shift 1
        ;;
        *)
        usage 
        exit 1
        ;;
    esac
done

set_build_info()
{
    if [ "$OS_NAME" == "Linux" ]; then
        MSBUILD_ADDITIONALARGS="$MSBUILD_ADDITIONALARGS /p:BaseNuGetRuntimeIdentifier=ubuntu.14.04"
        MONO_TOOLSET_NAME=mono.linux.4
        ROSLYN_TOOLSET_NAME=roslyn.linux.1
    elif [ "$OS_NAME" == "Darwin" ]; then
        MSBUILD_ADDITIONALARGS="$MSBUILD_ADDITIONALARGS /p:BaseNuGetRuntimeIdentifier=osx.10.10"
        MONO_TOOLSET_NAME=mono.mac.5
        ROSLYN_TOOLSET_NAME=roslyn.mac.1
    else
        echo Unrecognized OS $OS_NAME
        exit 1
    fi
}

restore_nuget()
{
    local package_name="nuget.40.zip"
    local target="/tmp/$package_name"
    echo "Installing NuGet Packages $target"
    if [ -f $target ]; then
        if [ "$USE_CACHE" = "true" ]; then
            echo "Nuget already installed"
            return
        fi
    fi

    pushd /tmp/

    rm $package_name 2>/dev/null
    curl -O https://dotnetci.blob.core.windows.net/roslyn/$package_name
    unzip -uoq $package_name -d ~/
    if [ $? -ne 0 ]; then
        echo "Unable to download NuGet packages"
        exit 1
    fi

    popd

}

run_msbuild()
{
    local is_good=false
    
    for i in `seq 1 $RETRY_COUNT`
    do
        mono $MONO_ARGS ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0-prerelease/lib/MSBuild.exe $MSBUILD_ADDITIONALARGS /p:SignAssembly=false /p:DebugSymbols=false /p:Configuration=$BUILD_CONFIGURATION "$@"
        if [ $? -eq 0 ]; then
            is_good=true
            break
        fi

        echo Build retry $i
    done

    if [ "$is_good" != "true" ]; then
        echo Build failed
        exit 1
    fi
}

# NuGet crashes on occasion during restore.  This isn't a fatal action so 
# we re-run it a number of times.  
run_nuget()
{
    local is_good=false
    for i in `seq 1 $RETRY_COUNT`
    do
        mono $MONO_ARGS .nuget/NuGet.exe "$@"
        if [ $? -eq 0 ]; then
            is_good=true
            break
        fi
    done

    if [ "$is_good" != "true" ]; then
        echo NuGet failed
        exit 1
    fi
}

# Install the Roslyn toolset which is used to build the bootstrap compilers
install_roslyn_toolset()
{
    local package_name=$ROSLYN_TOOLSET_NAME
    local package_path="/tmp/$ROSLYN_TOOLSET_NAME.tar.bz2"

    local download_package=false
    if [ ! -f $package_path ]; then
        download_package=true
    fi

    if [ "$USE_CACHE" = "true" ]; then
        download_package=true
    fi

    if [ "$download_package" = "true" ]; then
        rm $package_path 2>/dev/null
        pushd /tmp
        curl -O https://dotnetci.blob.core.windows.net/roslyn/$package_name.tar.bz2
        popd
    fi
    
    mkdir -p "Binaries"
    pushd Binaries > /dev/null
    tar -jxf $package_path
    popd > /dev/null
}

build_bootstrap()
{
    install_roslyn_toolset
    local bootstrap_arg="/p:CscToolPath=$(pwd)/Binaries/$ROSLYN_TOOLSET_NAME /p:CscToolExe=csc \
/p:VbcToolPath=$(pwd)/Binaries/$ROSLYN_TOOLSET_NAME /p:VbcToolExe=vbc"

    # Build the bootstrap compilers 
    echo Compiling the toolset compilers
    echo -e "  Compiling the C# compiler"
    run_msbuild /nologo $bootstrap_arg src/Compilers/CSharp/CscCore/CscCore.csproj /fileloggerparameters:LogFile=Binaries/Bootstrap_CscCore.log
    echo -e "  Compiling the VB compiler"
    run_msbuild /nologo $bootstrap_arg src/Compilers/VisualBasic/VbcCore/VbcCore.csproj /fileloggerparameters:LogFile=Binaries/Bootstrap_VbcCore.log

    # Save the compilers into the bootstrap directory
    local bootstrap_path="Binaries/Bootstrap"
    mkdir -p $bootstrap_path
    cp Binaries/$BUILD_CONFIGURATION/csccore/* $bootstrap_path
    cp Binaries/$BUILD_CONFIGURATION/vbccore/* $bootstrap_path

    # Clean out the built files so they will be re-built using the 
    # bootstrap compiler
    echo Cleaning the enlistment
    rm -rf Binaries/$BUILD_CONFIGURATION
    rm -rf Binaries/Obj
}

build_roslyn()
{    
    local bootstrap_arg="/p:CscToolPath=$(pwd)/Binaries/Bootstrap /p:CscToolExe=csc \
/p:VbcToolPath=$(pwd)/Binaries/Bootstrap /p:VbcToolExe=vbc"

    echo Building CrossPlatform.sln
    run_msbuild /nologo $bootstrap_arg CrossPlatform.sln /fileloggerparameters:LogFile=Binaries/Build.log
}

# Install the specified Mono toolset from our Azure blob storage.
install_mono_toolset()
{
    local target=/tmp/$1
    echo "Installing Mono toolset $1"

    if [ -d $target ]; then
        if [ "$USE_CACHE" = "true" ]; then
            echo "Mono already installed"
            return
        fi
    fi

    pushd /tmp

    rm -r $target 2>/dev/null
    rm $1.tar.bz2 2>/dev/null
    curl -O https://dotnetci.blob.core.windows.net/roslyn/$1.tar.bz2
    tar -jxf $1.tar.bz2
    if [ $? -ne 0 ]; then
        echo "Unable to download toolset"
        exit 1
    fi

    popd
}

# This function will update the PATH variable to put the desired
# version of Mono ahead of the system one. 
set_mono_path()
{
    if [ "$CUSTOM_MONO_PATH" != "" ]; then
        if [ ! -d "$CUSTOM_MONO_PATH" ]; then
            echo "Not a valid directory $CUSTOM_MONO_PATH"
            exit 1
        fi
  
        echo "Using mono path $CUSTOM_MONO_PATH"
        PATH=$CUSTOM_MONO_PATH:$PATH
        return
    fi


    install_mono_toolset $MONO_TOOLSET_NAME
    PATH=/tmp/$MONO_TOOLSET_NAME/bin:$PATH
}

check_mono()
{
    local mono_path=$(which mono)
    echo "Mono path $mono_path"
}

test_roslyn()
{
    local xunit_runner=~/.nuget/packages/xunit.runner.console/$XUNIT_VERSION/tools/xunit.console.x86.exe
    local test_binaries=(
        Roslyn.Compilers.CSharp.CommandLine.UnitTests
        Roslyn.Compilers.CSharp.Syntax.UnitTests
        Roslyn.Compilers.CSharp.Semantic.UnitTests
        Roslyn.Compilers.CSharp.Symbol.UnitTests
        Roslyn.Compilers.VisualBasic.Syntax.UnitTests)
    local any_failed=false

    # Need to copy over the execution dependencies.  This isn't being done correctly
    # by msbuild at the moment. 
    cp ~/.nuget/packages/xunit.extensibility.execution/$XUNIT_VERSION/lib/net45/xunit.execution.desktop.* Binaries/$BUILD_CONFIGURATION

    for i in "${test_binaries[@]}"
    do
        mkdir -p Binaries/$BUILD_CONFIGURATION/xUnitResults/
        mono $MONO_ARGS $xunit_runner Binaries/$BUILD_CONFIGURATION/$i.dll -xml Binaries/$BUILD_CONFIGURATION/xUnitResults/$i.dll.xml -noshadow
        if [ $? -ne 0 ]; then
            any_failed=true
        fi
    done

    if [ "$any_failed" = "true" ]; then
        echo Unit test failed
        exit 1
    fi
}

if [ "$CLEAN_RUN" == "true" ]; then
    echo Clean out the enlistment
    git clean -dxf . 
fi

# TODO: flow the mono path through to the tools used as a part of the build 
# TODO: flow the build configuration through 

set_build_info
restore_nuget
set_mono_path
check_mono
build_bootstrap
build_roslyn
test_roslyn

