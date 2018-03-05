// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LookupSymbolsInfo : AbstractLookupSymbolsInfo<Symbol>
    {
        // TODO: tune pool size.
        private const int poolSize = 64;
        private static readonly ObjectPool<LookupSymbolsInfo> s_pool = new ObjectPool<LookupSymbolsInfo>(() => new LookupSymbolsInfo(), poolSize);

        private LookupSymbolsInfo()
            : base(StringComparer.Ordinal)
        {
        }

        // To implement Poolable, you need two things:
        // 1) Expose Freeing primitive. 
        public void Free()
        {
            // Note that poolables are not finalizable. If one gets collected - no big deal.
            this.Clear();
            s_pool.Free(this);
        }

        // 2) Expose the way to get an instance.
        public static LookupSymbolsInfo GetInstance()
        {
            var info = s_pool.Allocate();
            Debug.Assert(info.Count == 0);
            return info;
        }
    }
}
