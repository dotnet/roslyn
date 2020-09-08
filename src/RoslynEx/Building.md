To start working with RoslynEx, first run `restore && build` from the root of the repo.

You can then open Compilers.sln in VS 2019 version 16.8.0 Preview or newer. (Roslyn.sln will work too, but is unnecessarily big for most tasks.)

To build the RoslynEx packages, you can either run `build -pack`, or you can use the Pack command in VS on the Microsoft.Net.Compilers.Toolset.Package project (which builds RoslynEx.Toolset) and the RoslynEx.Sdk project. The built packages are in artifacts\packages\\$Configuration\Shipping.

To build a release version of RoslynEx:

1. Set the version in eng\Versions.props.
2. Also set the same version in `<RoslynExToolsetVersion>` in build\RoslynEx.targets in RoslynEx.Sdk.
3. Run `build -c Release -pack /p:DotNetFinalVersionKind=release`. Without the last parameter, any pack produces a prerelease version, e.g. 3.8.0-dev.