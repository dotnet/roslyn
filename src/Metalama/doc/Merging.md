# Merging from new Roslyn branches

## 1. Find the source Roslyn version and branch

Check the versions of Microsoft.Net.Compilers.Toolset NuGet package. In the descrption of each version, you can find the commit from which the version was built. The commit then corresponds to a certain branch.

Alternatively, you can find the branch at https://github.com/dotnet/roslyn/releases. Several releases may share the same branch and version there.

Examples:
version 3.8.0, commit https://github.com/dotnet/roslyn/commit/8de9e4b2beba5b7c0edd6f1e6a4f192a51fdc872, branch release/dev16.8-vs-deps
version 3.11.0, commit https://github.com/dotnet/roslyn/commit/ae1fff344d46976624e68ae17164e0607ab68b10, branch release/dev16.11-vs-deps

## 2. Merge the selected Roslyn branch to Metalama.Compiler repo

See Modifications.md to better understand the changes done for Metalama.

## 3. Update eng-Metalama\MainVersions.props

Set RoslynVersion to the source Roslyn version.

## 4. Regenerate generated source files

See Modifications.md for details.

## 5. Make sure all test are green

TODO: Update for the new PostSharp Engineering.
To run Roslyn tests, execute `eng\Build.ps1 -test`.
To run Metalama.Compiler tests, execute `dotnet test src\Metalama\Metalama.Compiler.UnitTests\Metalama.Compiler.UnitTests.csproj`.

## 6. Update Metalama Framework

See docs\updating-roslyn.md in the Metalama repo.