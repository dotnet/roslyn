// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace BuildBoss
{
    /// <summary>
    /// Verifies the contents of our toolset NuPkg and SWR files are correct.
    /// 
    /// The compiler toolset is a particularly difficult package to get correct. In essence it is 
    /// merging the output of three different exes into a single directory. That causes a number 
    /// of issues during pack time:
    /// 
    ///     - The dependencies are not necessarily equal between all exes
    ///     - The dependencies can change based on subtle changes to the code
    ///     - There is no project which is guaranteed to have a superset of dependencies 
    ///     - There is no syntax for using the union of DLLs in a NuSpec file
    ///
    /// The most straightforward solution that could be decided on was to manage the list of dependencies 
    /// by hand in the NuSpec file and then rigorously verify the solution here.
    /// </summary>
    internal sealed class PackageContentsChecker : ICheckerUtil
    {
        private sealed class PackagePartData
        {
            public PackagePart PackagePart { get; }
            public string Name { get; }
            public string RelativeName { get; }
            public string Checksum { get; }

            public PackagePartData(PackagePart part, string checksum)
            {
                Name = part.GetName();
                RelativeName = part.GetRelativeName();
                PackagePart = part;
                Checksum = checksum;
            }

            public override string ToString() => RelativeName;
        }

        internal static StringComparer PathComparer { get; } = StringComparer.OrdinalIgnoreCase;
        internal static StringComparison PathComparison { get; } = StringComparison.OrdinalIgnoreCase;

        internal string ArtifactsDirectory { get; }
        internal string Configuration { get; }
        internal string RepositoryDirectory { get; }

        internal PackageContentsChecker(string repositoryDirectory, string artifactsDirectory, string configuration)
        {
            RepositoryDirectory = repositoryDirectory;
            ArtifactsDirectory = artifactsDirectory;
            Configuration = configuration;
        }

        public bool Check(TextWriter textWriter)
        {
            try
            {
                var allGood = true;
                allGood &= CheckPublishData(textWriter);
                allGood &= CheckPackages(textWriter);
                allGood &= CheckExternalApis(textWriter);
                return allGood;
            }
            catch (Exception ex)
            {
                textWriter.WriteLine($"Error verifying: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verify PublishData.json contains feeds for all packages that will be published.
        /// </summary>
        private bool CheckPublishData(TextWriter textWriter)
        {
            var allGood = true;

            // Load PublishData.json
            var publishDataPath = Path.Combine(RepositoryDirectory, "eng", "config", "PublishData.json");
            var publishDataRoot = JObject.Parse(File.ReadAllText(publishDataPath));
            var publishDataPackages = publishDataRoot["packages"]["default"] as JObject;

            // Check all shipping packages have an entry in PublishData.json
            var regex = new Regex(@"^(.*?)\.\d.*\.nupkg$");
            var packagesDirectory = Path.Combine(ArtifactsDirectory, "packages", Configuration, "Shipping");
            foreach (var packageFullPath in Directory.EnumerateFiles(packagesDirectory, "*.nupkg"))
            {
                var packageFileName = Path.GetFileName(packageFullPath);
                var match = regex.Match(packageFileName);
                if (!match.Success)
                {
                    allGood = false;
                    textWriter.WriteLine($"Unexpected package file name '{packageFileName}'");
                }
                else
                {
                    var packageId = match.Groups[1].Value;
                    if (!publishDataPackages.ContainsKey(packageId))
                    {
                        allGood = false;
                        textWriter.WriteLine($"Package doesn't have corresponding PublishData.json entry: {packageId} ({packageFileName})");
                    }
                }
            }

            return allGood;
        }

        /// <summary>
        /// Verify the contents of the compiler packages match the expected input
        /// </summary>
        private bool CheckPackages(TextWriter textWriter)
        {
            var allGood = true;

            // The VS.Tools.Roslyn package is a bit of a historical artifact from how our files used to 
            // be laid out in the VS repository. The structure is flat which means the build assets are 
            // mixed in with package artifacts and that makes the verification a bit more complicated and 
            // more needs to be excluded
            // 
            // The one to call out is excluding csc.exe for validation. That is because it's custom stamped
            // as an x86 exe to work around an issue in the VS build system
            //
            // https://github.com/dotnet/roslyn/issues/17864
            allGood &= VerifyPackageCore(
                textWriter,
                FindNuGetPackage(Path.Combine(ArtifactsDirectory, "VSSetup", Configuration, "DevDivPackages"), "VS.Tools.Roslyn"),
                excludeFunc: relativeFileName =>
                    PathComparer.Equals(relativeFileName, "csc.exe") ||
                    PathComparer.Equals(relativeFileName, "Icon.png") ||
                    PathComparer.Equals(relativeFileName, "Init.cmd") ||
                    PathComparer.Equals(relativeFileName, "VS.Tools.Roslyn.nuspec") ||
                    PathComparer.Equals(relativeFileName, "vbc.exe") ||
                    relativeFileName.EndsWith(".resources.dll", PathComparison) ||
                    relativeFileName.EndsWith(".rels", PathComparison) ||
                    relativeFileName.EndsWith(".psmdcp", PathComparison),
                ("", GetProjectOutputDirectory("csc", "net472")),
                ("", GetProjectOutputDirectory("vbc", "net472")),
                ("", GetProjectOutputDirectory("csi", "net472")),
                ("", GetProjectOutputDirectory("VBCSCompiler", "net472")),
                ("", GetProjectOutputDirectory("Microsoft.Build.Tasks.CodeAnalysis", "net472")));

            allGood &= VerifyPackageCore(
                textWriter,
                FindNuGetPackage(Path.Combine(ArtifactsDirectory, "packages", Configuration, "Shipping"), "Microsoft.Net.Compilers.Toolset.Arm64"),
                (@"tasks\net472", GetProjectOutputDirectory("csc-arm64", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("vbc-arm64", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("csi", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("VBCSCompiler-arm64", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("Microsoft.Build.Tasks.CodeAnalysis", "net472"))); ;

            allGood &= VerifyPackageCore(
                textWriter,
                FindNuGetPackage(Path.Combine(ArtifactsDirectory, "packages", Configuration, "Shipping"), "Microsoft.Net.Compilers.Toolset.Framework"),
                (@"tasks\net472", GetProjectOutputDirectory("csc", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("vbc", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("csi", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("VBCSCompiler", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("Microsoft.Build.Tasks.CodeAnalysis", "net472"))); ;

            allGood &= VerifyPackageCore(
                textWriter,
                FindNuGetPackage(Path.Combine(ArtifactsDirectory, "packages", Configuration, "Shipping"), "Microsoft.Net.Compilers.Toolset"),
                excludeFunc: relativeFileName =>
                    relativeFileName.StartsWith(@"tasks\netcore\bincore\Microsoft.DiaSymReader.Native", PathComparison) ||
                    relativeFileName.StartsWith(@"tasks\netcore\bincore\Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler.dll", PathComparison),
                (@"tasks\net472", GetProjectOutputDirectory("csc", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("vbc", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("csi", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("VBCSCompiler", "net472")),
                (@"tasks\net472", GetProjectOutputDirectory("Microsoft.Build.Tasks.CodeAnalysis", "net472")),
                (@"tasks\netcore\bincore", GetProjectPublishDirectory("csc", "net8.0")),
                (@"tasks\netcore\bincore", GetProjectPublishDirectory("vbc", "net8.0")),
                (@"tasks\netcore\bincore", GetProjectPublishDirectory("VBCSCompiler", "net8.0")),
                (@"tasks\netcore", GetProjectPublishDirectory("Microsoft.Build.Tasks.CodeAnalysis", "net8.0")));

            foreach (var arch in new[] { "x86", "x64", "arm64" })
            {
                var suffix = arch == "arm64" ? "-arm64" : "";
                allGood &= VerifyPackageCore(
                    textWriter,
                    FindVsix($"Microsoft.CodeAnalysis.Compilers.{arch}"),
                    (@"Contents\MSBuild\Current\Bin\Roslyn", GetProjectOutputDirectory($"csc{suffix}", "net472")),
                    (@"Contents\MSBuild\Current\Bin\Roslyn", GetProjectOutputDirectory($"vbc{suffix}", "net472")),
                    (@"Contents\MSBuild\Current\Bin\Roslyn", GetProjectOutputDirectory("csi", "net472")),
                    (@"Contents\MSBuild\Current\Bin\Roslyn", GetProjectOutputDirectory($"VBCSCompiler{suffix}", "net472")),
                    (@"Contents\MSBuild\Current\Bin\Roslyn", GetProjectOutputDirectory("Microsoft.Build.Tasks.CodeAnalysis", "net472"))); ;
            }

            return allGood;
        }

        private string GetProjectOutputDirectory(string projectName, string tfm)
            => Path.Combine(ArtifactsDirectory, "bin", projectName, Configuration, tfm);

        private string GetProjectPublishDirectory(string projectName, string tfm)
            => Path.Combine(ArtifactsDirectory, "bin", projectName, Configuration, tfm, "publish");

        private static bool VerifyPackageCore(
            TextWriter textWriter,
            string packageFilePath,
            params (string PackageFolderRelativePath, string BuildOutputFolder)[] packageInputs)
            => VerifyPackageCore(
                textWriter,
                packageFilePath,
                static _ => false,
                packageInputs);

        private static bool VerifyPackageCore(
            TextWriter textWriter,
            string packageFilePath,
            Func<string, bool> excludeFunc,
            params (string PackageFolderRelativePath, string BuildOutputDirectory)[] packageInputs)
        {
            textWriter.WriteLine($"Verifying {packageFilePath}");
            using var package = Package.Open(packageFilePath, FileMode.Open, FileAccess.Read);
            var allGood = true;
            var partList = GetPackagePartDataList(package);
            var partMap = partList.ToDictionary(x => x.RelativeName);
            var foundSet = new HashSet<string>(PathComparer);

            // First ensure all of the files in the build output directories is included in the 
            // correct folder in the package file
            foreach (var tuple in packageInputs)
            {
                var buildAssets = Directory
                    .EnumerateFiles(tuple.BuildOutputDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(IsTrackedAsset);
                var folderRelativePath = tuple.PackageFolderRelativePath;
                foreach (var buildAssetFilePath in buildAssets)
                {
                    var buildAssetRelativePath = buildAssetFilePath.Substring(tuple.BuildOutputDirectory.Length + 1);
                    buildAssetRelativePath = Path.Combine(folderRelativePath, buildAssetRelativePath);
                    if (excludeFunc(buildAssetRelativePath))
                    {
                        continue;
                    }

                    if (!partMap.TryGetValue(buildAssetRelativePath, out var partData))
                    {
                        allGood = false;
                        textWriter.WriteLine($"\tPart {buildAssetRelativePath} missing from package");
                        continue;
                    }

                    foundSet.Add(buildAssetRelativePath);
                    var buildAssetChecksum = GetChecksum(buildAssetFilePath);
                    if (buildAssetChecksum != partData.Checksum)
                    {
                        allGood = false;
                        textWriter.WriteLine($"\tPart {buildAssetFilePath} has wrong checksum in package");
                        textWriter.WriteLine($"\t\tBuild output {buildAssetFilePath}");
                        textWriter.WriteLine($"\t\tPackage part {partData.Checksum}");
                        continue;
                    }
                }
            }

            // Sanity check to make sure that we didn't accidentall include a series of empty directories
            if (foundSet.Count < 5)
            {
                allGood = false;
                textWriter.WriteLine($"Found {foundSet.Count} items in package which is far less than expected");
            }

            // Next ensure that all of the files in the package folders were expected (aka they 
            // came from the build output)
            foreach (var packageFolder in packageInputs.Select(x => x.PackageFolderRelativePath).Distinct().OrderBy(x => x))
            {
                foreach (var partData in partList)
                {
                    if (excludeFunc(partData.RelativeName))
                    {
                        continue;
                    }

                    if (partData.RelativeName.StartsWith(packageFolder, PathComparison) &&
                        !foundSet.Contains(partData.RelativeName))
                    {
                        textWriter.WriteLine($"\tFound unexpected part {partData.RelativeName}");
                        allGood = false;
                    }
                }
            }

            return allGood;
        }

        private static List<PackagePartData> GetPackagePartDataList(Package package)
        {
            var list = new List<PackagePartData>();
            foreach (var part in package.GetParts())
            {
                var relativeName = part.GetRelativeName();
                if (string.IsNullOrEmpty(relativeName))
                {
                    continue;
                }

                using var stream = part.GetStream(FileMode.Open, FileAccess.Read);
                var checksum = GetChecksum(stream);
                list.Add(new PackagePartData(part, checksum));
            }

            return list;
        }

        private static string GetChecksum(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return GetChecksum(stream);
        }

        private static string GetChecksum(Stream stream)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream));
        }

        /// <summary>
        /// Get all of the parts in the specified folder. Will exclude all items in child folders.
        /// </summary>
        private static IEnumerable<PackagePart> GetPartsInFolder(Package package, string folderRelativePath)
        {
            Debug.Assert(string.IsNullOrEmpty(folderRelativePath) || folderRelativePath[0] != '\\');
            var list = GetPackagePartDataList(package);
            return list
                .Where(x => x.RelativeName.StartsWith(folderRelativePath, PathComparison))
                .Select(x => x.PackagePart);
        }

        /// <summary>
        /// Verifies the VS.ExternalAPIs.Roslyn package is self consistent. Need to ensure that we insert all of the project dependencies
        /// that we build into the package. If we miss a dependency then the VS insertion will fail. Big refactorings can often forget to
        /// properly update this package.
        /// </summary>
        /// <param name="textWriter"></param>
        /// <returns></returns>
        private bool CheckExternalApis(TextWriter textWriter)
        {
            var packageFilePath = FindNuGetPackage(Path.Combine(ArtifactsDirectory, "VSSetup", Configuration, "DevDivPackages"), "VS.ExternalAPIs.Roslyn");
            var allGood = true;

            // This tracks the packages which are included in separate packages. Hence they don't need to
            // be included here.
            var excludedNameSet = new HashSet<string>(PathComparer)
            {
                "Microsoft.CodeAnalysis.Elfie"
            };

            textWriter.WriteLine("Verifying contents of VS.ExternalAPIs.Roslyn");
            textWriter.WriteLine("\tRoot Folder");
            verifyFolder("");
            return allGood;

            void verifyFolder(string folderRelativeName)
            {
                var foundDllNameSet = new HashSet<string>(PathComparer);
                var neededDllNameSet = new HashSet<string>(PathComparer);
                using var package = Package.Open(packageFilePath, FileMode.Open, FileAccess.Read);
                foreach (var part in GetPartsInFolder(package, folderRelativeName))
                {
                    var name = part.GetName();
                    if (Path.GetExtension(name) is not (".dll" or ".exe"))
                    {
                        continue;
                    }

                    foundDllNameSet.Add(Path.GetFileNameWithoutExtension(name));
                    using var peReader = new PEReader(part.GetStream(FileMode.Open, FileAccess.Read));
                    var metadataReader = peReader.GetMetadataReader();
                    foreach (var handle in metadataReader.AssemblyReferences)
                    {
                        var assemblyReference = metadataReader.GetAssemblyReference(handle);
                        var assemblyName = metadataReader.GetString(assemblyReference.Name);
                        neededDllNameSet.Add(assemblyName);
                    }
                }

                if (foundDllNameSet.Count == 0)
                {
                    allGood = false;
                    textWriter.WriteLine($"\t\tFound zero DLLs in {folderRelativeName}");
                    return;
                }

                // As a simplification we only validate the assembly names that begin with Microsoft.CodeAnalysis. This is a good 
                // heuristic for finding assemblies that we build. Can be expanded in the future if we find more assemblies that
                // are worth validating here.
                var neededDllNames = neededDllNameSet
                    .Where(x => x.StartsWith("Microsoft.CodeAnalysis"))
                    .OrderBy(x => x, PathComparer);
                foreach (var name in neededDllNames)
                {
                    if (!foundDllNameSet.Contains(name) && !excludedNameSet.Contains(name))
                    {
                        textWriter.WriteLine($"\t\tMissing dependency {name}");
                        allGood = false;
                    }
                }
            }
        }

        private string FindNuGetPackage(string directory, string partialName)
        {
            var regex = $@"{partialName}.\d.*\.nupkg";
            var file = Directory
                .EnumerateFiles(directory, "*.nupkg")
                .Where(filePath =>
                {
                    var fileName = Path.GetFileName(filePath);
                    return Regex.IsMatch(fileName, regex);
                })
               .SingleOrDefault();
            return file ?? throw new Exception($"Unable to find unique '{partialName}' in '{directory}'");
        }

        private string FindVsix(string fileName)
        {
            fileName = fileName + ".vsix";
            var directory = Path.Combine(ArtifactsDirectory, "VSSetup", Configuration);
            var file = Directory.EnumerateFiles(directory, fileName, SearchOption.AllDirectories).SingleOrDefault();
            return file ?? throw new Exception($"Unable to find '{fileName}' in '{directory}'");
        }

        /// <summary>
        /// The set of files that we track as assets in the NuPkg / VSIX files
        /// </summary>
        private static bool IsTrackedAsset(string filePath)
        {
            return
                filePath.EndsWith(".exe", PathComparison) ||
                filePath.EndsWith(".dll", PathComparison) ||
                filePath.EndsWith(".targets", PathComparison) ||
                filePath.EndsWith(".config", PathComparison) ||
                filePath.EndsWith(".rsp", PathComparison) ||
                filePath.EndsWith(".deps.json", PathComparison) ||
                filePath.EndsWith(".runtimeconfig.json", PathComparison) ||
                filePath.EndsWith(".props", PathComparison);
        }
    }
}
