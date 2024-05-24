Compiler Toolset NuGet Packages
====

The compiler produces a number of Toolset NuGet packages that allow developers to override the Roslyn compiler that comes with MSBuild or .NET SDK. Installing these packages effectively tells the underlying MSBuild engine to use the provided version of the Roslyn build tasks vs. the installed ones.

The underlying mechanism used is the same between all of the packages here but they differ in scenarios. 

- Microsoft.Net.Compilers.Toolset: this overrides the Roslyn compiler for both MSBuild and .NET SDK installations. This means it distributes both .NET Core and .NET Framework versions of our build tasks and compilers. It's meant as a way to hot patching bugs on customer machines and part of our mechanism for distributing the compiler in the .NET product build. It's not meant for or supported in general scenarios.
- Microsoft.Net.Compilers.Toolset.Arm64: this package is functionally the same as Microsoft.Net.Compilers.Toolset except the .NET Framework binaries are targeted for ARM64 architectures. This provides the most efficiency for building on ARM64 machines environments. 
- Microsoft.Net.Compilers.Toolset.Framework: this overrides the Roslyn compiler for MSBuild installations. This means it only distributes the .NET Framework versions of our build tasks and compilers which makes it significantly smaller than Microsoft.Net.Compilers.Toolset. This is used by the .NET SDK internally to ensure that using `msbuild` on a .NET SDK based project uses the version of Roslyn that shipped with the .NET SDK, not the one that happens to be installed for MSBuild. Users enable this by setting the property `BuildWithNetFrameworkHostedCompiler` to `true`

⚠️Warning⚠️ ️

Using these packages directly is **not a supported** operation. These packages are meant as a mechanism for hot patching toolset bugs, distributing compiler bits for our internal build purposes or as an implementation detail of .NET SDK features.