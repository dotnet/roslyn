// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

internal static class MinimizeUtil
{
    internal record FilePathInfo(string RelativeDirectory, string Directory, string RelativePath, string FullPath);

    internal static void Run(string sourceDirectory, string destinationDirectory, bool isUnix)
    {
        const string duplicateDirectoryName = ".duplicate";
        var duplicateDirectory = Path.Combine(destinationDirectory, duplicateDirectoryName);
        Directory.CreateDirectory(duplicateDirectory);

        // https://github.com/dotnet/roslyn/issues/49486
        // we should avoid copying the files under Resources.
        Directory.CreateDirectory(Path.Combine(destinationDirectory, "src/Workspaces/MSBuildTest/Resources"));
        var individualFiles = new[]
        {
            "global.json",
            "NuGet.config",
            "src/Workspaces/MSBuildTest/Resources/global.json",
            "src/Workspaces/MSBuildTest/Resources/Directory.Build.props",
            "src/Workspaces/MSBuildTest/Resources/Directory.Build.targets",
            "src/Workspaces/MSBuildTest/Resources/Directory.Build.rsp",
            "src/Workspaces/MSBuildTest/Resources/NuGet.Config",
        };

        foreach (var individualFile in individualFiles)
        {
            var outputPath = Path.Combine(destinationDirectory, individualFile);
            var outputDirectory = Path.GetDirectoryName(outputPath)!;
            CreateHardLink(outputPath, Path.Combine(sourceDirectory, individualFile));
        }

        // Map of all PE files MVID to the path information
        var idToFilePathMap = initialWalk();
        resolveDuplicates();
        writeHydrateFile();

        // The goal of initial walk is to
        //  1. Record any PE files as they are eligable for de-dup
        //  2. Hard link all other files into destination directory
        Dictionary<Guid, List<FilePathInfo>> initialWalk()
        {
            IEnumerable<string> directories = new[] {
                Path.Combine(sourceDirectory, "eng"),
            };

            if (!isUnix)
            {
                directories = directories.Concat([Path.Combine(sourceDirectory, "artifacts", "VSSetup")]);
            }

            var artifactsDir = Path.Combine(sourceDirectory, "artifacts/bin");
            directories = directories.Concat(Directory.EnumerateDirectories(artifactsDir, "*.UnitTests"));
            directories = directories.Concat(Directory.EnumerateDirectories(artifactsDir, "*.IntegrationTests"));
            directories = directories.Concat(Directory.EnumerateDirectories(artifactsDir, "RunTests"));

            var idToFilePathMap = directories.AsParallel()
                .SelectMany(unitDirPath => walkDirectory(unitDirPath, sourceDirectory, destinationDirectory))
                .GroupBy(pair => pair.mvid)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(pair => pair.pathInfo).ToList());

            return idToFilePathMap;
        }

        static IEnumerable<(Guid mvid, FilePathInfo pathInfo)> walkDirectory(string unitDirPath, string sourceDirectory, string destinationDirectory)
        {
            Console.WriteLine($"[{DateTime.UtcNow}] Walking {unitDirPath}");
            string? lastOutputDirectory = null;
            foreach (var sourceFilePath in Directory.EnumerateFiles(unitDirPath, "*", SearchOption.AllDirectories))
            {
                var currentDirName = Path.GetDirectoryName(sourceFilePath)!;
                var currentRelativeDirectory = Path.GetRelativePath(sourceDirectory, currentDirName);
                var currentOutputDirectory = Path.Combine(destinationDirectory, currentRelativeDirectory);
                if (currentOutputDirectory != lastOutputDirectory)
                {
                    Directory.CreateDirectory(currentOutputDirectory);
                    lastOutputDirectory = currentOutputDirectory;
                }
                var fileName = Path.GetFileName(sourceFilePath);

                if (fileName.EndsWith(".dll", StringComparison.Ordinal) && TryGetMvid(sourceFilePath, out var mvid))
                {
                    var filePathInfo = new FilePathInfo(
                        RelativeDirectory: currentRelativeDirectory,
                        Directory: currentDirName,
                        RelativePath: Path.Combine(currentRelativeDirectory, fileName),
                        FullPath: sourceFilePath);
                    yield return (mvid, filePathInfo);
                }
                else
                {
                    var destFilePath = Path.Combine(currentOutputDirectory, fileName);
                    CreateHardLink(destFilePath, sourceFilePath);
                }
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

        static string getPeFileName(Guid mvid) => mvid.ToString();

        string getPeFilePath(Guid mvid) => Path.Combine(duplicateDirectory, getPeFileName(mvid));

        void writeHydrateFile()
        {
            var fileList = new List<string>();
            var grouping = idToFilePathMap
                .Where(x => x.Value.Count > 1)
                .SelectMany(pair => pair.Value.Select(fp => (Id: pair.Key, FilePath: fp)))
                .GroupBy(fp => getGroupDirectory(fp.FilePath.RelativeDirectory));

            // The "rehydrate-all" script assumes we are running all tests on a single machine instead of on Helix.
            var rehydrateAllBuilder = new StringBuilder();
            if (isUnix)
            {
                writeUnixHeaderContent(rehydrateAllBuilder);
                rehydrateAllBuilder.AppendLine("export HELIX_CORRELATION_PAYLOAD=$scriptroot/.duplicate");
            }
            else
            {
                rehydrateAllBuilder.AppendLine(@"set HELIX_CORRELATION_PAYLOAD=%~dp0\.duplicate");
            }

            var builder = new StringBuilder();
            var fileName = isUnix ? "rehydrate.sh" : "rehydrate.cmd";
            var rehydratedDirectories = new List<string>();
            foreach (var group in grouping)
            {
                builder.Clear();
                if (isUnix)
                {
                    writeUnixRehydrateContent(builder, group);
                    rehydrateAllBuilder.AppendLine(@"bash """ + Path.Combine("$scriptroot", group.Key, "rehydrate.sh") + @"""");
                }
                else
                {
                    writeWindowsRehydrateContent(builder, group);
                    rehydrateAllBuilder.AppendLine("call " + Path.Combine("%~dp0", group.Key, "rehydrate.cmd"));
                }

                File.WriteAllText(Path.Combine(destinationDirectory, group.Key, fileName), builder.ToString());
                rehydratedDirectories.Add(group.Key);
            }

            // Even if we didn't have any duplicates, write out a file since later scripts rely on its existence.
            var noDuplicatesGrouping = idToFilePathMap.Values
                .SelectMany(v => v)
                .GroupBy(v => getGroupDirectory(v.RelativeDirectory));
            foreach (var noDuplicate in noDuplicatesGrouping)
            {
                if (!rehydratedDirectories.Contains(noDuplicate.Key))
                {
                    var file = Path.Combine(destinationDirectory, noDuplicate.Key, fileName);
                    Contract.Assert(!File.Exists(file));
                    File.WriteAllText(file, "echo \"Nothing to rehydrate\"");
                }
            }

            string rehydrateAllFilename = isUnix ? "rehydrate-all.sh" : "rehydrate-all.cmd";
            File.WriteAllText(Path.Combine(destinationDirectory, rehydrateAllFilename), rehydrateAllBuilder.ToString());

            static void writeWindowsRehydrateContent(StringBuilder builder, IGrouping<string, (Guid Id, FilePathInfo FilePath)> group)
            {
                builder.AppendLine("@echo off");
                var count = 0;
                foreach (var tuple in group)
                {
                    var source = getPeFileName(tuple.Id);
                    var destFileName = Path.GetRelativePath(group.Key, tuple.FilePath.RelativePath);
                    if (Path.GetDirectoryName(destFileName) is { Length: not 0 } directory)
                    {
                        builder.AppendLine($@"mkdir %~dp0\{directory} 2> nul");
                    }
                    builder.AppendLine($@"
mklink /h %~dp0\{destFileName} %HELIX_CORRELATION_PAYLOAD%\{source} > nul
if %errorlevel% neq 0 (
    echo Cmd failed: mklink /h %~dp0\{destFileName} %HELIX_CORRELATION_PAYLOAD%\{source}
    exit /b 1
)");
                    count++;
                    if (count % 1_000 == 0)
                    {
                        builder.AppendLine($"echo {count:n0} hydrated");
                    }
                }
                builder.AppendLine("@echo on"); // so the rest of the commands show up in helix logs
            }

            static void writeUnixHeaderContent(StringBuilder builder)
            {
                builder.AppendLine(@"#!/bin/bash

source=""${BASH_SOURCE[0]}""

# resolve $source until the file is no longer a symlink
while [[ -h ""$source"" ]]; do
scriptroot=""$( cd -P ""$( dirname ""$source"" )"" && pwd )""
source=""$(readlink ""$source"")""
# if $source was a relative symlink, we need to resolve it relative to the path where the
# symlink file was located
[[ $source != /* ]] && source=""$scriptroot/$source""
done
scriptroot=""$( cd -P ""$( dirname ""$source"" )"" && pwd )""
");
            }

            static void writeUnixRehydrateContent(StringBuilder builder, IGrouping<string, (Guid Id, FilePathInfo FilePath)> group)
            {
                writeUnixHeaderContent(builder);

                var count = 0;
                foreach (var tuple in group)
                {
                    var source = getPeFileName(tuple.Id);
                    var destFilePath = Path.GetRelativePath(group.Key, tuple.FilePath.RelativePath);
                    if (Path.GetDirectoryName(destFilePath) is { Length: not 0 } directory)
                    {
                        builder.AppendLine($@"mkdir -p ""$scriptroot/{directory}""");
                    }
                    builder.AppendLine($@"ln ""$HELIX_CORRELATION_PAYLOAD/{source}"" ""$scriptroot/{destFilePath}"" || exit $?");

                    count++;
                    if (count % 1_000 == 0)
                    {
                        builder.AppendLine($"echo '{count:n0} hydrated'");
                    }
                }

                // Working around an AzDo file permissions bug.
                // We want this to happen at the end so we can be agnostic about whether ilasm was already in the directory, or was linked in from the .duplicate directory.
                builder.AppendLine();
                builder.AppendLine(@"find $scriptroot -name ilasm -exec chmod 755 {} +");
            }

            static string getGroupDirectory(string relativePath)
            {
                // artifacts/TestProject/Debug/net472/whatever/etc should become:
                // artifacts/TestProject/Debug/net472

                var groupDirectory = relativePath;
                while (Path.GetFileName(Path.GetDirectoryName(groupDirectory)) is not (null or "Debug" or "Release"))
                    groupDirectory = Path.GetDirectoryName(groupDirectory);

                if (groupDirectory is null)
                {
                    // So far, this scenario doesn't seem to happen.
                    // If it *did* happen, we'd want to know, but it isn't necessarily a problem.
                    Console.WriteLine("Directory not grouped under configuration/TFM: " + relativePath);
                    return relativePath;
                }

                return groupDirectory;
            }
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
                throw new IOException($"Failed to create hard link from {existingFileName} to {fileName} with exception 0x{Marshal.GetLastWin32Error():X}");
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
