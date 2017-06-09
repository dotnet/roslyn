// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis
{
    internal class OperationCache<TBoundNode>
    {
        private readonly ConcurrentDictionary<(TBoundNode key, string kind), object> _cache =
            new ConcurrentDictionary<(TBoundNode key, string kind), object>(concurrencyLevel: 2, capacity: 10);

        public TRet GetValue<TNode, TRet>(TNode key, Func<TNode, TRet> creator)
            where TNode : TBoundNode
        {
            return GetValue(key, "Root", creator);
        }

        public TRet GetValue<TNode, TRet>(TNode key, string kind, Func<TNode, TRet> creator)
            where TNode : TBoundNode
        {
            return (TRet)_cache.GetOrAdd((key, kind), kv => creator((TNode)kv.key));
        }
    }
}
