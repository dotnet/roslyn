#!/bin/bash

echo Changing mono snapshot
. mono-snapshot mono/20150316155603
if [ $? -ne 0 ]; then
    echo Could not set mono snapshot 
    exit 1
fi

echo Restoring NuGet packages
mono src/.nuget/NuGet.exe restore src/Roslyn.sln
if [ $? -ne 0 ]; then
    echo Failed restoring NuGet packages
    exit 1
fi

cd src/Compilers/CSharp/csc

echo Compiling
xbuild /p:SignAssembly=false /p:DebugSymbols=false /p:DefineConstants=COMPILERCORE,DEBUG csc.csproj
