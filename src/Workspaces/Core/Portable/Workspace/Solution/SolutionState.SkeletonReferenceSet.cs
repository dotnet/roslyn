// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionState
{
    private sealed class SkeletonReferenceSet
    {
        private readonly ITemporaryStreamStorage _storage;
        private readonly string? _assemblyName;

        /// <summary>
        /// The documentation provider used to lookup xml docs for any metadata reference we pass out.  See
        /// docs on <see cref="DeferredDocumentationProvider"/> for why this is safe to hold onto despite it
        /// rooting a compilation internally.
        /// </summary>
        private readonly DeferredDocumentationProvider _documentationProvider;

        /// <summary>
        /// The actual assembly metadata produced from the data pointed to in <see cref="_storage"/>.
        /// </summary>
        private readonly AsyncLazy<(AssemblyMetadata metadata, ISupportDirectMemoryAccess? directMemoryAccess)> _metadataAndDirectMemoryAccess;

        /// <summary>
        /// Lock this object while reading/writing from it.
        /// </summary>
        private readonly Dictionary<(AssemblyMetadata, ISupportDirectMemoryAccess?, MetadataReferenceProperties), SkeletonPortableExecutableReference> _referenceMap = new();

        public SkeletonReferenceSet(
            ITemporaryStreamStorage storage,
            string? assemblyName,
            DeferredDocumentationProvider documentationProvider)
        {
            _storage = storage;
            _assemblyName = assemblyName;
            _documentationProvider = documentationProvider;

            // note: computing the assembly metadata is actually synchronous.  However, this ensures we don't have N
            // threads blocking on a lazy to compute the work.  Instead, we'll only occupy one thread, while any
            // concurrent requests asynchronously wait for that work to be done.
            _metadataAndDirectMemoryAccess = new AsyncLazy<(AssemblyMetadata, ISupportDirectMemoryAccess?)>(
                c => Task.FromResult(ComputeMetadata(_storage, c)), cacheResult: true);
        }

        private static (AssemblyMetadata, ISupportDirectMemoryAccess?) ComputeMetadata(ITemporaryStreamStorage storage, CancellationToken cancellationToken)
        {
            // first see whether we can use native memory directly.
            var stream = storage.ReadStream(cancellationToken);

            if (stream is ISupportDirectMemoryAccess supportNativeMemory)
            {
                // this is unfortunate that if we give stream, compiler will just re-copy whole content to 
                // native memory again. this is a way to get around the issue by we getting native memory ourselves and then
                // give them pointer to the native memory. also we need to handle lifetime ourselves.
                var metadata = AssemblyMetadata.Create(ModuleMetadata.CreateFromImage(supportNativeMemory.GetPointer(), (int)stream.Length));

                return (metadata, supportNativeMemory);
            }
            else
            {
                // Otherwise, we just let it use stream. Unfortunately, if we give stream, compiler will
                // internally copy it to native memory again. since compiler owns lifetime of stream,
                // it would be great if compiler can be little bit smarter on how it deals with stream.

                // We don't deterministically release the resulting metadata since we don't know 
                // when we should. So we leave it up to the GC to collect it and release all the associated resources.
                return (AssemblyMetadata.CreateFromStream(stream, leaveOpen: false), null);
            }
        }

        public MetadataReference? TryGetAlreadyBuiltMetadataReference(MetadataReferenceProperties properties)
        {
            _metadataAndDirectMemoryAccess.TryGetValue(out var tuple);
            return CreateMetadataReference(properties, tuple.metadata, tuple.directMemoryAccess);
        }

        public async Task<MetadataReference?> GetMetadataReferenceAsync(MetadataReferenceProperties properties, CancellationToken cancellationToken)
        {
            var (metadata, directMemberAccess) = await _metadataAndDirectMemoryAccess.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return CreateMetadataReference(properties, metadata, directMemberAccess);
        }

        private SkeletonPortableExecutableReference? CreateMetadataReference(
            MetadataReferenceProperties properties, AssemblyMetadata? metadata, ISupportDirectMemoryAccess? directMemoryAccess)
        {
            if (metadata == null)
                return null;

            var key = (metadata, directMemoryAccess, properties);
            lock (_referenceMap)
            {
                if (!_referenceMap.TryGetValue(key, out var value))
                {
                    value = new SkeletonPortableExecutableReference(
                        metadata,
                        properties,
                        _documentationProvider,
                        _assemblyName,
                        directMemoryAccess);
                    _referenceMap.Add(key, value);
                }

                return value;
            }
        }
    }
}
