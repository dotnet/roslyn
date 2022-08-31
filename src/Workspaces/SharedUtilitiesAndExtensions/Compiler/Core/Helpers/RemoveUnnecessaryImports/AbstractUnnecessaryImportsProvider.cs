// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractUnnecessaryImportsProvider<TSyntaxNode> :
        IUnnecessaryImportsProvider<TSyntaxNode>,
        IEqualityComparer<TSyntaxNode>
        where TSyntaxNode : SyntaxNode
    {
        public abstract ImmutableArray<TSyntaxNode> GetUnnecessaryImports(
            SemanticModel model, Func<SyntaxNode, bool>? predicate, CancellationToken cancellationToken);

        public ImmutableArray<TSyntaxNode> GetUnnecessaryImports(SemanticModel model, CancellationToken cancellationToken)
            => GetUnnecessaryImports(model, predicate: null, cancellationToken: cancellationToken);

        bool IEqualityComparer<TSyntaxNode>.Equals([AllowNull] TSyntaxNode x, [AllowNull] TSyntaxNode y)
            => x?.Span == y?.Span;

        int IEqualityComparer<TSyntaxNode>.GetHashCode([DisallowNull] TSyntaxNode obj)
            => obj.Span.GetHashCode();
    }
}
