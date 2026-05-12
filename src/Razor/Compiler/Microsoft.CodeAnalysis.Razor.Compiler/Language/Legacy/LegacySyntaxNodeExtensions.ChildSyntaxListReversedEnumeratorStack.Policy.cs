// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal static partial class LegacySyntaxNodeExtensions
{
    private partial struct ChildSyntaxListReversedEnumeratorStack
    {
        private sealed class Policy : IPooledObjectPolicy<ChildSyntaxList.Reversed.Enumerator[]>
        {
            public static readonly Policy Instance = new();

            private Policy()
            {
            }

            public ChildSyntaxList.Reversed.Enumerator[] Create() => new ChildSyntaxList.Reversed.Enumerator[16];

            public bool Return(ChildSyntaxList.Reversed.Enumerator[] stack)
            {
                // Return only reasonably-sized stacks to the pool.
                if (stack.Length < MaxArraySize)
                {
                    Array.Clear(stack, 0, stack.Length);
                    return true;
                }

                return false;
            }
        }
    }
}
