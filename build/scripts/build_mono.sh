#!/usr/bin/env bash

mkdir -p ../../Binaries/mono_build
cd ../../Binaries/mono_build

sudo apt-get install git autoconf libtool automake build-essential mono-devel gettext cmake

PREFIX=$PWD/mono
VERSION=5.8.0.88
curl https://download.mono-project.com/sources/mono/mono-${VERSION}.tar.bz2 | tar xj
pushd mono-$VERSION
./configure --prefix=$PREFIX
make
make install
popd
tar czf mono-${VERSION}.tar.gz mono
