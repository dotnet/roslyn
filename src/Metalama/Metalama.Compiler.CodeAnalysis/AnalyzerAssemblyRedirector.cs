// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System.IO;
using Roslyn.Utilities;

namespace Metalama.Compiler;

static class AnalyzerAssemblyRedirector
{
    private static readonly string? s_sdkAnalyzersDirectory = GetSdkAnalyzersDirectory();

    private static string? GetSdkAnalyzersDirectory()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(AnalyzerAssemblyRedirector).Assembly.Location);

        if (string.IsNullOrEmpty(assemblyDirectory))
        {
            return null;
        }

        // tasks/net6.0/bincore/Microsoft.CodeAnalysis.dll vs. tasks/net472/Microsoft.CodeAnalysis.dll
        var pathToRoot = assemblyDirectory.Contains("bincore") ? "../../.." : "../..";

        var sdkAnalyzersDirectory = Path.Combine(assemblyDirectory, pathToRoot, "sdkAnalyzers");

        sdkAnalyzersDirectory = FileUtilities.TryNormalizeAbsolutePath(sdkAnalyzersDirectory) ?? sdkAnalyzersDirectory;

        return Directory.Exists(sdkAnalyzersDirectory) ? sdkAnalyzersDirectory : null;
    }

    public static string? GetRedirectedPath(string originalPath)
    {
        if (s_sdkAnalyzersDirectory == null)
        {
            return null;
        }

        var fileName = Path.GetFileName(originalPath);

        var redirectedPath = Path.Combine(s_sdkAnalyzersDirectory, fileName);

        return File.Exists(redirectedPath) ? redirectedPath : null;
    }
}
