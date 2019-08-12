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
    internal static class AnalyzerDependencyChecker
    {
        public static AnalyzerDependencyResults ComputeDependencyConflicts(IEnumerable<string> analyzerFilePaths, IEnumerable<IIgnorableAssemblyList> ignorableAssemblyLists, IBindingRedirectionService bindingRedirectionService = null, CancellationToken cancellationToken = default)
        {
            var analyzerInfos = new List<AnalyzerInfo>();

            foreach (var analyzerFilePath in analyzerFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var info = TryReadAnalyzerInfo(analyzerFilePath);

                if (info != null)
                {
                    analyzerInfos.Add(info);
                }
            }

            var allIgnorableAssemblyLists = new List<IIgnorableAssemblyList>(ignorableAssemblyLists);
            allIgnorableAssemblyLists.Add(new IgnorableAssemblyIdentityList(analyzerInfos.Select(info => info.Identity)));

            // First check for analyzers with the same identity but different
            // contents (that is, different MVIDs).

            var conflicts = FindConflictingAnalyzers(analyzerInfos, cancellationToken);

            // Then check for missing references.

            var missingDependencies = FindMissingDependencies(analyzerInfos, allIgnorableAssemblyLists, bindingRedirectionService, cancellationToken);

            return new AnalyzerDependencyResults(conflicts, missingDependencies);
        }

        private static ImmutableArray<MissingAnalyzerDependency> FindMissingDependencies(List<AnalyzerInfo> analyzerInfos, List<IIgnorableAssemblyList> ignorableAssemblyLists, IBindingRedirectionService bindingRedirectionService, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<MissingAnalyzerDependency>();

            foreach (var analyzerInfo in analyzerInfos)
            {
                foreach (var reference in analyzerInfo.References)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var redirectedReference = bindingRedirectionService != null
                        ? bindingRedirectionService.ApplyBindingRedirects(reference)
                        : reference;

                    if (!ignorableAssemblyLists.Any(ignorableAssemblyList => ignorableAssemblyList.Includes(redirectedReference)))
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
            var builder = ImmutableArray.CreateBuilder<AnalyzerDependencyConflict>();

            foreach (var identityGroup in analyzerInfos.GroupBy(di => di.Identity))
            {
                var identityGroupArray = identityGroup.ToImmutableArray();

                for (var i = 0; i < identityGroupArray.Length; i++)
                {
                    for (var j = i + 1; j < identityGroupArray.Length; j++)
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

                    var mvid = ReadMvid(metadataReader);
                    var identity = ReadAssemblyIdentity(metadataReader);
                    var references = ReadReferences(metadataReader);

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

                var refname = metadataReader.GetString(reference.Name);
                var refversion = reference.Version;
                var refcultureName = metadataReader.GetString(reference.Culture);
                var refpublicKeyOrToken = metadataReader.GetBlobContent(reference.PublicKeyOrToken);
                var refflags = reference.Flags;
                var refhasPublicKey = (refflags & AssemblyFlags.PublicKey) != 0;

                builder.Add(new AssemblyIdentity(refname, refversion, refcultureName, refpublicKeyOrToken, hasPublicKey: refhasPublicKey));
            }

            return builder.ToImmutable();
        }

        private static AssemblyIdentity ReadAssemblyIdentity(SystemMetadataReader metadataReader)
        {
            var assemblyDefinition = metadataReader.GetAssemblyDefinition();
            var name = metadataReader.GetString(assemblyDefinition.Name);
            var version = assemblyDefinition.Version;
            var cultureName = metadataReader.GetString(assemblyDefinition.Culture);
            var publicKeyOrToken = metadataReader.GetBlobContent(assemblyDefinition.PublicKey);
            var flags = assemblyDefinition.Flags;
            var hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;

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
