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
    internal abstract class AbstractUnnecessaryImportsProvider<T>
        : IUnnecessaryImportsProvider, IEqualityComparer<T> where T : SyntaxNode
    {
        public abstract ImmutableArray<SyntaxNode> GetUnnecessaryImports(
            SemanticModel model, Func<SyntaxNode, bool>? predicate, CancellationToken cancellationToken);

        public ImmutableArray<SyntaxNode> GetUnnecessaryImports(SemanticModel model, CancellationToken cancellationToken)
            => GetUnnecessaryImports(model, predicate: null, cancellationToken: cancellationToken);

        bool IEqualityComparer<T>.Equals([AllowNull] T x, [AllowNull] T y)
            => x?.Span == y?.Span;

        int IEqualityComparer<T>.GetHashCode([DisallowNull] T obj)
            => obj.Span.GetHashCode();
    }
}
