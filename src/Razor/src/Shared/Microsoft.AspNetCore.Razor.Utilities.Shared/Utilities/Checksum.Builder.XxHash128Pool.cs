// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;
using System.IO.Hashing;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal readonly partial record struct Checksum
{
    internal readonly ref partial struct Builder
    {
        private sealed class XxHash128Pool : CustomObjectPool<XxHash128>
        {
            public static readonly XxHash128Pool Default = new(Policy.Instance, DefaultPoolSize);

            private XxHash128Pool(PooledObjectPolicy policy, Optional<int> poolSize)
                : base(policy, poolSize)
            {
            }

            private sealed class Policy : PooledObjectPolicy
            {
                public static readonly Policy Instance = new();

                private Policy()
                {
                }

                public override XxHash128 Create() => new();

                public override bool Return(XxHash128 hash)
                {
                    return true;
                }
            }
        }
    }
}
