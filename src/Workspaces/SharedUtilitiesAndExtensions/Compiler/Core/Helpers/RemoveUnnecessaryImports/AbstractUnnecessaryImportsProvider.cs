// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

internal abstract class AbstractUnnecessaryImportsProvider<TSyntaxNode> :
    IUnnecessaryImportsProvider<TSyntaxNode>,
    IEqualityComparer<TSyntaxNode>
    where TSyntaxNode : SyntaxNode
{
    public abstract ImmutableArray<TSyntaxNode> GetUnnecessaryImports(
        SemanticModel model, Func<SyntaxNode, bool>? predicate, CancellationToken cancellationToken);

    public ImmutableArray<TSyntaxNode> GetUnnecessaryImports(SemanticModel model, TextSpan? span, CancellationToken cancellationToken)
        => GetUnnecessaryImports(model, span, predicate: null, cancellationToken: cancellationToken);

    public ImmutableArray<TSyntaxNode> GetUnnecessaryImports(
        SemanticModel model, TextSpan? span, Func<SyntaxNode, bool>? predicate, CancellationToken cancellationToken)
    {
        // Bail out if there are no usings/imports in the filter span.
        if (span.HasValue && !HasImportThatIntersectsWithSpan(span.Value))
            return [];

        return GetUnnecessaryImports(model, predicate, cancellationToken);

        bool HasImportThatIntersectsWithSpan(TextSpan span)
        {
            var root = model.SyntaxTree.GetRoot(cancellationToken);
            return root
                .DescendantNodes(n => n.FullSpan.IntersectsWith(span))
                .Where(n => n.FullSpan.IntersectsWith(span))
                .OfType<TSyntaxNode>()
                .Any();
        }
    }

    bool IEqualityComparer<TSyntaxNode>.Equals([AllowNull] TSyntaxNode x, [AllowNull] TSyntaxNode y)
        => x?.Span == y?.Span;

    int IEqualityComparer<TSyntaxNode>.GetHashCode([DisallowNull] TSyntaxNode obj)
        => obj.Span.GetHashCode();
}
