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
            if (_mvidMap.TryGetValue(metadataReferenceInfo.ModuleVersionId, out assemblyInfo))
            {
                return true;
            }

            if (_nameMap.TryGetValue(metadataReferenceInfo.FileName, out var _))
            {
                // The file name of this reference has already been searched for and none of them 
                // had the correct MVID (else the _mvidMap lookup would succeed). No reason to do 
                // more work here.
                return false;
            }

            var list = new List<AssemblyInfo>();

            foreach (var directory in _indexDirectories)
            {
                foreach (var fileInfo in directory.EnumerateFiles(metadataReferenceInfo.FileName, SearchOption.AllDirectories))
                {
                    if (Util.GetPortableExecutableInfo(fileInfo.FullName) is not { } peInfo)
                    {
                        _logger.LogWarning($@"Could not read MVID from ""{fileInfo.FullName}""");
                        continue;
                    }

                    if (peInfo.IsReadyToRun)
                    {
                        _logger.LogInformation($@"Skipping ReadyToRun image ""{fileInfo.FullName}""");
                        continue;
                    }

                    var currentInfo = new AssemblyInfo(fileInfo.FullName, peInfo.Mvid);
                    list.Add(currentInfo);

                    if (!_mvidMap.ContainsKey(peInfo.Mvid))
                    {
                        _logger.LogTrace($"Caching [{peInfo.Mvid}, {fileInfo.FullName}]");
                        _mvidMap[peInfo.Mvid] = currentInfo;
                    }
                }
            }

            _nameMap[metadataReferenceInfo.FileName] = list;
            return _mvidMap.TryGetValue(metadataReferenceInfo.ModuleVersionId, out assemblyInfo);
        }
    }
}
