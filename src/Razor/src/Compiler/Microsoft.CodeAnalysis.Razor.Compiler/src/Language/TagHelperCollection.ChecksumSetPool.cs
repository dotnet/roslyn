// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    private sealed class ChecksumSetPool : CustomObjectPool<HashSet<Checksum>>
    {
        // Large projects can have thousands of tag helpers, causing the HashSet to grow well past
        // the LOH threshold (85KB) during document compilation. With a small retention limit, the
        // set is trimmed on return and must re-grow (through multiple LOH-allocating resizes) on
        // every subsequent use. Traces have shown GBs of allocation pressure from this resize churn.
        // A higher retention limit keeps the grown set in the pool, trading a few MB of stable
        // potentially LOH memory (across ~20 pool slots, most of which remain empty) for
        // eliminating repeated resize allocations. The initial capacity is kept smaller so that
        // small projects don't immediately allocate LOH-sized arrays.
        private const int InitialCapacity = 2048;
        private const int MaximumRetainedCapacity = 16384;

        public static readonly ChecksumSetPool Default = new(Policy.Instance, DefaultPoolSize);

        private ChecksumSetPool(PooledObjectPolicy policy, Opt<int> poolSize)
            : base(policy, poolSize)
        {
        }

        private sealed class Policy : PooledObjectPolicy
        {
            public static readonly Policy Instance = new();

            private Policy()
            {
            }

            public override HashSet<Checksum> Create()
            {
#if NET
                return new(capacity: InitialCapacity);
#else
                return [];
#endif
            }

            public override bool Return(HashSet<Checksum> set)
            {
                var count = set.Count;
                set.Clear();

                if (count > MaximumRetainedCapacity)
                {
#if NET9_0_OR_GREATER
                    set.TrimExcess(InitialCapacity);
#else
                    set.TrimExcess();
#endif
                }

                return true;
            }
        }
    }
}
