// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Allows a user to create Syntax based input nodes for incremental generation
    /// </summary>
    public readonly struct SyntaxValueSources
    {
        private readonly ArrayBuilder<ISyntaxInputNode> _nodes;

        internal SyntaxValueSources(ArrayBuilder<ISyntaxInputNode> nodes)
        {
            _nodes = nodes;
        }

        // PROTOTYPE(source-generators): Minimum exposed, low-level API for now, we can add more as needed
        public IncrementalValueSource<T> Transform<T>(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc)
        {
            var node = new SyntaxInputNode<T>(filterFunc, transformFunc);
            _nodes.Add(node);
            return new IncrementalValueSource<T>(node);
        }
    }
}
