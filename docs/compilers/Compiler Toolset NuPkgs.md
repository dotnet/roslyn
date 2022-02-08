Compiler Toolset NuPkgs
===
## Summary
The compiler produces the [Microsoft.Net.Compilers.Toolset NuPkg](https://www.nuget.org/packages/Microsoft.Net.Compilers.Toolset)
from all of Roslyn's main branches. When this NuPkg is installed it will
override the compiler that comes with MSBuild with the version from the branch
it was built in.

This package is meant to support the following scenarios:
1. Allows compiler team to provide rapid hot fixes to customers who hit a blocking
issue. This package can be installed until the fix is available in .NET SDK or 
Visual Studio servicing.
1. Serves as a transport mechanism for the Roslyn binaries in the greater .NET
SDK build process.
1. Allows customers to conduct experiments on various Roslyn builds, many of
which aren't in an official shipping product yet.

This package is **not** meant to support using newer compiler versions in an
older version of MSBuild. For example using Microsoft.Net.Compilers.Toolset
3.5 (C# 8) inside MSBuild 15 is explicitly not a supported scenario.

Customers who want to use the compiler as a part of their supported build 
infrastructure should use the [Visual Studio Build Tools SKU](https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools?view=vs-2022])
) or [.NET SDK](https://dotnet.microsoft.com/download/visual-studio-sdks)

## NuPkg Installation

To install the NuPgk run the following:

```cmd
> nuget install Microsoft.Net.Compilers.Toolset   # Install C# and VB compilers
```

Daily NuGet builds of the project are also available in our [Azure DevOps feed](https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=dotnet-tools):

> https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json

## Microsoft.Net.Compilers

The [Microsoft.Net.Compilers](https://www.nuget.org/packages/Microsoft.Net.Compilers)
NuPkg is deprecated. It is a .NET Desktop specific version of
Microsoft.Net.Compilers.Toolset and will not be produced anymore after the 
3.6.0 release. The Microsoft.Net.Compilers.Toolset package is a drop in
replacement for it for all supported scenarios.
