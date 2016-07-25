#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
$__scriptpath/build/run/init-tools.sh
if [ $? -ne 0 ]; then
    exit 1
fi

__toolRuntime=$__scriptpath/Binaries/Tools
__dotnet=$__toolRuntime/dotnetcli/dotnet

echo Running: \n$__dotnet $__toolRuntime/run.exe "$__scriptpath/build/run/config.json" $*

                $__dotnet $__toolRuntime/run.exe "$__scriptpath/build/run/config.json" $*

exit $?
