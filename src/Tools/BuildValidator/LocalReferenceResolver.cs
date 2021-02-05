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
            foreach (var directoryInfo in GetRefAssembliesDirectories())
            {
                _indexDirectories.Add(directoryInfo);
            }
            _indexDirectories.Add(new DirectoryInfo(options.AssembliesPath));
            _indexDirectories.Add(GetNugetCacheDirectory());
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

        public static DirectoryInfo[] GetRefAssembliesDirectories()
        {
            // PROTOTYPE
            return new[]
            {
                new DirectoryInfo(@"C:\Program Files\dotnet\packs\Microsoft.AspNetCore.App.Ref"),
                new DirectoryInfo(@"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref")
            };
        }

        public string GetReferencePath(MetadataReferenceInfo referenceInfo)
        {
            if (_cache.TryGetValue(referenceInfo.Mvid, out var value))
            {
                return value;
            }

            _logger.LogTrace($"Cache miss for reference {referenceInfo}");
            throw new Exception("PROTOTYPE");
        }

        public ImmutableArray<PortableExecutableReference> ResolveReferences(IEnumerable<MetadataReferenceInfo> references)
        {
            var referenceArray = references.ToImmutableArray();
            CacheNames(referenceArray);

            var files = referenceArray.Select(r => GetReferencePath(r));

            var metadataReferences = files.Select(f => MetadataReference.CreateFromFile(f)).ToImmutableArray();
            return metadataReferences;
        }

        public void CacheNames(ImmutableArray<MetadataReferenceInfo> names)
        {
            if (names.All(r => _cache.ContainsKey(r.Mvid)))
            {
                // All references have already been cached, no reason to look in the file system
                return;
            }

            foreach (var directory in _indexDirectories)
            {
                foreach (var file in directory.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    // A single file name can have multiple MVID, so compare by name first then
                    // open the files to check the MVID 
                    var potentialMatches = names.Where(m => FileNameEqualityComparer.Instance.Equals(m.FileInfo, file));

                    if (!potentialMatches.Any())
                    {
                        continue;
                    }

                    if (GetMvidForFile(file) is not { } mvid || _cache.ContainsKey(mvid))
                    {
                        continue;
                    }

                    var matchedReference = potentialMatches.FirstOrDefault(m => m.Mvid == mvid);
                    if (matchedReference.FileInfo is null)
                    {
                        continue;
                    }

                    _logger.LogTrace($"Caching [{mvid}, {file.FullName}]");
                    _cache[mvid] = file.FullName;
                }
            }

            var uncached = names.Where(m => !_cache.ContainsKey(m.Mvid)).ToArray();

            if (uncached.Any())
            {
                _logger.LogDebug($"Unable to find files for the following metadata references: {uncached}");
                // PROTOTYPE: should probably throw an exception here because we're guaranteed to fail to look up a filename for an assembly reference's MVID later on
            }
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
