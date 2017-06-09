// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Cache data associated with TBoundNode
    /// </summary>
    internal class OperationCache<TBoundNode>
    {
        // REVIEW: not sure about perf of value tuple used as key on a dictionary. I might need custom type
        //         if it is somehow not good on perf
        //
        // cache's key is (TBoundNode, string) since some of TBoundNode has multiple data associated with it.
        private readonly ConcurrentDictionary<(TBoundNode key, string kind), object> _cache =
            new ConcurrentDictionary<(TBoundNode key, string kind), object>(concurrencyLevel: 2, capacity: 10);

        public TRet GetOrCreateOperationFrom<TNode, TRet>(TNode key, Func<TNode, TRet> creator)
            where TNode : TBoundNode
            where TRet : IOperation
        {
            return GetOrCreateOperationFrom(key, "Root", creator);
        }

        public TRet GetOrCreateOperationFrom<TNode, TRet>(TNode key, string kind, Func<TNode, TRet> creator)
            where TNode : TBoundNode
            where TRet : IOperation
        {
            return GetValue(key, kind, creator);
        }

        public ImmutableArray<TRet> GetOrCreateOperationsFrom<TNode, TRet>(TNode key, string kind, Func<TNode, ImmutableArray<TRet>> creator)
            where TNode : TBoundNode
            where TRet : IOperation
        {
            return GetValue(key, kind, creator);
        }

        private TRet GetValue<TNode, TRet>(TNode key, string kind, Func<TNode, TRet> creator)
            where TNode : TBoundNode
        {
            return (TRet)_cache.GetOrAdd((key, kind), kv => creator((TNode)kv.key));
        }
    }
}
