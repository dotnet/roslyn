#nullable enable 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
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

        InitialWalk();
        ResolveDuplicates();
        WriteHydrateFile();

        // The goal of initial walk is to
        //  1. Record any PE files as they are eligable for de-dup
        //  2. Hard link all other files into destination directory
        void InitialWalk()
        {
            foreach (var unitDirPath in Directory.EnumerateDirectories(sourceDirectory, "*.UnitTests"))
            {
                foreach (var sourceFilePath in Directory.EnumerateFiles(unitDirPath, "*", SearchOption.AllDirectories))
                {
                    var currentDirName = Path.GetDirectoryName(sourceFilePath);
                    var currentRelativeDirectory = Path.GetRelativePath(sourceDirectory, currentDirName!);
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
                        CreateHardLink(destFilePath, sourceFilePath, IntPtr.Zero);
                    }
                }
            }
        }

        // Now that we have a complete list of PE files, determine which are duplicates
        void ResolveDuplicates()
        {
            foreach (var pair in idToFilePathMap)
            {
                if (pair.Value.Count > 1)
                {
                    CreateHardLink(GetPeFilePath(pair.Key), pair.Value[0].FullPath, IntPtr.Zero);
                }
                else
                {
                    var item = pair.Value[0];
                    var destFilePath = Path.Combine(destinationDirectory, item.RelativePath);
                    CreateHardLink(destFilePath, item.FullPath, IntPtr.Zero);
                }
            }
        }

        string GetPeFileName(Guid mvid) => mvid.ToString();

        string GetPeFilePath(Guid mvid) => Path.Combine(duplicateDirectory, GetPeFileName(mvid));

        void WriteHydrateFile()
        {
            var fileList = new List<string>();
            var grouping = idToFilePathMap
                .Where(x => x.Value.Count > 1)
                .SelectMany(pair => pair.Value.Select(fp => (Id: pair.Key, FilePath: fp)))
                .GroupBy(fp => fp.FilePath.RelativeDirectory);
            var builder = new StringBuilder();
            builder.AppendLine("@echo off");
            var count = 0;
            foreach (var group in grouping)
            {
                foreach (var tuple in group)
                {
                    var source = Path.Combine(duplicateDirectoryName, GetPeFileName(tuple.Id));
                    var destFileName = Path.Combine(group.Key, Path.GetFileName(tuple.FilePath.FullPath));
                    builder.AppendLine($@"
mklink /h {destFileName} {source} > nul
if %errorlevel% neq 0 (
    echo %errorlevel%
    echo Cmd failed: mklink /h {destFileName} {source} > nul
    exit 1
)");

                    count++;
                    if (count % 1_000 == 0)
                    {
                        builder.AppendLine($"echo {count:n0} hydrated");
                    }
                }
            }

            File.WriteAllText(Path.Combine(destinationDirectory, ".hydrate.cmd"), builder.ToString());
        }
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

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
