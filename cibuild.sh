#!/bin/bash

XUNIT_VERSION=2.0.0-alpha-build2576
FULL_RUN=true
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
        --minimal)
        FULL_RUN=false
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
    run_xbuild src/Compilers/CSharp/csc/csc.csproj

    if [ "$FULL_RUN" = "true" ]; then
        echo -e "\tCompiling VB compiler"
        run_xbuild src/Compilers/VisualBasic/vbc/vbc.csproj
    fi
}

# Save the toolset binaries from Binaries/Debug to Binaries/Bootstrap
save_toolset()
{
    compiler_binaries=(
        csc.exe
        Microsoft.CodeAnalysis.dll
        Microsoft.CodeAnalysis.Desktop.dll
        Microsoft.CodeAnalysis.CSharp.dll
        Microsoft.CodeAnalysis.CSharp.Desktop.dll
        System.Collections.Immutable.dll
        System.Reflection.Metadata.dll)

    if [ "$FULL_RUN" = "true" ]; then
        compiler_binaries=(
            ${compiler_binaries[@]} 
            vbc.exe
            Microsoft.CodeAnalysis.VisualBasic.dll
            Microsoft.CodeAnalysis.VisualBasic.Desktop.dll)
    fi

    mkdir Binaries/Bootstrap
    for i in ${compiler_binaries[@]}; do
        cp Binaries/Debug/${i} Binaries/Bootstrap/${i}
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
    xbuild /v:m /t:Clean src/Toolset.sln
    rm -rf Binaries/Debug
}

build_roslyn()
{
    BOOTSTRAP_ARG=/p:BootstrapBuildPath=$(pwd)/Binaries/Bootstrap

    echo Running the bootstrap build 
    echo -e "\tCompiling the C# compiler"
    run_xbuild $BOOTSTRAP_ARG src/Compilers/CSharp/csc/csc.csproj

    if [ "$FULL_RUN" = "true" ]; then
        echo -e "\tCompiling the VB compiler"
        run_xbuild $BOOTSTRAP_ARG src/Compilers/VisualBasic/vbc/vbc.csproj
        run_xbuild $BOOTSTRAP_ARG src/Compilers/CSharp/Test/Syntax/CSharpCompilerSyntaxTest.csproj
        run_xbuild $BOOTSTRAP_ARG src/Compilers/CSharp/Test/CommandLine/CSharpCommandLineTest.csproj
        run_xbuild $BOOTSTRAP_ARG src/Compilers/VisualBasic/Test/Syntax/BasicCompilerSyntaxTest.vbproj
    fi
}

test_roslyn()
{
    if [ "$FULL_RUN" != "true" ]; then
        return
    fi
    
    XUNIT_RUNNER=packages/xunit.runners.$XUNIT_VERSION/tools/xunit.console.x86.exe
    mono $XUNIT_RUNNER Binaries/Debug/Roslyn.Compilers.CSharp.Syntax.UnitTests.dll -noshadow
    mono $XUNIT_RUNNER Binaries/Debug/Roslyn.Compilers.CSharp.CommandLine.UnitTests.dll -noshadow
    mono $XUNIT_RUNNER Binaries/Debug/Roslyn.Compilers.VisualBasic.Syntax.UnitTests.dll -noshadow

    if [ $? -ne 0 ]; then
        echo Unit tests failed
        exit 1
    fi
}

usage()
{
    echo "Runs our integration suite on Linux"
    echo "usage: cibuild.sh [options]"
    echo ""
    echo "Options"
    echo "  --mono-path <path>  Path to the mono installation to use for the run" 
	echo "  --os <os>			OS to run (Linux / Darwin)"
	echo "  --minimal			Run a minimal set of suites (used when upgrading mono)"
}

# As a bootstrap mechanism in Jenkins we assume that Linux is a
# minimal run.  It is not yet updated to the latest mono build
# nor is the --minimal switch present for us to take advantage
# of.  This block will be deleted once everything gets pushed
# through. 
if [ "$OS_NAME" = "Linux" ]; then
    FULL_RUN=false
fi

if [ "$CUSTOM_MONO_PATH" != "" ]; then
    if [ ! -d "$CUSTOM_MONO_PATH" ]; then
        echo "Not a valid directory $CUSTOM_MONO_PATH"
        exit 1
    fi

    echo "Using mono path $CUSTOM_MONO_PATH"
    PATH=$CUSTOM_MONO_PATH:$PATH
else
    echo Changing mono snapshot
    . mono-snapshot mono/20150316155603
    if [ $? -ne 0 ]; then
        echo Could not set mono snapshot 
        exit 1
    fi
fi

# NuGet on mono crashes about every 5th time we run it.  This is causing
# Linux runs to fail frequently enough that we need to employ a 
# temporary work around.  
echo Restoring NuGet packages
run_nuget restore src/Roslyn.sln
run_nuget install xunit.runners -PreRelease -Version $XUNIT_VERSION -OutputDirectory packages

compile_toolset
save_toolset
clean_roslyn
build_roslyn
test_roslyn

