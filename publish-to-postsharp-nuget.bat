@echo off

echo Publishing Caravela compiler version %CC_VERSION%

cd artifacts\packages\Release\Shipping

dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Caravela.Compiler.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Caravela.Compiler.Sdk.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.Common.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.CSharp.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.CSharp.Features.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.CSharp.Workspaces.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.Features.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.VisualBasic.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.VisualBasic.Features.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.VisualBasic.Workspaces.%CC_VERSION%.nupkg 
dotnet nuget push -s https://nuget.postsharp.net/nuget/caravela/ -k %NUGET_KEY% Microsoft.CodeAnalysis.Workspaces.Common.%CC_VERSION%.nupkg 

cd ..\..\..\..