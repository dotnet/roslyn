// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioMetadataReferenceManager
    {
        private class MetadataCache
        {
            private const int InitialCapacity = 64;
            private const int CapacityMultiplier = 2;

            private readonly object _gate = new object();

            // value is ValueSource so that how metadata is re-acquired back are different per entry. 
            private readonly Dictionary<FileKey, ValueSource<AssemblyMetadata>> _metadataCache = new Dictionary<FileKey, ValueSource<AssemblyMetadata>>();

            private int _capacity = InitialCapacity;

            public bool TryGetMetadata(FileKey key, out AssemblyMetadata metadata)
            {
                lock (_gate)
                {
                    return TryGetMetadata_NoLock(key, out metadata);
                }
            }

            public bool TryGetSource(FileKey key, out ValueSource<AssemblyMetadata> source)
            {
                lock (_gate)
                {
                    return _metadataCache.TryGetValue(key, out source);
                }
            }

            private bool TryGetMetadata_NoLock(FileKey key, out AssemblyMetadata metadata)
            {
                if (_metadataCache.TryGetValue(key, out var metadataSource))
                {
                    metadata = metadataSource.GetValue();
                    return metadata != null;
                }

                metadata = null;
                return false;
            }

            public bool TryGetOrAddMetadata(FileKey key, ValueSource<AssemblyMetadata> newMetadata, out AssemblyMetadata metadata)
            {
                lock (_gate)
                {
                    if (TryGetMetadata_NoLock(key, out metadata))
                    {
                        return false;
                    }

                    EnsureCapacity_NoLock();

                    metadata = newMetadata.GetValue();
                    Contract.ThrowIfNull(metadata);

                    // don't use "Add" since key might already exist with already released metadata
                    _metadataCache[key] = newMetadata;
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
                foreach (var kv in _metadataCache)
                {
                    // metadata doesn't exist anymore. delete it from cache
                    if (!kv.Value.HasValue)
                    {
                        keysToRemove.Add(kv.Key);
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
