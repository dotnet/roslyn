// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

// Example:
// FullPath: "C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\8.0.8\analyzers\dotnet\System.Windows.Forms.Analyzers.dll"
// ProductVersion: "8.0.8"
// PathSuffix: "analyzers\dotnet"
using AnalyzerInfo = (string FullPath, string ProductVersion, string PathSuffix);

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem;

/// <summary>
/// See <see href="https://github.com/dotnet/sdk/blob/main/documentation/general/analyzer-redirecting.md"/>.
/// </summary>
[Export(typeof(IAnalyzerAssemblyRedirector))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SdkAnalyzerAssemblyRedirector(SVsServiceProvider serviceProvider) : SdkAnalyzerAssemblyRedirectorCore(
    GetInsertedAnalyzersDirectory(),
    (IVsActivityLog)serviceProvider.GetService(typeof(SVsActivityLog)))
{
    private static string? GetInsertedAnalyzersDirectory()
    {
        var enable = Environment.GetEnvironmentVariable("DOTNET_ANALYZER_REDIRECTING");
        if (!"0".Equals(enable, StringComparison.OrdinalIgnoreCase) && !"false".Equals(enable, StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"CommonExtensions\Microsoft\DotNet"));
        }

        return null;
    }
}

/// <summary>
/// Core functionality of <see cref="SdkAnalyzerAssemblyRedirector"/> extracted for testing.
/// </summary>
internal class SdkAnalyzerAssemblyRedirectorCore : IAnalyzerAssemblyRedirector
{
    private readonly IVsActivityLog? _log;

    /// <summary>
    /// Map from analyzer assembly name (file name without extension) to a list of matching analyzers.
    /// </summary>
    private readonly Lazy<ImmutableDictionary<string, List<AnalyzerInfo>>> _analyzerMap;

    public SdkAnalyzerAssemblyRedirectorCore(string? insertedAnalyzersDirectory, IVsActivityLog? log = null)
    {
        _log = log;
        _analyzerMap = new(() => CreateAnalyzerMap(insertedAnalyzersDirectory));
    }

    private ImmutableDictionary<string, List<AnalyzerInfo>> CreateAnalyzerMap(string? insertedAnalyzersDirectory)
    {
        if (insertedAnalyzersDirectory == null)
        {
            Log("Analyzer redirecting is disabled.");
            return ImmutableDictionary<string, List<AnalyzerInfo>>.Empty;
        }

        var metadataFilePath = Path.Combine(insertedAnalyzersDirectory, "metadata.json");
        if (!File.Exists(metadataFilePath))
        {
            Log($"File does not exist: {metadataFilePath}");
            return ImmutableDictionary<string, List<AnalyzerInfo>>.Empty;
        }

        Dictionary<string, string>? versions;

        try
        {
            versions = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(metadataFilePath));
        }
        catch (Exception ex)
        {
            Log($"Failed to read or parse metadata file '{metadataFilePath}'. {ex}", __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING);
            return ImmutableDictionary<string, List<AnalyzerInfo>>.Empty;
        }

        if (versions is null || versions.Count == 0)
        {
            Log($"Versions are empty: {metadataFilePath}", __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING);
            return ImmutableDictionary<string, List<AnalyzerInfo>>.Empty;
        }

        var builder = ImmutableDictionary.CreateBuilder<string, List<AnalyzerInfo>>(StringComparer.OrdinalIgnoreCase);

        // Expects layout like:
        // VsInstallDir\DotNetRuntimeAnalyzers\WindowsDesktopAnalyzers\analyzers\dotnet\System.Windows.Forms.Analyzers.dll
        //                                     ~~~~~~~~~~~~~~~~~~~~~~~                                                     = topLevelDirectory
        //                                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ = analyzerPath

        foreach (var topLevelDirectory in Directory.EnumerateDirectories(insertedAnalyzersDirectory))
        {
            var subsetName = Path.GetFileName(topLevelDirectory);

            foreach (var analyzerPath in Directory.EnumerateFiles(topLevelDirectory, "*.dll", SearchOption.AllDirectories))
            {
                if (!analyzerPath.StartsWith(topLevelDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    // We can't continue, because computations in code below assume this.
                    Debug.Fail($"Analyzer path '{analyzerPath}' must start with '{topLevelDirectory}'.");
                    continue;
                }

                if (!versions.TryGetValue(subsetName, out var version))
                {
                    continue;
                }

                var analyzerName = Path.GetFileNameWithoutExtension(analyzerPath);
                var pathSuffix = analyzerPath.Substring(topLevelDirectory.Length + 1 /* slash */);
                pathSuffix = Path.GetDirectoryName(pathSuffix);

                AnalyzerInfo analyzer = new() { FullPath = analyzerPath, ProductVersion = version, PathSuffix = pathSuffix };

                if (builder.TryGetValue(analyzerName, out var existing))
                {
                    existing.Add(analyzer);
                }
                else
                {
                    builder.Add(analyzerName, [analyzer]);
                }
            }
        }

        Log($"Loaded analyzer map ({builder.Count}): {insertedAnalyzersDirectory}");

        return builder.ToImmutable();
    }

    public string? RedirectPath(string fullPath)
    {
        if (_analyzerMap.Value.TryGetValue(Path.GetFileNameWithoutExtension(fullPath), out var analyzers))
        {
            foreach (var analyzer in analyzers)
            {
                var directoryPath = Path.GetDirectoryName(fullPath);

                // Note that both paths we compare here are normalized via netfx's Path.GetDirectoryName.
                if (directoryPath.EndsWith(analyzer.PathSuffix, StringComparison.OrdinalIgnoreCase) &&
                    MajorAndMinorVersionsMatch(directoryPath, analyzer.PathSuffix, analyzer.ProductVersion))
                {
                    return analyzer.FullPath;
                }
            }
        }

        return null;

        static bool MajorAndMinorVersionsMatch(string directoryPath, string pathSuffix, string version)
        {
            // Find the version number in the directory path - it is in the directory name before the path suffix.
            // Example:
            // "C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\8.0.8\analyzers\dotnet\" = directoryPath
            //                                                                       ~~~~~~~~~~~~~~~~   = pathSuffix
            //                                                                 ~~~~~                    = directoryPathVersion
            // This can match also a NuGet package because the version number is at the same position:
            // "C:\.nuget\packages\Microsoft.WindowsDesktop.App.Ref\8.0.8\analyzers\dotnet\"

            var index = directoryPath.LastIndexOf(pathSuffix, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            var directoryPathVersion = Path.GetFileName(Path.GetDirectoryName(directoryPath.Substring(0, index)));

            return AreVersionMajorMinorPartEqual(directoryPathVersion, version);
        }

        static bool AreVersionMajorMinorPartEqual(string version1, string version2)
        {
            var firstDotIndex = version1.IndexOf('.');
            if (firstDotIndex < 0)
            {
                return false;
            }

            var secondDotIndex = version1.IndexOf('.', firstDotIndex + 1);
            if (secondDotIndex < 0)
            {
                return false;
            }

            return 0 == string.Compare(version1, 0, version2, 0, secondDotIndex, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void Log(string message, __ACTIVITYLOG_ENTRYTYPE level = __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION)
    {
        _log?.LogEntry(
            (uint)level,
            "Roslyn" + nameof(SdkAnalyzerAssemblyRedirector),
            message);
    }
}
