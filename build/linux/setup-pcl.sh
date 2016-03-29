#!/usr/bin/env bash

usage()
{
    echo "Install PCL reference assemblies in a mono drop"
	echo "setup-pcl.sh <path-to-mono-install>" 
}

XBUILD_FRAMEWORKS=$1/lib/mono/xbuild-frameworks
if [ ! -d "$XBUILD_FRAMEWORKS" ]; then
	echo "$XBUILD_FRAMEWORKS does not exist"
	usage
	exit 1
fi

PCL_NAME=PortableReferenceAssemblies-2014-04-14
PCL_TARGET=$XBUILD_FRAMEWORKS/.NETPortable

# Now to install the PCL on the snapshot 
pushd /tmp 
wget http://storage.bos.xamarin.com/bot-provisioning/$PCL_NAME.zip -O /tmp/pcl.zip
unzip pcl.zip 
popd

if [ ! -d "/tmp/$PCL_NAME" ]; then
    echo "Error: Did not unzip the PCL correctly"
    exit 1
fi

echo "Installing to $PCL_TARGET"
mkdir $PCL_TARGET
cp -r /tmp/$PCL_NAME/* $PCL_TARGET

rm -rf /tmp/$PCL_NAME
rm /tmp/pcl.zip

