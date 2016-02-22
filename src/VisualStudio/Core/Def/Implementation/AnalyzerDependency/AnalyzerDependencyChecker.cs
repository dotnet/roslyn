// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Diagnostics;
using SystemMetadataReader = System.Reflection.Metadata.MetadataReader;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class AnalyzerDependencyChecker
    {
        private readonly HashSet<string> _analyzerFilePaths;
        private readonly List<IIgnorableAssemblyList> _ignorableAssemblyLists;
        private readonly IBindingRedirectionService _bindingRedirectionService;

        public AnalyzerDependencyChecker(IEnumerable<string> analyzerFilePaths, IEnumerable<IIgnorableAssemblyList> ignorableAssemblyLists, IBindingRedirectionService bindingRedirectionService = null)
        {
            Debug.Assert(analyzerFilePaths != null);
            Debug.Assert(ignorableAssemblyLists != null);

            _analyzerFilePaths = new HashSet<string>(analyzerFilePaths, StringComparer.OrdinalIgnoreCase);
            _ignorableAssemblyLists = ignorableAssemblyLists.ToList();
            _bindingRedirectionService = bindingRedirectionService;
        }

        public AnalyzerDependencyResults Run(CancellationToken cancellationToken = default(CancellationToken))
        {
            List<AnalyzerInfo> analyzerInfos = new List<AnalyzerInfo>();

            foreach (var analyzerFilePath in _analyzerFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AnalyzerInfo info = TryReadAnalyzerInfo(analyzerFilePath);

                if (info != null)
                {
                    analyzerInfos.Add(info);
                }
            }

            _ignorableAssemblyLists.Add(new IgnorableAssemblyIdentityList(analyzerInfos.Select(info => info.Identity)));

            // First check for analyzers with the same identity but different
            // contents (that is, different MVIDs).

            ImmutableArray<AnalyzerDependencyConflict> conflicts = FindConflictingAnalyzers(analyzerInfos, cancellationToken);

            // Then check for missing references.

            ImmutableArray<MissingAnalyzerDependency> missingDependencies = FindMissingDependencies(analyzerInfos, cancellationToken);

            return new AnalyzerDependencyResults(conflicts, missingDependencies);
        }

        private ImmutableArray<MissingAnalyzerDependency> FindMissingDependencies(List<AnalyzerInfo> analyzerInfos, CancellationToken cancellationToken)
        {
            ImmutableArray<MissingAnalyzerDependency>.Builder builder = ImmutableArray.CreateBuilder<MissingAnalyzerDependency>();

            foreach (var analyzerInfo in analyzerInfos)
            {
                foreach (var reference in analyzerInfo.References)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var redirectedReference = _bindingRedirectionService != null
                        ? _bindingRedirectionService.ApplyBindingRedirects(reference)
                        : reference;

                    if (!_ignorableAssemblyLists.Any(ignorableAssemblyList => ignorableAssemblyList.Includes(redirectedReference)))
                    {
                        builder.Add(new MissingAnalyzerDependency(
                            analyzerInfo.Path,
                            reference));
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static ImmutableArray<AnalyzerDependencyConflict> FindConflictingAnalyzers(List<AnalyzerInfo> analyzerInfos, CancellationToken cancellationToken)
        {
            ImmutableArray<AnalyzerDependencyConflict>.Builder builder = ImmutableArray.CreateBuilder<AnalyzerDependencyConflict>();

            foreach (var identityGroup in analyzerInfos.GroupBy(di => di.Identity))
            {
                var identityGroupArray = identityGroup.ToImmutableArray();

                for (int i = 0; i < identityGroupArray.Length; i++)
                {
                    for (int j = i + 1; j < identityGroupArray.Length; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (identityGroupArray[i].MVID != identityGroupArray[j].MVID)
                        {
                            builder.Add(new AnalyzerDependencyConflict(
                                identityGroup.Key,
                                identityGroupArray[i].Path,
                                identityGroupArray[j].Path));
                        }
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static AnalyzerInfo TryReadAnalyzerInfo(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                using (var peReader = new PEReader(stream))
                {
                    var metadataReader = peReader.GetMetadataReader();

                    Guid mvid = ReadMvid(metadataReader);
                    AssemblyIdentity identity = ReadAssemblyIdentity(metadataReader);
                    ImmutableArray<AssemblyIdentity> references = ReadReferences(metadataReader);

                    return new AnalyzerInfo(filePath, identity, mvid, references);
                }
            }
            catch { }

            return null;
        }

        private static ImmutableArray<AssemblyIdentity> ReadReferences(SystemMetadataReader metadataReader)
        {
            var builder = ImmutableArray.CreateBuilder<AssemblyIdentity>();
            foreach (var referenceHandle in metadataReader.AssemblyReferences)
            {
                var reference = metadataReader.GetAssemblyReference(referenceHandle);

                string refname = metadataReader.GetString(reference.Name);
                Version refversion = reference.Version;
                string refcultureName = metadataReader.GetString(reference.Culture);
                ImmutableArray<byte> refpublicKeyOrToken = metadataReader.GetBlobContent(reference.PublicKeyOrToken);
                AssemblyFlags refflags = reference.Flags;
                bool refhasPublicKey = (refflags & AssemblyFlags.PublicKey) != 0;

                builder.Add(new AssemblyIdentity(refname, refversion, refcultureName, refpublicKeyOrToken, hasPublicKey: refhasPublicKey));
            }

            return builder.ToImmutable();
        }

        private static AssemblyIdentity ReadAssemblyIdentity(SystemMetadataReader metadataReader)
        {
            var assemblyDefinition = metadataReader.GetAssemblyDefinition();
            string name = metadataReader.GetString(assemblyDefinition.Name);
            Version version = assemblyDefinition.Version;
            string cultureName = metadataReader.GetString(assemblyDefinition.Culture);
            ImmutableArray<byte> publicKeyOrToken = metadataReader.GetBlobContent(assemblyDefinition.PublicKey);
            AssemblyFlags flags = assemblyDefinition.Flags;
            bool hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;

            return new AssemblyIdentity(name, version, cultureName, publicKeyOrToken, hasPublicKey: hasPublicKey);
        }

        private static Guid ReadMvid(SystemMetadataReader metadataReader)
        {
            var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
            return metadataReader.GetGuid(mvidHandle);
        }

        private sealed class AnalyzerInfo
        {
            public AnalyzerInfo(string filePath, AssemblyIdentity identity, Guid mvid, ImmutableArray<AssemblyIdentity> references)
            {
                Path = filePath;
                Identity = identity;
                MVID = mvid;
                References = references;
            }

            public string Path { get; }

            public AssemblyIdentity Identity { get; }

            public Guid MVID { get; }

            public ImmutableArray<AssemblyIdentity> References { get; }
        }
    }
}