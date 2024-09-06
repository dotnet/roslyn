# Merging from new Roslyn branches

## 1. Find the source Roslyn version and branch

Check the versions of Microsoft.Net.Compilers.Toolset NuGet package. In the descrption of each version, you can find the commit from which the version was built. The commit then corresponds to a certain branch.

Alternatively, you can find the branch at https://github.com/dotnet/roslyn/releases. Several releases may share the same branch and version there.

When merging before the package has been published, you can find the commit in the product version of e.g. C:\Program Files\dotnet\sdk\\\<version>\Roslyn\bincore\Microsoft.CodeAnalysis.dll.

Examples:
version 3.8.0, commit https://github.com/dotnet/roslyn/commit/8de9e4b2beba5b7c0edd6f1e6a4f192a51fdc872, branch release/dev16.8-vs-deps
version 3.11.0, commit https://github.com/dotnet/roslyn/commit/ae1fff344d46976624e68ae17164e0607ab68b10, branch release/dev16.11-vs-deps

## 2. Merge the selected Roslyn branch to Metalama.Compiler repo

See Modifications.md to better understand the changes done for Metalama.

## 3. Enable original NuGet sources

Uncomment the original NuGet sources in NuGet.config file.
Do this now, because in the next steps, all the new packages will get restored on your machine,
so you'll be able to backup them.

## 4. Update eng\Versions.props

Set RoslynVersion to the source Roslyn version.

## 5. Regenerate generated source files

See Modifications.md for details.

## 6. Make sure all test are green

To run Metalama.Compiler tests, execute `b test`.
To run all Roslyn tests, execute `b test -p TestAll`.

## 7. Backup new NuGet packages

- If the project has not been built, or the repo got cleaned, execute `b build`.
- Execute `b push-nuget-dependencies`.

If authentication fails when pushing, copy one of the failing `nuget push` commands, and execute it wih an `--interactive` flag. Then execute the NuGet dependencies push again.

This step only works correctly, when all packages have been restored in the working copy of the repo, and the repo has not been cleaned afterwards.

## 8. Disable original NuGet sources

Comment out the original NuGet sources in NuGet.config again.
Never push the NuGet.config file with the original sources uncommented, so we know that a package is missing in the backup feed early enough.

## 9. Update Metalama Framework

See docs\updating-roslyn.md in the Metalama repo.

## 10. Review

- Use gitk command.
- Show the changes done in the merge commit.
- Tick the "ignore space change".
- Pay attention to changes marked with "++" - these are the changes that have been done manually, not coming from either of the merged branches.