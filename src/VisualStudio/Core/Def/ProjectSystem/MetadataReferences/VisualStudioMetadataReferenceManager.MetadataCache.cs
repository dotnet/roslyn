// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// <para>Gets specified metadata from the cache, or retrieves metadata from given <paramref name="newMetadata"/>
        /// and adds it to the cache if it's not there yet.</para>
        /// </summary>
        /// <returns>
        /// True if the metadata is retrieved from <paramref name="newMetadata"/> source, false if it already exists in the cache.
        /// </returns>
        public bool GetOrAddMetadata(FileKey key, AssemblyMetadata newMetadata, out AssemblyMetadata metadata)
        {
            lock (_gate)
            {
                if (TryGetMetadata_NoLock(key, out var cachedMetadata))
                {
                    metadata = cachedMetadata;
                    return false;
                }

                // the source is expected to keep the metadata alive at this point
                Contract.ThrowIfNull(newMetadata);

                // don't use "Add" since key might already exist with already released metadata
                _metadataCache[key] = newMetadata;
                metadata = newMetadata;
                return true;
            }
        }

        public void ClearCache()
        {
            lock (_gate)
            {
                _metadataCache.Clear();
            }
        }
    }
}
