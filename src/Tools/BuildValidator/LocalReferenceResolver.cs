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
        private readonly Dictionary<Guid, string> _cache = new Dictionary<Guid, string>();
        private readonly HashSet<DirectoryInfo> _indexDirectories = new HashSet<DirectoryInfo>();
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

        public string GetReferencePath(MetadataReferenceInfo referenceInfo)
        {
            if (_cache.TryGetValue(referenceInfo.Mvid, out var value))
            {
                return value;
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
                var file = GetReferencePath(reference);
                builder.Add(MetadataReference.CreateFromFile(
                    file,
                    new MetadataReferenceProperties(
                        kind: MetadataImageKind.Assembly,
                        aliases: reference.ExternAliases,
                        embedInteropTypes: reference.EmbedInteropTypes)));
            }

            results = builder.MoveToImmutable();
            return true;
        }

        public bool CacheNames(ImmutableArray<MetadataReferenceInfo> references)
        {
            if (references.All(r => _cache.ContainsKey(r.Mvid)))
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
                        if (reference.FileInfo.Name != file.Name)
                        {
                            continue;
                        }

                        if (GetMvidForFile(file) is not { } mvid)
                        {
                            _logger.LogWarning($@"Could not read MVID from ""{file.FullName}""");
                            continue;
                        }

                        if (_cache.ContainsKey(mvid))
                        {
                            continue;
                        }

                        if (mvid != reference.Mvid)
                        {
                            continue;
                        }

                        _logger.LogTrace($"Caching [{mvid}, {file.FullName}]");
                        _cache[mvid] = file.FullName;
                    }
                }
            }

            var uncached = references.Where(m => !_cache.ContainsKey(m.Mvid)).ToArray();
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

        private static Guid? GetMvidForFile(FileInfo fileInfo)
        {
            using (var stream = fileInfo.OpenRead())
            {
                PEReader reader = new PEReader(stream);

                if (reader.HasMetadata)
                {
                    var metadataReader = reader.GetMetadataReader();
                    var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                    return metadataReader.GetGuid(mvidHandle);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
