﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// A cache of metadata blobs loaded into processes being debugged.
    /// Thread safe.
    /// </summary>
    internal sealed class DebuggeeModuleInfoCache
    {
        // Maps MVIDs to metadata blobs loaded to specific processes
        private Dictionary<Guid, DebuggeeModuleInfo> _lazyCache;

        /// <summary>
        /// May return null if the provider returns null.
        /// </summary>
        public DebuggeeModuleInfo GetOrAdd(Guid mvid, Func<Guid, DebuggeeModuleInfo> provider)
        {
            if (_lazyCache == null)
            {
                Interlocked.CompareExchange(ref _lazyCache, new Dictionary<Guid, DebuggeeModuleInfo>(), null);
            }

            var cache = _lazyCache;

            lock (cache)
            {
                if (cache.TryGetValue(mvid, out var existing))
                {
                    return existing;
                }
            }

            var newInfo = provider(mvid);
            if (newInfo == null)
            {
                return default;
            }

            lock (cache)
            {
                if (cache.TryGetValue(mvid, out var existing))
                {
                    newInfo.Dispose();
                    return existing;
                }

                cache.Add(mvid, newInfo);
                return newInfo;
            }
        }

        /// <summary>
        /// Removes metadata of specified module.
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
