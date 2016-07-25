#!/usr/bin/env bash
set -euo pipefail

RoslynDir="$(dirname $0)/../../"

if [[ -z "$RoslynDir" ]]
then
    echo "could not find roslyn directory"
fi

usage()
{
    echo "clean"
    echo "Options:"
    echo "    -b  Deletes the binary output directory"
    echo "    -p  Deletes the local package directory"
    echo "    -c  Deletes the user package cache"
    echo "    -all  Performs all of the above"
}

DeleteOutput="false"
DeleteLocalPackage="false"
DeleteUserCache="false"

for var in "$@"
do
    case "$var" in
        -b)
            DeleteOutput="true"
            ;;
        -p)
            DeleteLocalPackage="true"
            ;;
        -c)
            DeleteUserCache="true"
            ;;
         -all)
            DeleteOutput="true"
            DeleteLocalPackage="true"
            DeleteUserCache="true"
            ;;
         -?)
            usage
            exit 0
            ;;
    esac
done

if [[ $DeleteOutput = true ]]
then
    echo "removing Binaries/Release and Binaries/Debug directories"
    rm -rf "$RoslynDir/Binaries/Debug" || true
    rm -rf "$RoslynDir/Binaries/Release" || true
fi

if [[ $DeleteLocalPackage = true ]]
then
    echo "removing local packages"
    rm -rf "$RoslynDir/packages/" || true
fi

if [[ $DeleteUserCache = true ]]
then
    echo "removing user package cache"
    rm -rf ~/.local/share/NuGet/Cache/ || true
    rm -rf ~/.nuget/packages || true
fi
