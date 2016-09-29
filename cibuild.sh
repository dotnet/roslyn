#!/usr/bin/env bash
set -e

usage()
{
    echo "Runs our integration suite on Linux"
    echo "usage: cibuild.sh [options]"
    echo ""
    echo "Options"
    echo "  --debug     Build Debug (default)"
    echo "  --release   Build Release"
    echo "  --nocache   Force download of toolsets"

}

BUILD_CONFIGURATION=Debug
USE_CACHE=true

MAKE="make"
if [[ $OSTYPE == *[Bb][Ss][Dd]* ]]; then
    MAKE="gmake"
fi

# LTTNG is the logging infrastructure used by coreclr.  Need this variable set 
# so it doesn't output warnings to the console.
export LTTNG_HOME=$HOME

while [[ $# > 0 ]]
do
    opt="$1"
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
        *)
        usage 
        exit 1
        ;;
    esac
done

MAKE_ARGS="BUILD_CONFIGURATION=$BUILD_CONFIGURATION"

if [ "$CLEAN_RUN" == "true" ]; then
    echo Clean out the enlistment
    git clean -dxf . 
fi

if [ "$USE_CACHE" == "false" ]; then
    echo Clean out the toolsets
    $MAKE clean_toolset
fi

echo Building this commit:
git show --no-patch --pretty=raw HEAD

echo Building Bootstrap
$MAKE bootstrap $MAKE_ARGS 

echo Building CrossPlatform.sln
$MAKE all $MAKE_ARGS BOOTSTRAP=true BUILD_LOG_PATH=Binaries/Build.log

$MAKE test

