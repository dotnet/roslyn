// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of root checksum tree node
    /// </summary>
    internal partial class ChecksumTreeCollection
    {
        /// <summary>
        /// map green to checksum object cache
        /// 
        /// we have this to reduce allocation of more expansive concurrent dictionary when not necessary.
        /// many times, green node cache in checksum tree node contains only small number of items. this should
        /// let us avoid creating concurrent dictionary in common small number of items case.
        /// </summary>
        private class GreenNodeChecksumCache
        {
            private static readonly Func<object, ChecksumObjectCache> s_createCheskumObjectCache = _ => new ChecksumObjectCache();

            private Tuple<object, ChecksumObjectCache> _greenNode1;
            private Tuple<object, ChecksumObjectCache> _greenNode2;
            private Tuple<object, ChecksumObjectCache> _greenNode3;

            private ConcurrentDictionary<object, ChecksumObjectCache> _lazyGreenNodeToChecksumObjectCache;

            public GreenNodeChecksumCache()
            {
            }

            private Tuple<object, ChecksumObjectCache> GreenNode1 => Volatile.Read(ref _greenNode1);
            private Tuple<object, ChecksumObjectCache> GreenNode2 => Volatile.Read(ref _greenNode2);
            private Tuple<object, ChecksumObjectCache> GreenNode3 => Volatile.Read(ref _greenNode3);

            private ConcurrentDictionary<object, ChecksumObjectCache> LazyGreenNodeToChecksumObjectCache => Volatile.Read(ref _lazyGreenNodeToChecksumObjectCache);

            public ChecksumObjectCache GetOrAdd(object key)
            {
                // optimization to not create map. in whole solution level, there is
                // many case where it contains only 3 item.
                if (Match(ref _greenNode1, key))
                {
                    return _greenNode1.Item2;
                }

                if (Match(ref _greenNode2, key))
                {
                    return _greenNode2.Item2;
                }

                if (Match(ref _greenNode3, key))
                {
                    return _greenNode3.Item2;
                }

                // more expansive case. create map to save checksum object per kind
                EnsureLazyMap();

                return _lazyGreenNodeToChecksumObjectCache.GetOrAdd(key, s_createCheskumObjectCache);
            }

            private bool Match(ref Tuple<object, ChecksumObjectCache> greenNode, object key)
            {
                var node = Volatile.Read(ref greenNode);
                if (node == null)
                {
                    Interlocked.Exchange(ref greenNode, Tuple.Create(key, new ChecksumObjectCache()));
                }

                return greenNode.Item1 == key;
            }

            public bool TryGetValue(object key, out ChecksumObjectCache cache)
            {
                if (GreenNode1?.Item1 == key)
                {
                    cache = GreenNode1.Item2;
                    return true;
                }

                if (GreenNode2?.Item1 == key)
                {
                    cache = GreenNode2.Item2;
                    return true;
                }

                if (GreenNode3?.Item1 == key)
                {
                    cache = GreenNode3.Item2;
                    return true;
                }

                if (LazyGreenNodeToChecksumObjectCache != null)
                {
                    return LazyGreenNodeToChecksumObjectCache.TryGetValue(key, out cache);
                }

                cache = null;
                return false;
            }

            public IEnumerable<ChecksumObjectCache> Caches
            {
                get
                {
                    if (GreenNode1 != null)
                    {
                        yield return GreenNode1.Item2;
                    }

                    if (GreenNode2 != null)
                    {
                        yield return GreenNode2.Item2;
                    }

                    if (GreenNode3 != null)
                    {
                        yield return GreenNode3.Item2;
                    }

                    var caches = LazyGreenNodeToChecksumObjectCache;
                    if (caches != null)
                    {
                        foreach (var v in caches.Values)
                        {
                            yield return v;
                        }
                    }
                }
            }

            private void EnsureLazyMap()
            {
                if (_lazyGreenNodeToChecksumObjectCache == null)
                {
                    // we have multiple entries. create lazy map
                    Interlocked.CompareExchange(ref _lazyGreenNodeToChecksumObjectCache, new ConcurrentDictionary<object, ChecksumObjectCache>(concurrencyLevel: 2, capacity: 3), null);
                }
            }
        }
    }
}
