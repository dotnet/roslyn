// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

internal static class MinimizeUtil
{
    internal record FilePathInfo(string RelativeDirectory, string Directory, string RelativePath, string FullPath);

    internal static void Run(string sourceDirectory, string destinationDirectory)
    {
        // Map of all PE files MVID to the path information
        var idToFilePathMap = new Dictionary<Guid, List<FilePathInfo>>();

        const string duplicateDirectoryName = ".duplicate";
        var duplicateDirectory = Path.Combine(destinationDirectory, duplicateDirectoryName);
        Directory.CreateDirectory(duplicateDirectory);

        initialWalk();
        resolveDuplicates();
        writeHydrateFile();

        // The goal of initial walk is to
        //  1. Record any PE files as they are eligable for de-dup
        //  2. Hard link all other files into destination directory
        void initialWalk()
        {
            IEnumerable<string> directories = new[] {
                Path.Combine(sourceDirectory, "eng")
            };
            var artifactsDir = Path.Combine(sourceDirectory, "artifacts/bin");
            directories = directories.Concat(Directory.EnumerateDirectories(artifactsDir, "*.UnitTests"));
            directories = directories.Concat(Directory.EnumerateDirectories(artifactsDir, "RunTests"));

            foreach (var unitDirPath in directories)
            {
                foreach (var sourceFilePath in Directory.EnumerateFiles(unitDirPath, "*", SearchOption.AllDirectories))
                {
                    var currentDirName = Path.GetDirectoryName(sourceFilePath)!;
                    var currentRelativeDirectory = Path.GetRelativePath(sourceDirectory, currentDirName);
                    var currentOutputDirectory = Path.Combine(destinationDirectory, currentRelativeDirectory);
                    Directory.CreateDirectory(currentOutputDirectory);
                    var fileName = Path.GetFileName(sourceFilePath);

                    if (fileName.EndsWith(".dll") && TryGetMvid(sourceFilePath, out var mvid))
                    {
                        if (!idToFilePathMap.TryGetValue(mvid, out var list))
                        {
                            list = new List<FilePathInfo>();
                            idToFilePathMap[mvid] = list;
                        }

                        var filePathInfo = new FilePathInfo(
                            RelativeDirectory: currentRelativeDirectory,
                            Directory: currentDirName,
                            RelativePath: Path.Combine(currentRelativeDirectory, fileName),
                            FullPath: sourceFilePath);
                        list.Add(filePathInfo);
                    }
                    else
                    {
                        var destFilePath = Path.Combine(currentOutputDirectory, fileName);
                        CreateHardLink(destFilePath, sourceFilePath);
                    }
                }
            }

            // https://github.com/dotnet/roslyn/issues/49486
            // we should avoid copying the files under Resources.
            var individualFiles = new[]
            {
                "./global.json",
                "src/Workspaces/MSBuildTest/Resources/.editorconfig",
                "src/Workspaces/MSBuildTest/Resources/global.json",
                "src/Workspaces/MSBuildTest/Resources/Directory.Build.props",
                "src/Workspaces/MSBuildTest/Resources/Directory.Build.targets",
                "src/Workspaces/MSBuildTest/Resources/Directory.Build.rsp",
                "src/Workspaces/MSBuildTest/Resources/NuGet.Config",
            };

            foreach (var individualFile in individualFiles)
            {
                var currentDirName = Path.GetDirectoryName(individualFile)!;
                var currentRelativeDirectory = Path.GetRelativePath(sourceDirectory, currentDirName);
                var currentOutputDirectory = Path.Combine(destinationDirectory, currentRelativeDirectory);
                Directory.CreateDirectory(currentOutputDirectory);

                var destGlobalJsonPath = Path.Combine(destinationDirectory, individualFile);
                CreateHardLink(destGlobalJsonPath, Path.Combine(sourceDirectory, individualFile));
            }
        }

        // Now that we have a complete list of PE files, determine which are duplicates
        void resolveDuplicates()
        {
            foreach (var pair in idToFilePathMap)
            {
                if (pair.Value.Count > 1)
                {
                    CreateHardLink(getPeFilePath(pair.Key), pair.Value[0].FullPath);
                }
                else
                {
                    var item = pair.Value[0];
                    var destFilePath = Path.Combine(destinationDirectory, item.RelativePath);
                    CreateHardLink(destFilePath, item.FullPath);
                }
            }
        }

        string getPeFileName(Guid mvid) => mvid.ToString();

        string getPeFilePath(Guid mvid) => Path.Combine(duplicateDirectory, getPeFileName(mvid));

        void writeHydrateFile()
        {
            var fileList = new List<string>();
            var grouping = idToFilePathMap
                .Where(x => x.Value.Count > 1)
                .SelectMany(pair => pair.Value.Select(fp => (Id: pair.Key, FilePath: fp)))
                .GroupBy(fp => fp.FilePath.RelativeDirectory);
            var builder = new StringBuilder();
            var count = 0;
            foreach (var group in grouping)
            {
                foreach (var tuple in group)
                {
                    var source = Path.Combine(duplicateDirectoryName, getPeFileName(tuple.Id));
                    var destFileName = Path.Combine(group.Key, Path.GetFileName(tuple.FilePath.FullPath));
                    builder.AppendLine($@"New-Item -ItemType HardLink -Name {destFileName} -Value {source} -ErrorAction Stop | Out-Null");

                    count++;
                    if (count % 1_000 == 0)
                    {
                        builder.AppendLine($"Write-Host '{count:n0} hydrated'");
                    }
                }
            }

            File.WriteAllText(Path.Combine(destinationDirectory, "rehydrate.ps1"), builder.ToString());
        }
    }

    private static void CreateHardLink(string fileName, string existingFileName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var success = CreateHardLink(fileName, existingFileName, IntPtr.Zero);
            if (!success)
            {
                // for debugging: https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes
                throw new IOException($"Failed to create hard link from {fileName} to {existingFileName} with exception 0x{Marshal.GetLastWin32Error():X}");
            }
        }
        else
        {
            var result = link(existingFileName, fileName);
            if (result != 0)
            {
                throw new IOException($"Failed to create hard link from {existingFileName} to {fileName} with error code {Marshal.GetLastWin32Error()}");
            }
        }

        // https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createhardlinkw
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        // https://man7.org/linux/man-pages/man2/link.2.html
        [DllImport("libc", SetLastError = true)]
        static extern int link(string oldpath, string newpath);
    }

    private static bool TryGetMvid(string filePath, out Guid mvid)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var reader = new PEReader(stream);
            if (!reader.HasMetadata)
            {
                mvid = default;
                return false;
            }
            var metadataReader = reader.GetMetadataReader();
            var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
            mvid = metadataReader.GetGuid(mvidHandle);
            return true;
        }
        catch
        {
            mvid = default;
            return false;
        }
    }
}
