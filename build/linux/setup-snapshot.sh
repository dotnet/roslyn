#!/bin/bash

if [ "$EUID" -ne 0 ]; then
    echo "Error: This script must be run as root"
    exit 1
fi

apt-get -y install mono-snapshot-latest

. mono-snapshot mono 
if [ ! -d "$MONO_PREFIX" ]; then
    echo "Error: Mono snapshot did not load correctly"
    exit 1
fi

# Now install the PCL assemblies on the snapshot
source setup-pcl $MONO_PREFIX
