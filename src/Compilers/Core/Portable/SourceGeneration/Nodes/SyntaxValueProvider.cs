// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Allows a user to create Syntax based input nodes for incremental generation
    /// </summary>
    public readonly partial struct SyntaxValueProvider
    {
        private readonly IncrementalGeneratorInitializationContext _context;
        private readonly ArrayBuilder<SyntaxInputNode> _inputNodes;
        private readonly TransformFactory _transformFactory;
        private readonly Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> _registerOutput;
        private readonly Action<SyntaxInputNode, ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> _registerOutputAndDeferredInput;
        private readonly ISyntaxHelper _syntaxHelper;

        internal SyntaxValueProvider(
            IncrementalGeneratorInitializationContext context,
            ArrayBuilder<SyntaxInputNode> inputNodes,
            TransformFactory transformFactory,
            Action<ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> registerOutput,
            ISyntaxHelper syntaxHelper)
        {
            _context = context;
            _inputNodes = inputNodes;
            _transformFactory = transformFactory;
            _registerOutput = registerOutput;
            _registerOutputAndDeferredInput = RegisterOutputAndDeferredInput;
            _syntaxHelper = syntaxHelper;
        }

        /// <summary>
        /// Creates an <see cref="IncrementalValueProvider{T}"/> that can provide a transform over <see cref="SyntaxNode"/>s
        /// </summary>
        /// <typeparam name="T">The type of the value the syntax node is transformed into</typeparam>
        /// <param name="predicate">A function that determines if the given <see cref="SyntaxNode"/> should be transformed</param>
        /// <param name="transform">A function that performs the transform, when <paramref name="predicate"/>returns <c>true</c> for a given node</param>
        /// <returns>An <see cref="IncrementalValueProvider{T}"/> that provides the results of the transformation</returns>
        public IncrementalValuesProvider<T> CreateSyntaxProvider<T>(Func<SyntaxNode, CancellationToken, bool> predicate, Func<GeneratorSyntaxContext, CancellationToken, T> transform)
        {
            // registration of the input is deferred until we know the node is used
            return new IncrementalValuesProvider<T>(
                new SyntaxInputNode<T>(
                    new PredicateSyntaxStrategy<T>(_transformFactory.WrapUserFunction(predicate), _transformFactory.WrapUserFunction(transform), _syntaxHelper),
                    _transformFactory,
                    _registerOutputAndDeferredInput));
        }

        /// <summary>
        /// Creates a syntax receiver input node. Only used for back compat in <see cref="SourceGeneratorAdaptor"/>
        /// </summary>
        internal IncrementalValueProvider<ISyntaxContextReceiver?> CreateSyntaxReceiverProvider(SyntaxContextReceiverCreator creator)
        {
            var node = new SyntaxInputNode<ISyntaxContextReceiver?>(
                new SyntaxReceiverStrategy<ISyntaxContextReceiver?>(creator, _registerOutput, _syntaxHelper), _transformFactory, _registerOutputAndDeferredInput);
            _inputNodes.Add(node);
            return new IncrementalValueProvider<ISyntaxContextReceiver?>(node);
        }

        private void RegisterOutputAndDeferredInput(SyntaxInputNode node, ArrayBuilder<IIncrementalGeneratorOutputNode> outputNodes, IIncrementalGeneratorOutputNode output)
        {
            _registerOutput(outputNodes, output);
            if (!_inputNodes.Contains(node))
            {
                _inputNodes.Add(node);
            }
        }
    }
}
