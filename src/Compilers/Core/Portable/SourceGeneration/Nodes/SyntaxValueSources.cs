// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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

        /// <summary>
        /// Creates an <see cref="IncrementalValueSource{T}"/> that can provide a transform over <see cref="SyntaxNode"/>s
        /// </summary>
        /// <typeparam name="T">The type of the value the syntax node is transformed into</typeparam>
        /// <param name="filterFunc">A function that determines if the given <see cref="SyntaxNode"/> should be transformed</param>
        /// <param name="transformFunc">A function that performs the transform, when <paramref name="filterFunc"/>returns <c>true</c> for a given node</param>
        /// <returns>An <see cref="IncrementalValueSource{T}"/> that provides the results of the transformation</returns>
        public IncrementalValueSource<T> Transform<T>(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc)
        {
            // registration of the input is deferred until we know the node is used
            return new IncrementalValueSource<T>(new SyntaxInputNode<T>(filterFunc.WrapUserFunction(), transformFunc.WrapUserFunction(), RegisterOutputAndDeferredInput));
        }

        /// <summary>
        /// Creates a syntax receiver input node. Only used for back compat in <see cref="SourceGeneratorAdaptor"/>
        /// </summary>
        internal IncrementalValueSource<ISyntaxContextReceiver> CreateSyntaxReceiverInput(SyntaxContextReceiverCreator creator)
        {
            var node = new SyntaxReceiverInputNode(creator, _registerOutput);
            _inputNodes.Add(node);
            return new IncrementalValueSource<ISyntaxContextReceiver>(node);
        }

        private void RegisterOutputAndDeferredInput(ISyntaxInputNode node, IIncrementalGeneratorOutputNode output)
        {
            _registerOutput(output);
            if (!_inputNodes.Contains(node))
            {
                _inputNodes.Add(node);
            }
        }
    }
}
