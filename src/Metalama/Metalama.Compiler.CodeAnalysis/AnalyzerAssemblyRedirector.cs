// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Utilities;

namespace Metalama.Compiler;

static class AnalyzerAssemblyRedirector
{
    private const string SdkAnalyzersResourceNamePrefix = "Metalama.Compiler.SdkAnalyzers.";
    private const string SdkAnalyzersResourceNameSuffix = ".zip";

    public static string? GetRedirectedPath(string originalPath, TempFileManager tempFileManager)
    {
        var thisAssembly = typeof(AnalyzerAssemblyRedirector).Assembly;
        var resourceNames = thisAssembly.GetManifestResourceNames();

        var sdkAnalyzersResourceName = resourceNames.FirstOrDefault(name => name.StartsWith(SdkAnalyzersResourceNamePrefix, StringComparison.Ordinal));

        if (sdkAnalyzersResourceName == null)
        {
            return null;
        }

        var sdkVersion = sdkAnalyzersResourceName[SdkAnalyzersResourceNamePrefix.Length..^SdkAnalyzersResourceNameSuffix.Length];

        var redirectionDirectory = tempFileManager.GetTempDirectory(directory: "SdkAnalyzers", subdirectory: sdkVersion, cleanUpStrategy: CleanUpStrategy.WhenUnused, versionNeutral: true);

        using (var stream = thisAssembly.GetManifestResourceStream(sdkAnalyzersResourceName))
        {
            ExtractSdkAssemblies(redirectionDirectory, stream!);
        }

        var fileName = Path.GetFileName(originalPath);

        var redirectedPath = Path.Combine(redirectionDirectory, fileName);

        if (File.Exists(redirectedPath))
        {
            return redirectedPath;
        }

        return null;
    }

    private static void ExtractSdkAssemblies(string directory, Stream assembliesArchiveStream)
    {
        var completedFilePath = Path.Combine(directory, ".completed");

        if (File.Exists(completedFilePath))
        {
            return;
        }

        using (MutexHelper.WithGlobalLock(directory))
        {
            if (File.Exists(completedFilePath))
            {
                return;
            }

            using (var archive = new ZipArchive(assembliesArchiveStream))
            {
                archive.ExtractToDirectory(directory);
            }

            File.WriteAllText(completedFilePath, "completed");
        }
    }
}
