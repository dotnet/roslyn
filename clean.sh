#!/usr/bin/env bash
set -euo pipefail

RoslynDir="$(dirname $0)"

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

if [ $DeleteOutput -eq "true" ]
then
    rm -rf "$RoslynDir/Binaries/"
fi

if [ $DeleteLocalPackage -eq "true" ]
then
    rm -rf "$RoslynDir/packages/"
fi

if [ $DeleteUserCache -eq "true" ]
then
    make --makefile "$RoslynDir/Makefile" nuget
    eval "$RoslynDir/nuget.exe" locals all -clear
fi
