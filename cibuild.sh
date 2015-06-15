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
        *)
        usage 
        exit 1
        ;;
    esac
done

run_xbuild()
{
    xbuild /v:m /p:SignAssembly=false /p:DebugSymbols=false "$@"
    if [ $? -ne 0 ]; then
        echo Compilation failed
        exit 1
    fi
}

# NuGet crashes on occasion during restore.  This isn't a fatal action so 
# we re-run it a number of times.  
run_nuget()
{
    i=5
    while [ $i -gt 0 ]; do
        mono src/.nuget/NuGet.exe "$@"
        if [ $? -eq 0 ]; then
            i=0
        else
            i=$((i - 1))
        fi
    done

    if [ $? -ne 0 ]; then
        echo NuGet Failed
        exit 1
    fi
}

# Run the compilation.  Can pass additional build arguments as parameters
compile_toolset()
{
    echo Compiling the toolset compilers
    echo -e "\tCompiling the C# compiler"
    run_xbuild src/Compilers/CSharp/csc/csc.csproj /p:Configuration=$BUILD_CONFIGURATION
    run_xbuild src/Compilers/VisualBasic/vbc/vbc.csproj /p:Configuration=$BUILD_CONFIGURATION
}

# Save the toolset binaries from Binaries/BUILD_CONFIGURATION to Binaries/Bootstrap
save_toolset()
{
    local compiler_binaries=(
        csc.exe
        Microsoft.CodeAnalysis.dll
        Microsoft.CodeAnalysis.CSharp.dll
        System.Collections.Immutable.dll
        System.Reflection.Metadata.dll
        vbc.exe
        Microsoft.CodeAnalysis.VisualBasic.dll)

    mkdir Binaries/Bootstrap
    for i in ${compiler_binaries[@]}; do
        cp Binaries/$BUILD_CONFIGURATION/${i} Binaries/Bootstrap/${i}
        if [ $? -ne 0 ]; then
            echo Saving bootstrap binaries failed
            exit 1
        fi
    done
}

# Clean out all existing binaries.  This ensures the bootstrap phase forces
# a rebuild instead of picking up older binaries.
clean_roslyn()
{
    echo Cleaning the enlistment
    xbuild /v:m /t:Clean src/Toolset.sln /p:Configuration=$BUILD_CONFIGURATION
    rm -rf Binaries/$BUILD_CONFIGURATION
}

build_roslyn()
{
    BOOTSTRAP_ARG=/p:BootstrapBuildPath=$(pwd)/Binaries/Bootstrap

    echo Building CrossPlatform.sln
    run_xbuild $BOOTSTRAP_ARG src/CrossPlatform.sln /p:Configuration=$BUILD_CONFIGURATION
}

# Install the specified Mono toolset from our Azure blob storage.
install_mono_toolset()
{
    TARGET=/tmp/$1
    echo "Installing Mono toolset $1"
    if [ -d $TARGET ]; then
        echo "Already installed"
        return
    fi

    pushd /tmp

    rm $TARGET 2>/dev/null
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

    if [ "$OS_NAME" = "Darwin" ]; then
        MONO_TOOLSET_NAME=mono.mac.1
    elif [ "$OS_NAME" = "Linux" ]; then
        MONO_TOOLSET_NAME=mono.linux.1
    else
        echo "Error: Unsupported OS $OS_NAME"
        exit 1
    fi

    install_mono_toolset $MONO_TOOLSET_NAME
    PATH=/tmp/$MONO_TOOLSET_NAME/bin:$PATH
}

test_roslyn()
{
    local xunit_runner=packages/xunit.runners.$XUNIT_VERSION/tools/xunit.console.x86.exe
    local test_binaries=(
        Roslyn.Compilers.CSharp.CommandLine.UnitTests
        Roslyn.Compilers.CSharp.Syntax.UnitTests
        Roslyn.Compilers.CSharp.Semantic.UnitTests
        Roslyn.Compilers.CSharp.Symbol.UnitTests
        Roslyn.Compilers.VisualBasic.Syntax.UnitTests)
    local any_failed=false

    for i in "${test_binaries[@]}"
    do
        mono $xunit_runner Binaries/$BUILD_CONFIGURATION/$i.dll -xml Binaries/$BUILD_CONFIGURATION/$i.TestResults.xml -noshadow
        if [ $? -ne 0 ]; then
            any_failed=true
        fi
    done

    if [ "$any_failed" = "true" ]; then
        echo Unit test failed
        exit 1
    fi
}

# NuGet on mono crashes about every 5th time we run it.  This is causing
# Linux runs to fail frequently enough that we need to employ a 
# temporary work around.  
echo Restoring NuGet packages
run_nuget restore src/Roslyn.sln
run_nuget install xunit.runners -PreRelease -Version $XUNIT_VERSION -OutputDirectory packages

set_mono_path
which mono
compile_toolset
save_toolset
clean_roslyn
build_roslyn
test_roslyn

