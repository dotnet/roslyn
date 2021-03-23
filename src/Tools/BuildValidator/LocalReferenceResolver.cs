// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
        /// This maps a given file name to all of the <see cref="AssemblyInfo"/> that we ever considered 
        /// for that file name. It's useful for diagnostic purposes to see where we may have missed a
        /// reference lookup.
        /// </summary>
        private readonly Dictionary<string, List<AssemblyInfo>> _nameMap = new(FileNameEqualityComparer.StringComparer);
        private readonly HashSet<DirectoryInfo> _indexDirectories = new();
        private readonly ILogger _logger;

        public LocalReferenceResolver(Options options, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<LocalReferenceResolver>();
            foreach (var path in options.AssembliesPaths)
            {
                _indexDirectories.Add(new DirectoryInfo(path));
            }
            _indexDirectories.Add(GetNugetCacheDirectory());
            foreach (var path in options.ReferencesPaths)
            {
                _indexDirectories.Add(new DirectoryInfo(path));
            }

            using var _ = _logger.BeginScope("Assembly Reference Search Paths");
            foreach (var directory in _indexDirectories)
            {
                _logger.LogInformation($@"""{directory.FullName}""");
            }
        }

        public static DirectoryInfo GetNugetCacheDirectory()
        {
            var nugetPackageDirectory = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (nugetPackageDirectory is null)
            {
                nugetPackageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget");
            }

            return new DirectoryInfo(nugetPackageDirectory);
        }

        public IEnumerable<AssemblyInfo> GetCachedAssemblyInfos(string fileName) => _nameMap.TryGetValue(fileName, out var list)
            ? list
            : Array.Empty<AssemblyInfo>();

        public bool TryGetCachedAssemblyInfo(Guid mvid, [NotNullWhen(true)] out AssemblyInfo? assemblyInfo) => _mvidMap.TryGetValue(mvid, out assemblyInfo);

        public string GetCachedReferencePath(MetadataReferenceInfo referenceInfo)
        {
            if (_mvidMap.TryGetValue(referenceInfo.Mvid, out var value))
            {
                return value.FilePath;
            }

            throw new Exception($"Could not find referenced assembly {referenceInfo}");
        }

        public bool TryResolveReferences(ImmutableArray<MetadataReferenceInfo> references, out ImmutableArray<MetadataReference> results)
        {
            if (!CacheNames(references))
            {
                results = default;
                return false;
            }

            var builder = ImmutableArray.CreateBuilder<MetadataReference>(references.Length);
            foreach (var reference in references)
            {
                var filePath = GetCachedReferencePath(reference);
                using var fileStream = File.OpenRead(filePath);

                // This is deliberately using an ordinal comparison here. The name of the assembly is written out 
                // into the PDB. Rebuild will only succeed if the provided reference has the same name with the
                // same casing
                if (Path.GetFileName(filePath) != reference.Name)
                {
                    filePath = Path.Combine(Path.GetDirectoryName(filePath)!, reference.Name);
                }
                builder.Add(MetadataReference.CreateFromStream(
                    fileStream,
                    filePath: filePath,
                    properties: new MetadataReferenceProperties(
                        kind: MetadataImageKind.Assembly,
                        aliases: reference.ExternAliases,
                        embedInteropTypes: reference.EmbedInteropTypes)));
            }

            results = builder.MoveToImmutable();
            return true;
        }

        public bool CacheNames(ImmutableArray<MetadataReferenceInfo> references)
        {
            if (references.All(r => _mvidMap.ContainsKey(r.Mvid)))
            {
                // All references have already been cached, no reason to look in the file system
                return true;
            }

            foreach (var directory in _indexDirectories)
            {
                foreach (var file in directory.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    // A single file name can have multiple MVID, so compare by name first then
                    // open the files to check the MVID 
                    foreach (var reference in references)
                    {
                        if (!FileNameEqualityComparer.StringComparer.Equals(reference.FileInfo.Name, file.Name))
                        {
                            continue;
                        }

                        if (Util.GetPortableExecutableInfo(file.FullName) is not { } peInfo)
                        {
                            _logger.LogWarning($@"Could not read MVID from ""{file.FullName}""");
                            continue;
                        }

                        if (peInfo.IsReadyToRun)
                        {
                            _logger.LogInformation($@"Skipping ReadyToRun image ""{file.FullName}""");
                            continue;
                        }

                        var assemblyInfo = new AssemblyInfo(file.FullName, peInfo.Mvid);
                        if (!_nameMap.TryGetValue(assemblyInfo.FileName, out var list))
                        {
                            list = new List<AssemblyInfo>();
                            _nameMap[assemblyInfo.FileName] = list;
                        }
                        list.Add(assemblyInfo);

                        if (!_mvidMap.ContainsKey(peInfo.Mvid))
                        {
                            _logger.LogTrace($"Caching [{peInfo.Mvid}, {file.FullName}]");
                            _mvidMap[peInfo.Mvid] = assemblyInfo;
                        }
                    }
                }
            }

            var uncached = references.Where(m => !_mvidMap.ContainsKey(m.Mvid)).ToArray();
            if (uncached.Any())
            {
                using var _ = _logger.BeginScope($"Missing {uncached.Length} metadata references:");
                foreach (var missingReference in uncached)
                {
                    _logger.LogError($@"{missingReference.Name} - {missingReference.Mvid}");
                }
                return false;
            }

            return true;
        }
    }
}
