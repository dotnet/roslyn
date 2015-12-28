#!/usr/bin/env bash

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

run_make()
{
    local is_good=false
    
    for i in `seq 1 $RETRY_COUNT`
    do
        make "$@" BUILD_CONFIGURATION=$BUILD_CONFIGURATION
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

if [ "$CLEAN_RUN" == "true" ]; then
    echo Clean out the enlistment
    git clean -dxf . 
fi

if [ "$USE_CACHE" == "false" ]; then
    echo Clean out the toolsets
    make clean_toolset
fi

echo Building Bootstrap
run_make bootstrap

echo Building CrossPlatform.sln
run_make all BOOTSTRAP=true BUILD_LOG_PATH=Binaries/Build.log

make test

