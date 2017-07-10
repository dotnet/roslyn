#!/usr/bin/env bash
set -e

usage()
{
    echo "Runs our integration suite on Linux"
    echo "usage: cibuild.sh [options]"
    echo ""
    echo "Options"
    echo "  --debug               Build Debug (default)"
    echo "  --release             Build Release"
    echo "  --skiptest            Do not run tests"
    echo "  --skipcrossgen        Do not crossgen the bootstrapped compiler"
    echo "  --skipcommitprinting  Do not print commit information"
    echo "  --nocache       Force download of toolsets"
}

BUILD_CONFIGURATION=Debug
USE_CACHE=true
SKIP_TESTS=false
SKIP_CROSSGEN=false
SKIP_COMMIT_PRINTING=false

MAKE="make"
if [[ $OSTYPE == *[Bb][Ss][Dd]* ]]; then
    MAKE="gmake"
fi

# LTTNG is the logging infrastructure used by coreclr.  Need this variable set 
# so it doesn't output warnings to the console.
export LTTNG_HOME=$HOME

# There's no reason to send telemetry or prime a local package cach when building
# in CI.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

while [[ $# > 0 ]]
do
    opt="$(echo $1 | awk '{print tolower($0)}')"
    case $opt in
        -h|--help)
        usage
        exit 1
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
        --skiptests)
        SKIP_TESTS=true
        shift 1
        ;;
        --skipcrossgen)
        SKIP_CROSSGEN=true
        shift 1
        ;;
        --skipcommitprinting)
        SKIP_COMMIT_PRINTING=true
        shift 1
        ;;
        *)
        usage 
        exit 1
        ;;
    esac
done

MAKE_ARGS="BUILD_CONFIGURATION=$BUILD_CONFIGURATION SKIP_CROSSGEN=$SKIP_CROSSGEN"

if [ "$CLEAN_RUN" == "true" ]; then
    echo Clean out the enlistment
    git clean -dxf . 
fi

if [ "$SKIP_COMMIT_PRINTING" == "false" ]; then
    echo Building this commit:
    git show --no-patch --pretty=raw HEAD
fi

echo Building Bootstrap
$MAKE bootstrap $MAKE_ARGS 

echo Building CrossPlatform.sln
$MAKE all $MAKE_ARGS BOOTSTRAP=true BUILD_LOG_PATH=Binaries/Build.log

if [ "$SKIP_TESTS" == "false" ]; then
    $MAKE test $MAKE_ARGS
fi

