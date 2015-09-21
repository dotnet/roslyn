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

XUNIT_VERSION=2.0.0-alpha-build2576
BUILD_CONFIGURATION=Debug
OS_NAME=$(uname -s)
USE_CACHE=true
MONO_ARGS='--debug=mdb-optimizations --attach=disable'

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

acquire_sem_or_wait()
{
    local lockpath="/tmp/${1}.lock.d"
    echo "Acquiring ${lockpath}"
    while true; do
        mkdir "${lockpath}" 2>/dev/null
        if [ $? -eq 0 ]; then
            break;
        fi
        echo "Waiting for lock $1"
        sleep 10
    done
}

release_sem()
{
    rmdir "/tmp/${1}.lock.d"
}

restore_nuget()
{
    acquire_sem_or_wait "restore_nuget"

    local package_name="nuget.11.zip"
    local target="/tmp/$package_name"
    echo "Installing NuGet Packages $target"
    if [ -f $target ]; then
        if [ "$USE_CACHE" = "true" ]; then
            echo "Already installed"
            release_sem "restore_nuget"
            return
        fi
    fi

    pushd /tmp/

    rm $package_name 2>/dev/null
    curl -O https://dotnetci.blob.core.windows.net/roslyn/$package_name
    unzip -uoq $package_name -d ~/
    if [ $? -ne 0 ]; then
        echo "Unable to download NuGet packages"
        release_sem "restore_nuget"
        exit 1
    fi

    popd

    release_sem "restore_nuget"
}

run_msbuild()
{
    local is_good=false
    
    for i in `seq 1 $RETRY_COUNT`
    do
        mono $MONO_ARGS ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0-prerelease/lib/MSBuild.exe /v:m /p:SignAssembly=false /p:DebugSymbols=false "$@"
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

# Run the compilation.  Can pass additional build arguments as parameters
compile_toolset()
{
    echo Compiling the toolset compilers
    echo -e "Compiling the C# compiler"
    run_msbuild src/Compilers/CSharp/CscCore/CscCore.csproj /p:Configuration=$BUILD_CONFIGURATION
    echo -e "Compiling the VB compiler"
    run_msbuild src/Compilers/VisualBasic/VbcCore/VbcCore.csproj /p:Configuration=$BUILD_CONFIGURATION
}

# Save the toolset binaries from Binaries/BUILD_CONFIGURATION to Binaries/Bootstrap
save_toolset()
{
    mkdir Binaries/Bootstrap
    cp Binaries/$BUILD_CONFIGURATION/core-clr/* Binaries/Bootstrap

    if [ "$OS_NAME" == "Linux" ]; then
      # Copy over the CoreCLR runtime
      ./build/linux/copy-coreclr-runtime.sh Binaries/Bootstrap
      if [ $? -ne 0 ]; then
        echo Saving bootstrap binaries failed
        exit 1
      fi
      chmod +x Binaries/Bootstrap/csc
      chmod +x Binaries/Bootstrap/vbc
    fi 
}

# Clean out all existing binaries.  This ensures the bootstrap phase forces
# a rebuild instead of picking up older binaries.
clean_roslyn()
{
    echo Cleaning the enlistment
    mono $MONO_ARGS ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0-prerelease/lib/MSBuild.exe /v:m /t:Clean build/Toolset.sln /p:Configuration=$BUILD_CONFIGURATION
    rm -rf Binaries/$BUILD_CONFIGURATION
}

build_roslyn()
{    
    local bootstrapArg=""

    if [ "$OS_NAME" == "Linux" ]; then
      bootstrapArg="/p:CscToolPath=$(pwd)/Binaries/Bootstrap /p:CscToolExe=csc \
/p:VbcToolPath=$(pwd)/Binaries/Bootstrap /p:VbcToolExe=vbc"
    fi

    echo Building CrossPlatform.sln
    run_msbuild $bootstrapArg CrossPlatform.sln /p:Configuration=$BUILD_CONFIGURATION
}

# Install the specified Mono toolset from our Azure blob storage.
install_mono_toolset()
{
    local target=/tmp/$1
    echo "Installing Mono toolset $1"

    acquire_sem_or_wait "$1"

    if [ -d $target ]; then
        if [ "$USE_CACHE" = "true" ]; then
            echo "Already installed"
            release_sem "$1"
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
        release_sem "$1"
        exit 1
    fi

    popd
    release_sem "$1"
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

    if [ "$OS_NAME" = "Darwin" ]; then
        MONO_TOOLSET_NAME=mono.mac.3
    elif [ "$OS_NAME" = "Linux" ]; then
        MONO_TOOLSET_NAME=mono.linux.3
    else
        echo "Error: Unsupported OS $OS_NAME"
        exit 1
    fi

    install_mono_toolset $MONO_TOOLSET_NAME
    PATH=/tmp/$MONO_TOOLSET_NAME/bin:$PATH
}

test_roslyn()
{
    local xunit_runner=~/.nuget/packages/xunit.runners/$XUNIT_VERSION/tools/xunit.console.x86.exe
    local test_binaries=(
        Roslyn.Compilers.CSharp.CommandLine.UnitTests
        Roslyn.Compilers.CSharp.Syntax.UnitTests
        Roslyn.Compilers.CSharp.Semantic.UnitTests
        Roslyn.Compilers.CSharp.Symbol.UnitTests
        Roslyn.Compilers.VisualBasic.Syntax.UnitTests)
    local any_failed=false

    for i in "${test_binaries[@]}"
    do
        mono $MONO_ARGS $xunit_runner Binaries/$BUILD_CONFIGURATION/$i.dll -xml Binaries/$BUILD_CONFIGURATION/$i.TestResults.xml -noshadow
        if [ $? -ne 0 ]; then
            any_failed=true
        fi
    done

    if [ "$any_failed" = "true" ]; then
        echo Unit test failed
        exit 1
    fi
}

echo Clean out the enlistment
git clean -dxf . 

restore_nuget
set_mono_path
which mono
compile_toolset
save_toolset
clean_roslyn
build_roslyn
test_roslyn

