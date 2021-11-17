// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionState
{
    private partial class SkeletonReferenceCache
    {
        private sealed class SkeletonReferenceSet
        {
            private const string s_peStreamKey = "PeStream";
            private const string s_peStreamKeyHasAllInformation = "PeStreamHasAllInformation";
            private const string s_xmlDocumentationStreamKey = "XmlDocumentationStream";
            private const string s_xmlDocumentationStreamKeyHasAllInformation = "XmlDocumentationStreamHasAllInformation";
            private const string s_assemblyNameKey = "AssemblyName";
            private const string s_assemblyNameKeyHasAllInformation = "AssemblyNameHasAllInformation";

            /// <summary>
            /// A map to ensure that the streams from the temporary storage service that back the metadata we create stay alive as long
            /// as the metadata is alive.
            /// </summary>
            private static readonly ConditionalWeakTable<AssemblyMetadata, ISupportDirectMemoryAccess> s_metadataToBackingMemoryMap = new();

            private readonly ITemporaryStreamStorage _peStreamStorage;
            private readonly ITemporaryStreamStorage _xmlDocumentationStreamStorage;
            private readonly string? _assemblyName;

            private readonly AssemblyMetadata _assemblyMetadata;
            private readonly XmlDocumentationProvider _xmlDocumentationProvider;

            /// <summary>
            /// Use WeakReference so we don't keep MetadataReference's alive if they are not being consumed. 
            /// Note: if the weak-reference is actually <see langword="null"/> (not that it points to null),
            /// that means we know we were unable to generate a reference for those properties, and future
            /// calls can early exit.
            /// </summary>
            /// <remarks>
            /// This instance should be locked when being read/written.
            /// </remarks>
            private readonly Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>?> _metadataReferences = new();

            public SkeletonReferenceSet(ITemporaryStreamStorage peStreamStorage, ITemporaryStreamStorage xmlDocumentationStreamStorage, string? assemblyName)
            {
                _peStreamStorage = peStreamStorage;
                _xmlDocumentationStreamStorage = xmlDocumentationStreamStorage;
                _assemblyName = assemblyName;

                var peStream = _peStreamStorage.ReadStream();
                if (peStream is ISupportDirectMemoryAccess supportDirectMemory)
                {
                    _assemblyMetadata = AssemblyMetadata.Create(ModuleMetadata.CreateFromImage(supportDirectMemory.GetPointer(), (int)peStream.Length));
                    s_metadataToBackingMemoryMap.Add(_assemblyMetadata, supportDirectMemory);
                }
                else
                {
                    _assemblyMetadata = AssemblyMetadata.CreateFromStream(peStream);
                }

                _xmlDocumentationProvider = XmlDocumentationProvider.CreateFromStream(_xmlDocumentationStreamStorage.ReadStream());
            }

            public MetadataReference? GetMetadataReference(MetadataReferenceProperties properties)
            {
                // lookup first and eagerly return cached value if we have it.
                lock (_metadataReferences)
                {
                    if (TryGetExisting_NoLock(properties, out var metadataReference))
                        return metadataReference;
                }

                // otherwise, create the metadata outside of the lock, and then try to assign it if no one else beat us
                {
                    var metadataReference = _assemblyMetadata.GetReference(
                        documentation: _xmlDocumentationProvider,
                        aliases: properties.Aliases,
                        embedInteropTypes: properties.EmbedInteropTypes,
                        display: _assemblyName);

                    var weakMetadata = metadataReference == null ? null : new WeakReference<MetadataReference>(metadataReference);

                    lock (_metadataReferences)
                    {
                        // see if someone beat us to writing this.
                        if (TryGetExisting_NoLock(properties, out var existingMetadataReference))
                            return existingMetadataReference;

                        _metadataReferences[properties] = weakMetadata;
                    }

                    return metadataReference;
                }

                bool TryGetExisting_NoLock(MetadataReferenceProperties properties, out MetadataReference? metadataReference)
                {
                    metadataReference = null;
                    if (!_metadataReferences.TryGetValue(properties, out var weakMetadata))
                        return false;

                    // If we are pointing at a null-weak reference (not a weak reference that points to null), then we 
                    // know we failed to create the metadata the last time around, and we can shortcircuit immediately,
                    // returning null *with* success to bubble that up.
                    if (weakMetadata == null)
                        return true;

                    return weakMetadata.TryGetTarget(out metadataReference);
                }
            }

            public async Task WriteToPersistentStorageAsync(
                SolutionState solution,
                ProjectState project,
                Checksum checksum,
                CancellationToken cancellationToken)
            {
                try
                {
                    var workspace = solution.Workspace;
                    var persistentStorageService = workspace.Services.GetPersistentStorageService(workspace.Options);

                    var solutionKey = SolutionKey.ToSolutionKey(solution);
                    var projectKey = ProjectKey.ToProjectKey(solutionKey, project);

                    var (peStreamKey, xmlDocumentationStreamKey, assemblyNameKey) = GetKeys(project);

                    var storage = await persistentStorageService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), cancellationToken).ConfigureAwait(false);
                    await using var _ = storage.ConfigureAwait(false);

                    {
                        using var peStream = await _peStreamStorage.ReadStreamAsync(cancellationToken).ConfigureAwait(false);
                        await storage.WriteStreamAsync(projectKey, peStreamKey, peStream, checksum, cancellationToken).ConfigureAwait(false);
                    }

                    {
                        using var xmlDocumentationStream = await _xmlDocumentationStreamStorage.ReadStreamAsync(cancellationToken).ConfigureAwait(false);
                        await storage.WriteStreamAsync(projectKey, xmlDocumentationStreamKey, xmlDocumentationStream, checksum, cancellationToken).ConfigureAwait(false);
                    }

                    {
                        using var assemblyNameStream = new MemoryStream();
                        using var objectWriter = new ObjectWriter(assemblyNameStream, leaveOpen: true, cancellationToken);

                        objectWriter.WriteString(_assemblyName);
                        assemblyNameStream.Position = 0;

                        await storage.WriteStreamAsync(projectKey, assemblyNameKey, assemblyNameStream, checksum, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
                {
                }
            }

            private static (string peStreamKey, string xmlDocumentationStreamKey, string assemblyNameKey) GetKeys(ProjectState project)
                => project.HasAllInformation
                    ? (s_peStreamKeyHasAllInformation, s_xmlDocumentationStreamKeyHasAllInformation, s_assemblyNameKeyHasAllInformation)
                    : (s_peStreamKey, s_xmlDocumentationStreamKey, s_assemblyNameKey);

            public static async Task<SkeletonReferenceSet?> TryReadFromPersistentStorageAsync(
                SolutionState solution,
                ProjectState project,
                Checksum checksum,
                CancellationToken cancellationToken)
            {
                try
                {
                    var workspace = solution.Workspace;
                    var persistentStorageService = workspace.Services.GetPersistentStorageService(workspace.Options);
                    var temporaryStorageService = workspace.Services.GetRequiredService<ITemporaryStorageService>();

                    var solutionKey = SolutionKey.ToSolutionKey(solution);
                    var projectKey = ProjectKey.ToProjectKey(solutionKey, project);
                    var (peStreamKey, xmlDocumentationStreamKey, assemblyNameKey) = GetKeys(project);

                    var storage = await persistentStorageService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), cancellationToken).ConfigureAwait(false);
                    await using var _ = storage.ConfigureAwait(false);

                    var peStreamStorage = temporaryStorageService.CreateTemporaryStreamStorage(cancellationToken);
                    var xmlDocumentationStreamStorage = temporaryStorageService.CreateTemporaryStreamStorage(cancellationToken);
                    string assemblyName;

                    {
                        using var peStream = await storage.ReadStreamAsync(projectKey, peStreamKey, checksum, cancellationToken).ConfigureAwait(false);
                        if (peStream == null)
                            return null;

                        await peStreamStorage.WriteStreamAsync(peStream, cancellationToken).ConfigureAwait(false);
                    }

                    {
                        using var xmlDocumentationStream = await storage.ReadStreamAsync(projectKey, xmlDocumentationStreamKey, checksum, cancellationToken).ConfigureAwait(false);
                        if (xmlDocumentationStream == null)
                            return null;

                        await xmlDocumentationStreamStorage.WriteStreamAsync(xmlDocumentationStream, cancellationToken).ConfigureAwait(false);
                    }

                    {
                        using var assemblyNameStream = await storage.ReadStreamAsync(projectKey, assemblyNameKey, checksum, cancellationToken).ConfigureAwait(false);
                        if (assemblyNameStream == null)
                            return null;

                        using var reader = ObjectReader.TryGetReader(assemblyNameStream, cancellationToken: cancellationToken);
                        if (reader == null)
                            return null;

                        assemblyName = reader.ReadString();
                    }

                    return new SkeletonReferenceSet(peStreamStorage, xmlDocumentationStreamStorage, assemblyName);
                }
                catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
                {
                    return null;
                }
            }
        }
    }
}
