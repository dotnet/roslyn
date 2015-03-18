#!/bin/bash

echo Changing mono snapshot
. mono-snapshot mono/20150316155603

echo Restoring NuGet packages
mono src/.nuget/NuGet.exe restore src/Roslyn.sln

cd src/Compilers/CSharp/csc

echo Compiling
xbuild /p:SignAssembly=false /p:DebugSymbols=false /p:DefineConstants=COMPILERCORE,DEBUG csc.csproj
