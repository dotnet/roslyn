// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractUnnecessaryImportsProvider<T>
        : IUnnecessaryImportsProvider, IEqualityComparer<T> where T : SyntaxNode
    {
        public ImmutableArray<SyntaxNode> GetUnnecessaryImports(
            SemanticModel model, CancellationToken cancellationToken)
        {
            var root = model.SyntaxTree.GetRoot(cancellationToken);
            return GetUnnecessaryImports(model, root, predicate: null, cancellationToken: cancellationToken);
        }

        protected abstract ImmutableArray<SyntaxNode> GetUnnecessaryImports(
            SemanticModel model, SyntaxNode root,
            Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken);

        ImmutableArray<SyntaxNode> IUnnecessaryImportsProvider.GetUnnecessaryImports(SemanticModel model, SyntaxNode root, Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken)
            => GetUnnecessaryImports(model, root, predicate, cancellationToken);

        bool IEqualityComparer<T>.Equals(T x, T y)
            => x.Span == y.Span;

        int IEqualityComparer<T>.GetHashCode(T obj)
            => obj.Span.GetHashCode();
    }
}
