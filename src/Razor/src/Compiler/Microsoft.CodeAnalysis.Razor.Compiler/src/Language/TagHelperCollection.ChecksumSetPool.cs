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
        private const int MaximumObjectSize = 2048;

        public static readonly ChecksumSetPool Default = new(Policy.Instance, DefaultPoolSize);

        private ChecksumSetPool(PooledObjectPolicy policy, Optional<int> poolSize)
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
                return new(capacity: MaximumObjectSize);
#else
                return [];
#endif
            }

            public override bool Return(HashSet<Checksum> set)
            {
                var count = set.Count;
                set.Clear();

                if (count > MaximumObjectSize)
                {
#if NET9_0_OR_GREATER
                    set.TrimExcess(MaximumObjectSize);
#else
                    set.TrimExcess();
#endif
                }

                return true;
            }
        }
    }
}
