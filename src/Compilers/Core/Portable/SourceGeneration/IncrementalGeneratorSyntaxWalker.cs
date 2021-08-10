// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class IncrementalGeneratorSyntaxWalker : SyntaxWalker
    {
        private readonly Func<SyntaxNode, CancellationToken, bool> _filter;
        private readonly CancellationToken _token;
        private ArrayBuilder<SyntaxNode>? _results;

        internal IncrementalGeneratorSyntaxWalker(Func<SyntaxNode, CancellationToken, bool> filter, CancellationToken token)
        {
            _filter = filter;
            _token = token;
        }

        public static ImmutableArray<SyntaxNode> GetFilteredNodes(SyntaxNode root, Func<SyntaxNode, CancellationToken, bool> func, CancellationToken token)
        {
            var walker = new IncrementalGeneratorSyntaxWalker(func, token);
            walker.Visit(root);
            return walker._results.ToImmutableOrEmptyAndFree();
        }

        public override void Visit(SyntaxNode node)
        {
            _token.ThrowIfCancellationRequested();

            if (_filter(node, _token))
            {
                (_results ??= ArrayBuilder<SyntaxNode>.GetInstance()).Add(node);
            }
            base.Visit(node);
        }
    }
}
