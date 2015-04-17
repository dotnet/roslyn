#!/bin/bash

PCL_NAME=PortableReferenceAssemblies-2014-04-14

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

PCL_TARGET=$MONO_PREFIX/lib/mono/xbuild-frameworks/.NETPortable
if [ -d "$PCL_TARGET" ]; then
    echo "Error: PCL already installed at $PCL_TARGET"
    exit 1
fi

# Now to install the PCL on the snapshot 
cd /tmp 
wget http://storage.bos.xamarin.com/bot-provisioning/$PCL_NAME.zip -O /tmp/pcl.zip
unzip pcl.zip 
if [ ! -d "$PCL_NAME" ]; then
    echo "Error: Did not unzip the PCL correctly"
    exit 1
fi

echo "Installing to $PCL_TARGET"
mkdir $PCL_TARGET
mv $PCL_NAME/* $PCL_TARGET
