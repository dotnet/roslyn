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

# Setup apt-get for grabbing mono snapshot builds
apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://jenkins.mono-project.com/repo/debian sid main" | tee /etc/apt/sources.list.d/mono-jenkins.list
apt-get update > /dev/null

# Now that we are all setup get the latest snapshot up and running
source setup-snapshot.sh

