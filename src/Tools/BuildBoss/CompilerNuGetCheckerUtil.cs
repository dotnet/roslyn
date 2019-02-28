﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BuildBoss
{
    /// <summary>
    /// Verifies the contents of our toolset NuPkg and SWR files are correct.
    /// 
    /// The compiler toolset is a particularly difficult package to get correct. In essense it is 
    /// merging the output of three different exes into a single directory. That causes a number 
    /// of issues during pack time:
    /// 
    ///     - The dependencies are not necessarily equal between all exes
    ///     - The dependencies can change based on subtle changes to the code
    ///     - There is no project which is guaranteed to have a superset of dependencies 
    ///     - There is no syntax for using the union of DLLs in a NuSpec file
    ///
    /// The least crazy solution that could be decided on was to manage the list of dependencies 
    /// by hand in the NuSpec file and then rigorously verify the solution here.
    /// </summary>
    internal sealed class PackageContentsChecker : ICheckerUtil
    {
        private readonly struct PackageAsset
        {
            public bool IsDesktop { get; }
            public string FileRelativeName { get; }
            public string Checksum { get; }
            public bool IsCoreClr => !IsDesktop;
            public string FileName => Path.GetFileName(FileRelativeName);

            public PackageAsset(string fileRelativeName, string checksum, bool isDesktop)
            {
                FileRelativeName = fileRelativeName;
                Checksum = checksum;
                IsDesktop = isDesktop;
            }

            public PackageAsset WithFileRelativeName(string fileRelativeName) => new PackageAsset(fileRelativeName, Checksum, IsDesktop);

            public override string ToString() => FileRelativeName;
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
                var packageAssets = new List<PackageAsset>();
                if (!GetPackageAssets(textWriter, packageAssets))
                {
                    return false;
                }

                var allGood = true;
                allGood &= CheckDesktop(textWriter, filter(isDesktop: true));
                allGood &= CheckCoreClr(textWriter, filter(isDesktop: false));
                allGood &= CheckCombined(textWriter, packageAssets);
                return allGood;

                IEnumerable<string> filter(bool isDesktop) => packageAssets.Where(x => x.IsDesktop == isDesktop).Select(x => x.FileRelativeName);
            }
            catch (Exception ex)
            {
                textWriter.WriteLine($"Error verifying: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verify the contents of our desktop targeting compiler packages are correct.
        /// </summary>
        private bool CheckDesktop(TextWriter textWriter, IEnumerable<string> assetRelativeNames)
        {
            var allGood = true;
            allGood &= VerifyNuPackage(
                        textWriter,
                        FindNuGetPackage(Path.Combine(ArtifactsDirectory, "packages", Configuration, "Shipping"), "Microsoft.Net.Compilers"),
                        @"tools",
                        assetRelativeNames);

            allGood &= VerifyNuPackage(
                        textWriter,
                        FindNuGetPackage(Path.Combine(ArtifactsDirectory, "VSSetup", Configuration, "DevDivPackages"), "VS.Tools.Roslyn"),
                        string.Empty,
                        assetRelativeNames);

            allGood &= VerifyVsix(
                        textWriter,
                        FindVsix("Roslyn.Compilers.Extension"),
                        assetRelativeNames.Concat(new[] { "Roslyn.Compilers.Extension.dll" }));
            return allGood;
        }

        /// <summary>
        /// Verify the contents of our desktop targeting compiler packages are correct.
        /// </summary>
        private bool CheckCoreClr(TextWriter textWriter, IEnumerable<string> assetRelativeNames)
        {
            return VerifyNuPackage(
                        textWriter,
                        FindNuGetPackage(Path.Combine(ArtifactsDirectory, "packages", Configuration, "Shipping"), "Microsoft.NETCore.Compilers"),
                        @"tools",
                        assetRelativeNames);
        }

        /// <summary>
        /// Verify the contents of our combinde toolset compiler packages are correct.
        /// </summary>
        private bool CheckCombined(TextWriter textWriter, IEnumerable<PackageAsset> packageAssets)
        {
            var list = new List<string>();
            foreach (var asset in packageAssets)
            {
                var folder = asset.IsDesktop
                    ? @"net472"
                    : @"netcoreapp2.1";
                var fileRelativeName = Path.Combine(folder, asset.FileRelativeName);
                list.Add(fileRelativeName);
            }

            return VerifyNuPackage(
                    textWriter,
                    FindNuGetPackage(Path.Combine(ArtifactsDirectory, "packages", Configuration, "Shipping"), "Microsoft.Net.Compilers.Toolset"),
                    @"tasks",
                    list);
        }

        private bool GetPackageAssets(TextWriter textWriter, List<PackageAsset> packageAssets)
        {
            var allGood = true;
            var desktopAssets = new List<PackageAsset>();
            var coreClrAssets = new List<PackageAsset>();

            allGood &= GetPackageAssetsCore(
                textWriter,
                isDesktop: true,
                desktopAssets,
                $@"csc\{Configuration}\net472",
                $@"vbc\{Configuration}\net472",
                $@"csi\{Configuration}\net472",
                $@"VBCSCompiler\{Configuration}\net472",
                $@"Microsoft.Build.Tasks.CodeAnalysis\{Configuration}\net472");

            allGood &= GetPackageAssetsCore(
                textWriter,
                isDesktop: false,
                coreClrAssets,
                $@"csc\{Configuration}\netcoreapp2.1\publish",
                $@"vbc\{Configuration}\netcoreapp2.1\publish",
                $@"VBCSCompiler\{Configuration}\netcoreapp2.1\publish");

            // The native DLLs ship inside the runtime specific directories but build deploys it at the 
            // root as well. That copy is unnecessary.
            coreClrAssets.RemoveAll(asset =>
                PathComparer.Equals("Microsoft.DiaSymReader.Native.amd64.dll", asset.FileRelativeName) ||
                PathComparer.Equals("Microsoft.DiaSymReader.Native.x86.dll", asset.FileRelativeName));

            // Move all of the assets into bincore as that is where the non-MSBuild task assets will go
            coreClrAssets = coreClrAssets.Select(x => x.WithFileRelativeName(Path.Combine("bincore", x.FileRelativeName))).ToList();

            allGood &= GetPackageAssetsCore(
                textWriter,
                isDesktop: false,
                coreClrAssets,
                $@"Microsoft.Build.Tasks.CodeAnalysis\{Configuration}\netcoreapp2.1\publish");

            packageAssets.AddRange(desktopAssets);
            packageAssets.AddRange(coreClrAssets);
            packageAssets.Sort((x, y) => x.FileRelativeName.CompareTo(y.FileRelativeName));
            return allGood;
        }

        /// <summary>
        /// Get all of the dependencies in the specified directory set. 
        /// </summary>
        private bool GetPackageAssetsCore(TextWriter textWriter, bool isDesktop, List<PackageAsset> packageAssets, params string[] directoryPaths)
        {
            var relativeNameMap = new Dictionary<string, PackageAsset>(PathComparer);
            var allGood = true;

            IEnumerable<string> enumerateAssets(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly)
            {
                return Directory
                    .EnumerateFiles(directory, "*.*", searchOption)
                    .Where(IsTrackedAsset);
            }

            // This will record all of the assets files in a directory. The name of the assets and the checksum of the contents will 
            // be added to the map
            void recordDependencies(MD5 md5, string directory)
            {
                // Need to consider the files in the immediate directory and those in the runtimes directory. The resource dlls
                // are unique and simple to include hence we don't go through the process of verifying them.
                IEnumerable<string> enumerateFiles()
                {
                    foreach (var filePath in enumerateAssets(directory))
                    {
                        yield return filePath;
                    }

                    var runtimeDirectory = Path.Combine(directory, "runtimes");
                    if (Directory.Exists(runtimeDirectory))
                    {
                        foreach (var filePath in enumerateAssets(runtimeDirectory, SearchOption.AllDirectories))
                        {
                            yield return filePath;
                        }
                    }
                }

                var normalizedDirectoryName = (directory[directory.Length - 1] == '\\') ? directory : directory + @"\";
                string getRelativeName(string filePath) => filePath.Substring(normalizedDirectoryName.Length);

                var foundOne = false;
                foreach (var assetFilePath in enumerateFiles())
                {
                    foundOne = true;
                    using (var stream = File.Open(assetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var assetRelativeName = getRelativeName(assetFilePath);
                        var hash = md5.ComputeHash(stream);
                        var hashString = BitConverter.ToString(hash);
                        if (relativeNameMap.TryGetValue(assetRelativeName, out PackageAsset existingAsset))
                        {
                            // Make sure that all copies of the DLL have the same contents. The DLLs are being merged into
                            // a single directory in the resulting NuGet. If the contents are different then our merge is 
                            // invalid.
                            if (existingAsset.Checksum != hashString)
                            {
                                textWriter.WriteLine($"Asset {assetRelativeName} exists at two different versions");
                                textWriter.WriteLine($"\tHash 1: {hashString}");
                                textWriter.WriteLine($"\tHash 2: {existingAsset.Checksum}");
                                allGood = false;
                            }
                        }
                        else
                        {
                            var packageAsset = new PackageAsset(assetRelativeName, hashString, isDesktop);
                            packageAssets.Add(packageAsset);
                            relativeNameMap[assetRelativeName] = packageAsset;
                        }
                    }
                }

                if (!foundOne)
                {
                    textWriter.WriteLine($"Directory {directory} did not have any assets");
                    allGood = false;
                }
            }

            using (var md5 = MD5.Create())
            {
                foreach (var directory in directoryPaths)
                {
                    recordDependencies(md5, Path.Combine(ArtifactsDirectory, "bin", directory));
                }
            }

            return allGood;
        }

        private static bool VerifyNuPackage(
            TextWriter textWriter,
            string nupkgFilePath,
            string folderRelativePath,
            IEnumerable<string> dllFileNames) => VerifyCore(textWriter, nupkgFilePath, folderRelativePath, dllFileNames);

        private static bool VerifyVsix(
            TextWriter textWriter,
            string vsixFilePath,
            IEnumerable<string> dllFileNames) => VerifyCore(textWriter, vsixFilePath, folderRelativePath: "", dllFileNames);

        private static bool VerifyCore(
            TextWriter textWriter,
            string packageFilePath,
            string folderRelativePath,
            IEnumerable<string> dllFileNames)
        {
            Debug.Assert(string.IsNullOrEmpty(folderRelativePath) || folderRelativePath[0] != '\\');

            // Get all of the assets parts that are in the specified folder. Will exclude items that
            // are in any child folder
            IEnumerable<string> getPartsInFolder()
            {
                using (var package = Package.Open(packageFilePath, FileMode.Open, FileAccess.Read))
                {
                    foreach (var part in package.GetParts())
                    {
                        var relativeName = part.Uri.ToString().Replace('/', '\\');
                        if (string.IsNullOrEmpty(relativeName))
                        {
                            continue;
                        }

                        if (relativeName[0] == '\\')
                        {
                            relativeName = relativeName.Substring(1);
                        }

                        if (!relativeName.StartsWith(folderRelativePath, PathComparison))
                        {
                            continue;
                        }

                        if (IsTrackedAsset(relativeName))
                        {
                            yield return relativeName;
                        }
                    }
                }
            }

            var map = dllFileNames
                .ToDictionary(
                    keySelector: x => Path.Combine(folderRelativePath, x),
                    elementSelector: _ => false,
                    comparer: PathComparer);
            var allGood = true;
            var packageFileName = Path.GetFileName(packageFilePath);

            textWriter.WriteLine($"Verifying {packageFileName}");
            foreach (var relativeName in getPartsInFolder())
            {
                var name = Path.GetFileName(relativeName);
                if (map.TryGetValue(relativeName, out var isFound))
                {
                    if (isFound)
                    {
                        textWriter.WriteLine($"\tFound duplicate part {relativeName}");
                        allGood = false;
                    }
                    else
                    {
                        map[relativeName] = true;
                    }
                }
                else
                {
                    textWriter.WriteLine($"\tFound unexpected asset {relativeName}");
                    allGood = false;
                }
            }

            foreach (var pair in map.Where(x => !x.Value))
            {
                textWriter.WriteLine($"\tAsset {pair.Key} not found");
                allGood = false;
            }

            return allGood;
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
            var file = Directory.EnumerateFiles(directory, fileName).SingleOrDefault();
            return file ?? throw new Exception($"Unable to find '{fileName}' in '{directory}'");
        }

        /// <summary>
        /// The set of files that we track as assets in the NuPkg file
        /// </summary>
        private static bool IsTrackedAsset(string filePath)
        {
            if (filePath.EndsWith(".dll", PathComparison))
            {
                return !filePath.EndsWith(".resources.dll");
            }

            return
                filePath.EndsWith(".exe") ||
                filePath.EndsWith(".targets") ||
                filePath.EndsWith(".props");
        }
    }
}
