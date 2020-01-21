#!/bin/sh

artifact_path='producedbuilds/*'

chmod -v +x Artifacts/Builds/Binaries/Linux/csc
ls -l Artifacts/Builds/Binaries/Linux/csc
chmod -v +x Artifacts/Builds/Binaries/Linux/VBCSCompiler
ls -l Artifacts/Builds/Binaries/Linux/VBCSCompiler

chmod -v +x Artifacts/Builds/Binaries/Mac/csc
ls -l Artifacts/Builds/Binaries/Mac/csc
chmod -v +x Artifacts/Builds/Binaries/Mac/VBCSCompiler
ls -l Artifacts/Builds/Binaries/Mac/VBCSCompiler

cp License.txt $PWD/Artifacts/Builds/Binaries/Linux/LICENSE.txt
cp License.txt $PWD/Artifacts/Builds/Binaries/Mac/LICENSE.txt
cp License.txt $PWD/Artifacts/Builds/Binaries/Windows/LICENSE.txt
cp License.txt $PWD/Artifacts/Builds/Binaries/Net46/LICENSE.txt

mkdir -p producedbuilds

7z a producedbuilds/roslyn-csc-linux.7z $PWD/Artifacts/Builds/Binaries/Linux/*
7z a producedbuilds/roslyn-csc-mac.7z $PWD/Artifacts/Builds/Binaries/Mac/*
7z a producedbuilds/roslyn-csc-win64.7z $PWD/Artifacts/Builds/Binaries/Windows/*
7z a producedbuilds/roslyn-csc-net46.7z $PWD/Artifacts/Builds/Binaries/Net46/*
