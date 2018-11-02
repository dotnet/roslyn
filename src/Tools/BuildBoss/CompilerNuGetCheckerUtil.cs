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
        internal static StringComparer PathComparer { get; } = StringComparer.OrdinalIgnoreCase;
        internal static StringComparison PathComparison { get; } = StringComparison.OrdinalIgnoreCase;

        internal string ConfigDirectory { get; }
        internal string RepositoryDirectory { get; }

        internal PackageContentsChecker(string repositoryDirectory, string configDirectory)
        {
            RepositoryDirectory = repositoryDirectory;
            ConfigDirectory = configDirectory;
        }

        public bool Check(TextWriter textWriter)
        {
            try
            {
                var allGood = CheckDesktop(textWriter);
                allGood &= CheckCoreClr(textWriter);
                return allGood;
            }
            catch (Exception ex)
            {
                textWriter.WriteLine($"Error verifying NuPkg files: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verify the contents of our desktop targeting compiler packages are correct.
        /// </summary>
        private bool CheckDesktop(TextWriter textWriter)
        {
            var (allGood, dllRelativeNames) = GetDllRelativeNames(
                textWriter,
                @"Exes\Csc\net472",
                @"Exes\Vbc\net472",
                @"Exes\Csi\net472",
                @"Exes\VBCSCompiler\net472",
                @"Dlls\Microsoft.Build.Tasks.CodeAnalysis\net472");
            if (!allGood)
            {
                return false;
            }

            // These are the core MSBuild dlls that will always be present / redirected when running
            // inside of desktop MSBuild. Even though they are in our output directories they should
            // not be a part of our deployment
            // need to be 
            dllRelativeNames = FilterRelativeFileNames(
                dllRelativeNames,
                "Microsoft.Build.dll",
                "Microsoft.Build.Framework.dll",
                "Microsoft.Build.Tasks.Core.dll",
                "Microsoft.Build.Utilities.Core.dll").ToList();

            allGood &= VerifyNuPackage(
                        textWriter,
                        FindNuGetPackage(@"NuGet\PreRelease", "Microsoft.Net.Compilers"),
                        @"tools",
                        dllRelativeNames);

            allGood &= VerifyNuPackage(
                        textWriter,
                        FindNuGetPackage(@"DevDivPackages\Roslyn", "VS.Tools.Roslyn"),
                        string.Empty,
                        dllRelativeNames);
            return allGood;
        }

        /// <summary>
        /// Verify the contents of our desktop targeting compiler packages are correct.
        /// </summary>
        private bool CheckCoreClr(TextWriter textWriter)
        {
            var (allGood, dllRelativeNames) = GetDllRelativeNames(
                textWriter,
                @"Exes\Csc\netcoreapp2.1\publish",
                @"Exes\Vbc\netcoreapp2.1\publish",
                @"Exes\VBCSCompiler\netcoreapp2.1\publish");
            if (!allGood)
            {
                return false;
            }

            // The native DLLs ship inside the runtime specific directories but build deploys it at the 
            // root as well. That copy is unnecessary.
            dllRelativeNames = FilterRelativeFileNames(
                dllRelativeNames,
                "Microsoft.DiaSymReader.Native.amd64.dll",
                "Microsoft.DiaSymReader.Native.x86.dll").ToList();

            return VerifyNuPackage(
                        textWriter,
                        FindNuGetPackage(@"NuGet\PreRelease", "Microsoft.NETCore.Compilers"),
                        @"tools\bincore",
                        dllRelativeNames);
        }

        /// <summary>
        /// Get all of the dependencies in the specified directory set. 
        /// </summary>
        private (bool succeeded, List<string> dllRelativeNames) GetDllRelativeNames(TextWriter textWriter, params string[] directoryPaths)
        {
            var dllToChecksumMap = new Dictionary<string, string>(PathComparer);
            var allGood = true;

            // This will record all of the DLL files in a directory. The name of the DLL and the checksum of the contents will 
            // be added to the map
            void recordDependencies(MD5 md5, string directory)
            {
                // Need to consider the files in the immediate directory and those in the runtimes directory. The resource dlls
                // are unique and simple to include hence we don't go through the process of verifying them.
                IEnumerable<string> enumerateFiles()
                {
                    foreach (var filePath in Directory.EnumerateFiles(directory, "*.dll"))
                    {
                        yield return filePath;
                    }

                    var runtimeDirectory = Path.Combine(directory, "runtimes");
                    if (Directory.Exists(runtimeDirectory))
                    {
                        foreach (var filePath in Directory.EnumerateFiles(Path.Combine(runtimeDirectory), "*.dll", SearchOption.AllDirectories))
                        {
                            yield return filePath;
                        }
                    }
                }

                var normalizedDirectoryName = (directory[directory.Length - 1] == '\\') ? directory : directory + @"\";
                string getRelativeName(string filePath) => filePath.Substring(normalizedDirectoryName.Length);

                var foundOne = false;
                foreach (var dllFilePath in enumerateFiles())
                {
                    foundOne = true;
                    var dllRelativeName = getRelativeName(dllFilePath);
                    using (var stream = File.Open(dllFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var hash = md5.ComputeHash(stream);
                        var hashString = BitConverter.ToString(hash);
                        if (dllToChecksumMap.TryGetValue(dllRelativeName, out string existingHashString))
                        {
                            // Make sure that all copies of the DLL have the same contents. The DLLs are being merged into
                            // a single directory in the resulting NuGet. If the contents are different then our merge is 
                            // invalid.
                            if (existingHashString != hashString)
                            {
                                textWriter.WriteLine($"Dll {dllRelativeName} exists at two different versions");
                                textWriter.WriteLine($"\tHash 1: {hashString}");
                                textWriter.WriteLine($"\tHash 2: {existingHashString}");
                                allGood = false;
                            }
                        }
                        else
                        {
                            dllToChecksumMap.Add(dllRelativeName, hashString);
                        }
                    }
                }

                if (!foundOne)
                {
                    textWriter.WriteLine($"Directory {directory} did not have any dlls");
                    allGood = false;
                }
            }

            using (var md5 = MD5.Create())
            {
                foreach (var directory in directoryPaths)
                {
                    recordDependencies(md5, Path.Combine(ConfigDirectory, directory));
                }
            }

            var dllFileNames = dllToChecksumMap.Keys.OrderBy(x => x).ToList();
            return (allGood, dllFileNames);
        }

        private IEnumerable<string> FilterRelativeFileNames(IEnumerable<string> relativeFileNames, params string[] excludeNames)
        {
            foreach (var relativeFileName in relativeFileNames)
            {
                var keep = true;
                foreach (var excludeName in excludeNames)
                {
                    if (PathComparer.Equals(excludeName, relativeFileName))
                    {
                        keep = false;
                        break;
                    }
                }

                if (keep)
                {
                    yield return relativeFileName;
                }
            }
        }

        private static bool VerifyNuPackage(
            TextWriter textWriter, 
            string nupkgFilePath, 
            string folderRelativePath, 
            IEnumerable<string> dllFileNames)
        {
            Debug.Assert(string.IsNullOrEmpty(folderRelativePath) || folderRelativePath[0] != '\\');

            // Get all of the DLL parts that are in the specified folder. Will exclude items that
            // are in any child folder
            IEnumerable<string> getPartsInFolder()
            {
                using (var package = Package.Open(nupkgFilePath, FileMode.Open, FileAccess.Read))
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

                        if (!relativeName.StartsWith(folderRelativePath, PathComparison) ||
                            !relativeName.EndsWith(".dll", PathComparison) ||
                            relativeName.EndsWith(".resources.dll", PathComparison))
                        {
                            continue;
                        }

                        yield return relativeName;
                    }
                }
            }

            var map = dllFileNames
                .ToDictionary(
                    keySelector: x => Path.Combine(folderRelativePath, x),
                    elementSelector: _ => false,
                    comparer: PathComparer);
            var allGood = true;
            var nupkgFileName = Path.GetFileName(nupkgFilePath);

            textWriter.WriteLine($"Verifying NuPkg {nupkgFileName}");
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
                    textWriter.WriteLine($"\tFound unexpected dll {relativeName}");
                    allGood = false;
                }
            }

            foreach (var pair in map.Where(x => !x.Value))
            {
                textWriter.WriteLine($"\tDll {pair.Key} not found");
                allGood = false;
            }

            return allGood;
        }

        private string FindNuGetPackage(string directory, string partialName)
        {
            var file = Directory
                .EnumerateFiles(Path.Combine(ConfigDirectory, directory), partialName + "*.nupkg")
                .SingleOrDefault();
            if (file == null)
            {
                throw new Exception($"Unable to find NuPgk {partialName} in {directory}");
            }

            return file;
        }
    }
}
