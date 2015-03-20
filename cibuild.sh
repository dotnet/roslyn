#!/bin/bash

# Run the compilation.  Can pass additional build arguments as parameters
docompile()
{
    xbuild /v:m /p:SignAssembly=false /p:DebugSymbols=false /p:DefineConstants=COMPILERCORE,DEBUG $1 src/Compilers/CSharp/csc/csc.csproj
}

echo Changing mono snapshot
. mono-snapshot mono/20150316155603
if [ $? -ne 0 ]; then
    echo Could not set mono snapshot 
    exit 1
fi

# NuGet on mono crashes about every 5th time we run it.  This is causing
# Linux runs to fail frequently enough that we need to employ a 
# temporary work around.  
echo Restoring NuGet packages
i=5
while [ $i -gt 0 ]; do
    mono src/.nuget/NuGet.exe restore src/Roslyn.sln
    if [ $? -eq 0 ]; then
        i=0
    fi
done
if [ $? -ne 0 ]; then
    echo NuGet Failed
    exit 1
fi

echo Compiling using toolset compiler 
docompile
if [ $? -ne 0 ]; then
    echo Compilation failed
    exit 1
fi

compiler_binaries=(
    csc.exe
    Microsoft.CodeAnalysis.dll
    Microsoft.CodeAnalysis.Desktop.dll
    Microsoft.CodeAnalysis.CSharp.dll
    Microsoft.CodeAnalysis.CSharp.Desktop.dll
    System.Collections.Immutable.dll
    System.Reflection.Metadata.dll)

mkdir Binaries/Bootstrap
for i in ${compiler_binaries[@]}; do
    cp Binaries/Debug/${i} Binaries/Bootstrap/${i}
    if [ $? -ne 0 ]; then
        echo Compilation failed
        exit 1
    fi
done
rm -rf Binaries/Debug

echo Compiling using bootstrap compiler 
docompile /p:BootstrapBuild="$(pwd)/Binaries/Bootstrap"
if [ $? -ne 0 ]; then
    echo Compilation failed
    exit 1
fi

