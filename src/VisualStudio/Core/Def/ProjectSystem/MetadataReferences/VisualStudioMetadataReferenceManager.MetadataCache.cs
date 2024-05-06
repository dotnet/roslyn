// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal sealed partial class VisualStudioMetadataReferenceManager
{
    private sealed class MetadataCache
    {
        private readonly object _gate = new();

        // value is ValueSource so that how metadata is re-acquired back are different per entry. 
        private readonly Dictionary<FileKey, AssemblyMetadata> _metadataCache = [];

        public bool TryGetMetadata(FileKey key, [NotNullWhen(true)] out AssemblyMetadata? metadata)
        {
            lock (_gate)
            {
                return TryGetMetadata_NoLock(key, out metadata);
            }
        }

        private bool TryGetMetadata_NoLock(FileKey key, [NotNullWhen(true)] out AssemblyMetadata? metadata)
            => _metadataCache.TryGetValue(key, out metadata) && metadata != null;

        /// <summary>
        /// <para>Gets specified metadata from the cache, or retrieves metadata from given <paramref
        /// name="newMetadata"/> and adds it to the cache if it's not there yet.</para>.  If the metadata is already in
        /// the cache then <paramref name="newMetadata"/> will be <see cref="IDisposable.Dispose"/>d, and the cached
        /// metadata will be returned.
        /// </summary>
        public AssemblyMetadata GetOrAddMetadata(FileKey key, AssemblyMetadata newMetadata)
        {
            lock (_gate)
            {
                if (TryGetMetadata_NoLock(key, out var cachedMetadata))
                {
                    // Another thread beat the calling thread.  Dispose the metadata that was just created and return
                    // the cached metadata.
                    newMetadata.Dispose();
                    return cachedMetadata;
                }

                // the source is expected to keep the metadata alive at this point
                Contract.ThrowIfNull(newMetadata);

                // don't use "Add" since key might already exist with already released metadata
                _metadataCache[key] = newMetadata;
                return newMetadata;
            }
        }
    }
}
