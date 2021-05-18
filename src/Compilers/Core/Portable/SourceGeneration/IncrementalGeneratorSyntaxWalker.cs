// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class IncrementalGeneratorSyntaxWalker : SyntaxWalker
    {
        private readonly Func<SyntaxNode, bool> _filter;

        private ArrayBuilder<SyntaxNode>? _results;

        internal IncrementalGeneratorSyntaxWalker(Func<SyntaxNode, bool> filter)
        {
            _filter = filter;
        }

        public static ImmutableArray<SyntaxNode> GetFilteredNodes(SyntaxNode root, Func<SyntaxNode, bool> func)
        {
            var walker = new IncrementalGeneratorSyntaxWalker(func);
            walker.Visit(root);
            return walker._results.ToImmutableOrEmptyAndFree();
        }

        public override void Visit(SyntaxNode node)
        {
            if (_filter(node))
            {
                (_results ??= ArrayBuilder<SyntaxNode>.GetInstance()).Add(node);
            }
            base.Visit(node);
        }
    }
}
