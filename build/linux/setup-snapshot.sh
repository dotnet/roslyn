#!/usr/bin/env bash

if [ "$EUID" -ne 0 ]; then
    echo "Error: This script must be run as root"
    exit 1
fi

# Setup apt-get for grabbing mono snapshot builds
apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://jenkins.mono-project.com/repo/debian sid main" | tee /etc/apt/sources.list.d/mono-jenkins.list
apt-get update > /dev/null

apt-get -y install mono-snapshot-latest

. mono-snapshot mono 
if [ ! -d "$MONO_PREFIX" ]; then
    echo "Error: Mono snapshot did not load correctly"
    exit 1
fi

# Now install the PCL assemblies on the snapshot
source setup-pcl $MONO_PREFIX
