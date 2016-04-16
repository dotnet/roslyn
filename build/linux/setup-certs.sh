#!/bin/bash

if [ "$EUID" -ne 0 ]; then
    echo "This script must be run as root"
    exit 1
fi

# Install the certs so NuGet.exe can function correctly
mozroots --import --machine --sync
echo "Y" | certmgr -ssl https://go.microsoft.com
echo "Y" | certmgr -ssl https://nugetgallery.blob.core.windows.net
echo "Y" | certmgr -ssl https://nuget.org
