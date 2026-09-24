// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Microsoft.CodeAnalysis.LanguageServer only targets .NET, so its assets reader is only benchmarked there.
#if NET

#nullable disable

using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace IdeCoreBenchmarks.Lsp
{
    /// <summary>
    /// Compares the two ways of answering "did restore already resolve every PackageReference?" for a
    /// project.assets.json: reading it into NuGet's LockFile model versus streaming just the library keys.
    /// </summary>
    /// <remarks>
    /// Run with <c>--inProcess</c>. The default toolchain rebuilds the whole compiler graph for its generated
    /// project, which races itself on Microsoft.CodeAnalysis' intermediate output and fails to build.
    /// </remarks>
    [MemoryDiagnoser]
    public class ProjectAssetsReaderBenchmarks
    {
        private static readonly PackageReferenceItem[] s_packageReferences = new PackageReferenceItem[]
        {
            new("Microsoft.Extensions.FileSystemGlobbing", "[10.0.1]"),
            new("Microsoft.Extensions.Logging", "[10.0.1]"),
            new("Microsoft.ServiceHub.Framework", "[4.10.128]"),
            new("Microsoft.TestPlatform.ObjectModel", "[17.14.1]"),
            new("Microsoft.TestPlatform.TranslationLayer", "[17.14.1]"),
            new("Microsoft.VisualStudio.Composition", "[18.9.15]"),
            new("NuGet.ProjectModel", "[6.8.0-rc.112]"),
            new("SQLite3MC.PCLRaw.bundle", "[2.3.5]"),
            new("SQLitePCLRaw.core", "[3.0.2]"),
            new("System.CommandLine", "[3.0.0-preview.6.26324.102]"),
        };

        private string _projectAssetsPath;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            _projectAssetsPath = Path.Combine(
                roslynRoot,
                "src",
                "Tools",
                "IdeCoreBenchmarks",
                "Lsp",
                "ProjectAssetsReaderBenchmarkData.json");

            // Both paths must agree the sample is fully restored, otherwise they are not doing the same work.
            if (!CheckUpToDateWithLockFileModel() || !CheckUpToDateWithStreamingReader())
                throw new InvalidDataException("The benchmark project.assets.json does not resolve all package references.");
        }

        [Benchmark(Baseline = true)]
        public bool CheckUpToDateWithLockFileModel()
        {
            var lockFile = new LockFileFormat().Read(_projectAssetsPath);
            var projectAssetsMap = lockFile.Libraries
                .GroupBy(static library => library.Name, static library => library.Version, StringComparer.OrdinalIgnoreCase)
                .ToImmutableDictionary(
                    static group => group.Key,
                    static group => group.ToImmutableArray(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var reference in s_packageReferences)
            {
                if (!projectAssetsMap.TryGetValue(reference.Name, out var versions))
                    return false;

                var requestedVersionRange = VersionRange.TryParse(reference.VersionRange, out var versionRange)
                    ? versionRange
                    : VersionRange.All;
                if (!versions.Any(requestedVersionRange.Satisfies))
                    return false;
            }

            return true;
        }

        [Benchmark]
        public bool CheckUpToDateWithStreamingReader()
        {
            var resolvedReferences = ArrayPool<bool>.Shared.Rent(s_packageReferences.Length);
            Array.Clear(resolvedReferences, 0, s_packageReferences.Length);
            try
            {
                int? assetsFileVersion = null;
                ProjectAssetsReader.FindResolvedPackageReferences(
                    _projectAssetsPath,
                    s_packageReferences,
                    resolvedReferences.AsSpan(0, s_packageReferences.Length),
                    ref assetsFileVersion);

                for (var i = 0; i < s_packageReferences.Length; i++)
                {
                    if (!resolvedReferences[i])
                        return false;
                }

                return true;
            }
            finally
            {
                ArrayPool<bool>.Shared.Return(resolvedReferences);
            }
        }
    }
}

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace
{
    /// <summary>
    /// Stands in for the language server's model type, which is not reachable from this project.
    /// </summary>
    internal sealed class PackageReferenceItem
    {
        public PackageReferenceItem(string name, string versionRange)
        {
            Name = name;
            VersionRange = versionRange;
        }

        public string Name { get; }
        public string VersionRange { get; }
    }
}

#endif
