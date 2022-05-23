// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis;
internal static partial class IncrementalGeneratorInitializationContextExtensions
{
    private class SyntaxNodeArrayComparer<TSyntaxNode> : IEqualityComparer<ImmutableArray<TSyntaxNode>>
        where TSyntaxNode : SyntaxNode
    {
        public static readonly IEqualityComparer<ImmutableArray<TSyntaxNode>> Instance = new SyntaxNodeArrayComparer<TSyntaxNode>();

        public bool Equals([AllowNull] ImmutableArray<TSyntaxNode> x, [AllowNull] ImmutableArray<TSyntaxNode> y)
        {
            if (x == y)
                return true;

            if (x.Length != y.Length)
                return false;

            for (int i = 0, n = x.Length; i < n; i++)
            {
                if (x[i] != y[i])
                    return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] ImmutableArray<TSyntaxNode> obj)
        {
            var hashCode = 0;
            foreach (var node in obj)
                hashCode = Hash.Combine(hashCode, node.GetHashCode());

            return hashCode;
        }
    }
}
