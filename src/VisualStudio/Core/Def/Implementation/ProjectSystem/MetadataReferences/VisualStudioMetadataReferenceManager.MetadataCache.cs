// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioMetadataReferenceManager
    {
        private sealed class MetadataCache
        {
            private const int InitialCapacity = 64;
            private const int CapacityMultiplier = 2;

            private readonly object _gate = new();

            // value is ValueSource so that how metadata is re-acquired back are different per entry. 
            private readonly Dictionary<FileKey, ValueSource<Optional<AssemblyMetadata>>> _metadataCache = new();

            private int _capacity = InitialCapacity;

            public bool TryGetMetadata(FileKey key, [NotNullWhen(true)] out AssemblyMetadata? metadata)
            {
                lock (_gate)
                {
                    return TryGetMetadata_NoLock(key, out metadata);
                }
            }

            public bool TryGetSource(FileKey key, [NotNullWhen(true)] out ValueSource<Optional<AssemblyMetadata>>? source)
            {
                lock (_gate)
                {
                    return _metadataCache.TryGetValue(key, out source);
                }
            }

            private bool TryGetMetadata_NoLock(FileKey key, [NotNullWhen(true)] out AssemblyMetadata? metadata)
            {
                if (_metadataCache.TryGetValue(key, out var metadataSource))
                {
                    metadata = metadataSource.GetValueOrNull();
                    return metadata != null;
                }

                metadata = null;
                return false;
            }

            /// <summary>
            /// <para>Gets specified metadata from the cache, or retrieves metadata from given <paramref name="metadataSource"/>
            /// and adds it to the cache if it's not there yet.</para>
            /// 
            /// <para><paramref name="metadataSource"/> is expected to to provide metadata at least until this method returns.</para>
            /// </summary>
            /// <returns>
            /// True if the metadata is retrieved from <paramref name="metadataSource"/> source, false if it already exists in the cache.
            /// </returns>
            public bool GetOrAddMetadata(FileKey key, ValueSource<Optional<AssemblyMetadata>> metadataSource, out AssemblyMetadata metadata)
            {
                lock (_gate)
                {
                    if (TryGetMetadata_NoLock(key, out var cachedMetadata))
                    {
                        metadata = cachedMetadata;
                        return false;
                    }

                    EnsureCapacity_NoLock();

                    var newMetadata = metadataSource.GetValueOrNull();

                    // the source is expected to keep the metadata alive at this point
                    Contract.ThrowIfNull(newMetadata);

                    // don't use "Add" since key might already exist with already released metadata
                    _metadataCache[key] = metadataSource;
                    metadata = newMetadata;
                    return true;
                }
            }

            private void EnsureCapacity_NoLock()
            {
                if (_metadataCache.Count < _capacity)
                {
                    return;
                }

                using var pooledObject = SharedPools.Default<List<FileKey>>().GetPooledObject();
                var keysToRemove = pooledObject.Object;
                foreach (var (fileKey, metadataSource) in _metadataCache)
                {
                    // metadata doesn't exist anymore. delete it from cache
                    if (!metadataSource.TryGetValue(out _))
                    {
                        keysToRemove.Add(fileKey);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _metadataCache.Remove(key);
                }

                // cache is too small, increase it
                if (_metadataCache.Count >= _capacity)
                {
                    _capacity *= CapacityMultiplier;
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
}
