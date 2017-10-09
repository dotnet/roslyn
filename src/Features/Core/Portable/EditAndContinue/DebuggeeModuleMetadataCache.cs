// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// A cache of metadata blobs loaded into processes being debugged.
    /// Thread safe.
    /// </summary>
    internal sealed class DebuggeeModuleMetadataCache
    {
        // Maps MVIDs to metadata blobs loaded to specific processes
        private Dictionary<Guid, ModuleMetadata> _lazyCache;

        /// <summary>
        /// May return null if the provider returns null.
        /// </summary>
        public ModuleMetadata GetOrAdd(Guid mvid, Func<Guid, ModuleMetadata> provider)
        {
            if (_lazyCache == null)
            {
                Interlocked.CompareExchange(ref _lazyCache, new Dictionary<Guid, ModuleMetadata>(), null);
            }

            var cache = _lazyCache;

            lock (cache)
            {
                if (cache.TryGetValue(mvid, out var existing))
                {
                    return existing;
                }
            }

            var newMetadata = provider(mvid);
            if (newMetadata == null)
            {
                return null;
            }

            lock (cache)
            {
                if (cache.TryGetValue(mvid, out var existing))
                {
                    newMetadata.Dispose();
                    return existing;
                }

                cache.Add(mvid, newMetadata);
                return newMetadata;
            }
        }

        /// <summary>
        /// Removes metadata of specified module and process.
        /// </summary>
        public bool Remove(Guid mvid)
        {
            Debug.Assert(mvid != default);

            var cache = _lazyCache;
            if (cache == null)
            {
                return false;
            }

            lock (cache)
            {
                if (cache.TryGetValue(mvid, out var entry))
                {
                    cache.Remove(mvid);
                    entry.Dispose();
                    return true;
                }
            }

            return false;
        }
    }
}
