#!/bin/bash
export HOME=/home/ankitakherauser
export PATH=/home/ankitakherauser/.dotnet:/usr/bin:/bin:$PATH
export DOTNET_ROOT=/home/ankitakherauser/.dotnet
cd /mnt/c/roslyn
dotnet test src/Compilers/CSharp/Test/Emit2/Microsoft.CodeAnalysis.CSharp.Emit2.UnitTests.csproj -c Debug -f net10.0 --no-build --nologo 2>&1 | grep 'Failed ' | sed 's/\[.*//;s/^.*Failed //' | rev | cut -d. -f2 | rev | sort | uniq -c | sort -rn | head -n 10
