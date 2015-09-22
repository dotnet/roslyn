#!/bin/bash

CORECLR_DIR="$1"
RUNTIME_SOURCE_DIR=~/.nuget/packages/tmp_coreclr_runtime

if [ ! -d $CORECLR_DIR ]; then
    echo "usage: $0 core-clr-directory"
    exit 1
fi

if [ -f "$CORECLR_DIR/csc.exe" ]; then
    mv "$CORECLR_DIR/csc.exe" "$CORECLR_DIR/csc.dll"
fi

if [ ! -f "$CORECLR_DIR/csc" ]; then
    cp "$RUNTIME_SOURCE_DIR/coreconsole" $CORECLR_DIR/csc
fi

if [ -f "$CORECLR_DIR/vbc.exe" ]; then
    mv "$CORECLR_DIR/vbc.exe" "$CORECLR_DIR/vbc.dll"
fi

if [ ! -f "$CORECLR_DIR/vbc" ]; then
    cp "$RUNTIME_SOURCE_DIR/coreconsole" $CORECLR_DIR/vbc
fi

cp $RUNTIME_SOURCE_DIR/{*.dll,*.so} "$CORECLR_DIR"
