// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is collection of root checksum tree node
    /// </summary>
    internal partial class ChecksumTreeCollection
    {
        /// <summary>
        /// map kind to actual checksum object collection
        /// 
        /// since our hierarchical checksum tree should only hold onto green node. 
        /// different kind of checksum object might share same green node such as document state
        /// for SourceText and DocumentInfo. so we have this cache type
        /// </summary>
        private class ChecksumObjectCache
        {
            private ChecksumObject _checksumObject1;
            private ChecksumObject _checksumObject2;

            private SubTreeNode _lazyChecksumTree;

            private ConcurrentDictionary<string, ChecksumObject> _lazyKindToChecksumObjectMap;
            private ConcurrentDictionary<Checksum, ChecksumObject> _lazyChecksumToChecksumObjectMap;

            public ChecksumObjectCache()
            {
            }

            private ChecksumObject ChecksumObject1 => Volatile.Read(ref _checksumObject1);
            private ChecksumObject ChecksumObject2 => Volatile.Read(ref _checksumObject2);

            private SubTreeNode LazyChecksumTree => Volatile.Read(ref _lazyChecksumTree);

            private ConcurrentDictionary<string, ChecksumObject> LazyKindToChecksumObjectMap => Volatile.Read(ref _lazyKindToChecksumObjectMap);
            private ConcurrentDictionary<Checksum, ChecksumObject> LazyChecksumToChecksumObjectMap => Volatile.Read(ref _lazyChecksumToChecksumObjectMap);

            public ChecksumObject Add(ChecksumObject checksumObject)
            {
                // optimization to not create map. in whole solution level, there is
                // many case where green node is unique per checksum object such as metadata reference
                // or p2p reference.
                Interlocked.CompareExchange(ref _checksumObject1, checksumObject, null);
                if (_checksumObject1.Kind == checksumObject.Kind)
                {
                    // we already have one
                    Contract.Requires(_checksumObject1.Checksum.Equals(checksumObject.Checksum));
                    return _checksumObject1;
                }

                Interlocked.CompareExchange(ref _checksumObject2, checksumObject, null);
                if (_checksumObject2.Kind == checksumObject.Kind)
                {
                    // we already have one
                    Contract.Requires(_checksumObject2.Checksum.Equals(checksumObject.Checksum));
                    return _checksumObject2;
                }

                // more expansive case. create map to save checksum object per kind
                EnsureLazyMap();

                _lazyChecksumToChecksumObjectMap.TryAdd(checksumObject.Checksum, checksumObject);
                if (_lazyKindToChecksumObjectMap.TryAdd(checksumObject.Kind, checksumObject))
                {
                    // just added new one
                    return checksumObject;
                }

                // there is existing one.
                return _lazyKindToChecksumObjectMap[checksumObject.Kind];
            }

            public bool TryGetValue(string kind, out ChecksumObject checksumObject)
            {
                if (ChecksumObject1?.Kind == kind)
                {
                    checksumObject = ChecksumObject1;
                    return true;
                }

                if (ChecksumObject2?.Kind == kind)
                {
                    checksumObject = ChecksumObject2;
                    return true;
                }

                if (LazyKindToChecksumObjectMap != null)
                {
                    return LazyKindToChecksumObjectMap.TryGetValue(kind, out checksumObject);
                }

                checksumObject = null;
                return false;
            }

            public bool TryGetValue(Checksum checksum, out ChecksumObject checksumObject)
            {
                if (ChecksumObject1?.Checksum == checksum)
                {
                    checksumObject = ChecksumObject1;
                    return true;
                }

                if (ChecksumObject2?.Checksum == checksum)
                {
                    checksumObject = ChecksumObject2;
                    return true;
                }

                if (LazyChecksumToChecksumObjectMap != null)
                {
                    return LazyChecksumToChecksumObjectMap.TryGetValue(checksum, out checksumObject);
                }

                checksumObject = null;
                return false;
            }

            public SubTreeNode TryGetSubTreeNode()
            {
                return LazyChecksumTree;
            }

            public SubTreeNode GetOrCreateSubTreeNode(ChecksumTreeCollection owner, Serializer serializer)
            {
                if (_lazyChecksumTree == null)
                {
                    Interlocked.CompareExchange(ref _lazyChecksumTree, new SubTreeNode(owner, serializer), null);
                }

                return _lazyChecksumTree;
            }

            private void EnsureLazyMap()
            {
                if (_lazyKindToChecksumObjectMap == null)
                {
                    // we have multiple entries. create lazy map
                    Interlocked.CompareExchange(ref _lazyKindToChecksumObjectMap, new ConcurrentDictionary<string, ChecksumObject>(concurrencyLevel: 2, capacity: 1), null);
                }

                if (_lazyChecksumToChecksumObjectMap == null)
                {
                    // we have multiple entries. create lazy map
                    Interlocked.CompareExchange(ref _lazyChecksumToChecksumObjectMap, new ConcurrentDictionary<Checksum, ChecksumObject>(concurrencyLevel: 2, capacity: 1), null);
                }
            }
        }
    }
}
