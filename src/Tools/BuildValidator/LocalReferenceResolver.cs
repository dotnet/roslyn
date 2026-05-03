// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.Extensions.Logging;

namespace BuildValidator
{
    /// <summary>
    /// Resolves references for a package by looking in local nuget and artifact
    /// directories for Roslyn
    /// </summary>
    internal class LocalReferenceResolver
    {
        /// <summary>
        /// This maps MVID to the <see cref="AssemblyInfo"/> we are using for that particular MVID.
        /// </summary>
        private readonly Dictionary<Guid, AssemblyInfo> _mvidMap = new();

        /// <summary>
        /// Map file names to all of the paths it exists at. This map is depopulated as we realize
        /// the information from these file locations.
        /// </summary>
        private readonly Dictionary<string, List<string>> _nameToLocationsMap = new();

        /// <summary>
        /// This maps a given file name to all of the <see cref="AssemblyInfo"/> that we ever considered 
        /// for that file name. It's useful for diagnostic purposes to see where we may have missed a
        /// reference lookup.
        /// </summary>
        private readonly Dictionary<string, List<AssemblyInfo>> _nameMap = new(FileNameEqualityComparer.StringComparer);
        private readonly HashSet<DirectoryInfo> _indexDirectories = new();
        private readonly ILogger _logger;

        private LocalReferenceResolver(Dictionary<string, List<string>> nameToLocationsMap, ILogger logger)
        {
            _nameToLocationsMap = nameToLocationsMap;
            _logger = logger;
        }

        public static LocalReferenceResolver Create(Options options, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<LocalReferenceResolver>();
            var directories = new List<DirectoryInfo>();
            foreach (var path in options.AssembliesPaths)
            {
                directories.Add(new DirectoryInfo(path));
            }

            directories.Add(GetNugetCacheDirectory());
            foreach (var path in options.ReferencesPaths)
            {
                directories.Add(new DirectoryInfo(path));
            }

            using var _ = logger.BeginScope("Assembly Location Cache Population");
            var nameToLocationsMap = new Dictionary<string, List<string>>();
            foreach (var directory in directories)
            {
                logger.LogInformation($"Searching {directory.FullName}");
                var allFiles = directory
                    .EnumerateFiles("*.dll", SearchOption.AllDirectories)
                    .Concat(directory.EnumerateFiles("*.exe", SearchOption.AllDirectories));

                foreach (var fileInfo in allFiles)
                {
                    if (!nameToLocationsMap.TryGetValue(fileInfo.Name, out var locations))
                    {
                        locations = new();
                        nameToLocationsMap[fileInfo.Name] = locations;
                    }

                    locations.Add(fileInfo.FullName);
                }
            }

            return new LocalReferenceResolver(nameToLocationsMap, logger);
        }

        public static DirectoryInfo GetNugetCacheDirectory()
        {
            var nugetPackageDirectory = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            nugetPackageDirectory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget");

            return new DirectoryInfo(nugetPackageDirectory);
        }

        public IEnumerable<AssemblyInfo> GetCachedAssemblyInfos(string fileName) => _nameMap.TryGetValue(fileName, out var list)
            ? list
            : Array.Empty<AssemblyInfo>();

        public bool TryGetCachedAssemblyInfo(Guid mvid, [NotNullWhen(true)] out AssemblyInfo? assemblyInfo) => _mvidMap.TryGetValue(mvid, out assemblyInfo);

        public string GetCachedReferencePath(MetadataReferenceInfo referenceInfo)
        {
            if (_mvidMap.TryGetValue(referenceInfo.ModuleVersionId, out var value))
            {
                return value.FilePath;
            }

            throw new Exception($"Could not find referenced assembly {referenceInfo}");
        }

        public bool TryResolveReferences(MetadataReferenceInfo metadataReferenceInfo, [NotNullWhen(true)] out MetadataReference? metadataReference)
        {
            if (!TryGetAssemblyInfo(metadataReferenceInfo, out var assemblyInfo))
            {
                metadataReference = null;
                return false;
            }

            // This is deliberately using an ordinal comparison here. The name of the assembly is written out 
            // into the PDB. Rebuild will only succeed if the provided reference has the same name with the
            // same casing
            var filePath = assemblyInfo.FilePath;
            if (Path.GetFileName(filePath) != metadataReferenceInfo.FileName)
            {
                filePath = Path.Combine(Path.GetDirectoryName(filePath)!, metadataReferenceInfo.FileName);
            }

            metadataReference = MetadataReference.CreateFromStream(
                File.OpenRead(assemblyInfo.FilePath),
                filePath: filePath,
                properties: new MetadataReferenceProperties(
                    kind: MetadataImageKind.Assembly,
                    aliases: metadataReferenceInfo.ExternAlias is null ? ImmutableArray<string>.Empty : ImmutableArray.Create(metadataReferenceInfo.ExternAlias),
                    embedInteropTypes: metadataReferenceInfo.EmbedInteropTypes));
            return true;
        }

        public bool TryGetAssemblyInfo(MetadataReferenceInfo metadataReferenceInfo, [NotNullWhen(true)] out AssemblyInfo? assemblyInfo)
        {
            EnsureCachePopulated(metadataReferenceInfo.FileName);
            return _mvidMap.TryGetValue(metadataReferenceInfo.ModuleVersionId, out assemblyInfo);
        }

        private void EnsureCachePopulated(string fileName)
        {
            if (!_nameToLocationsMap.TryGetValue(fileName, out var locations))
            {
                return;
            }

            _nameToLocationsMap.Remove(fileName);

            using var _ = _logger.BeginScope($"Populating {fileName}");
            var assemblyInfoList = new List<AssemblyInfo>();
            foreach (var filePath in locations)
            {
                if (Util.GetPortableExecutableInfo(filePath) is not { } peInfo)
                {
                    _logger.LogWarning($@"Could not read MVID from ""{filePath}""");
                    continue;
                }

                if (peInfo.IsReadyToRun)
                {
                    _logger.LogInformation($@"Skipping ReadyToRun image ""{filePath}""");
                    continue;
                }

                var currentInfo = new AssemblyInfo(filePath, peInfo.Mvid);
                assemblyInfoList.Add(currentInfo);

                if (!_mvidMap.ContainsKey(peInfo.Mvid))
                {
                    _logger.LogTrace($"Caching [{peInfo.Mvid}, {filePath}]");
                    _mvidMap[peInfo.Mvid] = currentInfo;
                }
            }

            _nameMap[fileName] = assemblyInfoList;
        }
    }
}
