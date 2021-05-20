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
        private readonly ArrayBuilder<ISyntaxInputNode> _inputNodes;
        private readonly Action<IIncrementalGeneratorOutputNode> _registerOutput;

        internal SyntaxValueSources(ArrayBuilder<ISyntaxInputNode> inputNodes, Action<IIncrementalGeneratorOutputNode> registerOutput)
        {
            _inputNodes = inputNodes;
            _registerOutput = registerOutput;
        }

        // PROTOTYPE(source-generators): Minimum exposed, low-level API for now, we can add more as needed
        public IncrementalValueSource<T> Transform<T>(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc)
        {
            var node = new SyntaxInputNode<T>(filterFunc, transformFunc);
            _inputNodes.Add(node);
            return new IncrementalValueSource<T>(node, _registerOutput);
        }

        /// <summary>
        /// Creates a syntax receiver input node. Only used for back compat in <see cref="SourceGeneratorAdaptor"/>
        /// </summary>
        internal IncrementalValueSource<ISyntaxContextReceiver> CreateSyntaxReceiverInput(SyntaxContextReceiverCreator creator)
        {
            var node = new SyntaxReceiverInputNode(creator);
            _inputNodes.Add(node);
            return new IncrementalValueSource<ISyntaxContextReceiver>(node, _registerOutput);
        }
    }
}
