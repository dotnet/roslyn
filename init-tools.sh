#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__init_tools_log=$__scriptpath/init-tools.log
__PACKAGES_DIR=$__scriptpath/packages
__TOOLRUNTIME_DIR=$__scriptpath/Tools
__DOTNET_PATH=$__TOOLRUNTIME_DIR/dotnetcli
__DOTNET_CMD=$__DOTNET_PATH/dotnet
if [ -z "$__BUILDTOOLS_SOURCE" ]; then __BUILDTOOLS_SOURCE=https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json; fi
__BUILD_TOOLS_PACKAGE_VERSION=$(cat $__scriptpath/BuildToolsVersion.txt)
__DOTNET_TOOLS_VERSION=$(cat $__scriptpath/DotnetCLIVersion.txt)
__BUILD_TOOLS_PATH=$__PACKAGES_DIR/Microsoft.DotNet.BuildTools/$__BUILD_TOOLS_PACKAGE_VERSION/lib
__PROJECT_JSON_PATH=$__TOOLRUNTIME_DIR/$__BUILD_TOOLS_PACKAGE_VERSION
__PROJECT_JSON_FILE=$__PROJECT_JSON_PATH/project.json
__PROJECT_JSON_CONTENTS="{ \"dependencies\": { \"Microsoft.DotNet.BuildTools\": \"$__BUILD_TOOLS_PACKAGE_VERSION\" }, \"frameworks\": { \"netcoreapp1.0\": { } } }"
__INIT_TOOLS_DONE_MARKER=$__PROJECT_JSON_PATH/done

if [ -z "$__DOTNET_PKG" ]; then
OSName=$(uname -s)
    case $OSName in
        Darwin)
            OS=OSX
            __DOTNET_PKG=dotnet-dev-osx-x64
            ulimit -n 2048
            ;;

        Linux)
            OS=Linux
            if [ ! -e /etc/os-release ]; then
                echo "Cannot determine Linux distribution, asuming Ubuntu 14.04."
                __DOTNET_PKG=dotnet-dev-ubuntu.14.04-x64
            else
                source /etc/os-release
                if [[ "$ID" == "ubuntu" && "$VERSION_ID" != "14.04" && "$VERSION_ID" != "16.04" ]]; then
                    echo "Unsupported Ubuntu version, falling back to Ubuntu 14.04."
                    __DOTNET_PKG=dotnet-dev-ubuntu.14.04-x64
                else
                    __DOTNET_PKG="dotnet-dev-$ID.$VERSION_ID-x64"
                fi
            fi
            ;;

        *)
            echo "Unsupported OS '$OSName' detected. Downloading ubuntu-x64 tools."
            OS=Linux
            __DOTNET_PKG=dotnet-dev-ubuntu-x64
            ;;
  esac
fi

if [ ! -e $__INIT_TOOLS_DONE_MARKER ]; then
    if [ -e $__TOOLRUNTIME_DIR ]; then rm -rf -- $__TOOLRUNTIME_DIR; fi
    echo "Running: $__scriptpath/init-tools.sh" > $__init_tools_log
    if [ ! -e $__DOTNET_PATH ]; then
        echo "Installing dotnet cli..."
        __DOTNET_LOCATION="https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/${__DOTNET_TOOLS_VERSION}/${__DOTNET_PKG}.${__DOTNET_TOOLS_VERSION}.tar.gz"
        # curl has HTTPS CA trust-issues less often than wget, so lets try that first.
        echo "Installing '${__DOTNET_LOCATION}' to '$__DOTNET_PATH/dotnet.tar'" >> $__init_tools_log
        which curl > /dev/null 2> /dev/null
        if [ $? -ne 0 ]; then
            mkdir -p "$__DOTNET_PATH"
            wget -q -O $__DOTNET_PATH/dotnet.tar ${__DOTNET_LOCATION}
        else
            curl --retry 10 -sSL --create-dirs -o $__DOTNET_PATH/dotnet.tar ${__DOTNET_LOCATION}
        fi
        cd $__DOTNET_PATH
        tar -xf $__DOTNET_PATH/dotnet.tar

        cd $__scriptpath
    fi

    if [ ! -d "$__PROJECT_JSON_PATH" ]; then mkdir "$__PROJECT_JSON_PATH"; fi
    echo $__PROJECT_JSON_CONTENTS > "$__PROJECT_JSON_FILE"

    if [ ! -e $__BUILD_TOOLS_PATH ]; then
        echo "Restoring BuildTools version $__BUILD_TOOLS_PACKAGE_VERSION..."
        echo "Running: $__DOTNET_CMD restore \"$__PROJECT_JSON_FILE\" --no-cache --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE" >> $__init_tools_log
        $__DOTNET_CMD restore "$__PROJECT_JSON_FILE" --no-cache --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE >> $__init_tools_log
        if [ ! -e "$__BUILD_TOOLS_PATH/init-tools.sh" ]; then echo "ERROR: Could not restore build tools correctly. See '$__init_tools_log' for more details."; fi
    fi

    echo "Initializing BuildTools..."
    echo "Running: $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR" >> $__init_tools_log
    $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR >> $__init_tools_log
    if [ "$?" != "0" ]; then
        echo "ERROR: An error occured when trying to initialize the tools. Please check '$__init_tools_log' for more details."
        exit 1
    fi
    touch $__INIT_TOOLS_DONE_MARKER
    echo "Done initializing tools."
else
    echo "Tools are already initialized"
fi
