// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Allows a user to create Syntax based input nodes for incremental generation
    /// </summary>
    public readonly struct SyntaxProviderFactory
    {
        private readonly ArrayBuilder<ISyntaxInputNode> _inputNodes;
        private readonly Action<IIncrementalGeneratorOutputNode> _registerOutput;
        private readonly GeneratorSyntaxHelper _syntaxHelper;

        internal SyntaxProviderFactory(ArrayBuilder<ISyntaxInputNode> inputNodes, Action<IIncrementalGeneratorOutputNode> registerOutput, GeneratorSyntaxHelper syntaxHelper)
        {
            _inputNodes = inputNodes;
            _registerOutput = registerOutput;
            _syntaxHelper = syntaxHelper;
        }

        /// <summary>
        /// Creates an <see cref="IncrementalValueProvider{TResult}"/> that can provide a transform over <see cref="SyntaxNode"/>s
        /// </summary>
        /// <typeparam name="TResult">The type of the value the syntax node is transformed into</typeparam>
        /// <param name="predicate">A function that determines if the given <see cref="SyntaxNode"/> should be transformed</param>
        /// <param name="transform">A function that performs the transform, when <paramref name="predicate"/>returns <c>true</c> for a given node</param>
        /// <returns>An <see cref="IncrementalValueProvider{TResult}"/> that provides the results of the transformation</returns>
        public IncrementalValuesProvider<TResult> FromPredicate<TResult>(Func<SyntaxNode, CancellationToken, bool> predicate, Func<GeneratorSyntaxContext, CancellationToken, TResult> transform)
        {
            // registration of the input is deferred until we know the node is used
            return new IncrementalValuesProvider<TResult>(new SyntaxInputNode<TResult>(predicate.WrapUserFunction(), transform.WrapUserFunction(), RegisterOutputAndDeferredInput));
        }

        [Obsolete("CreateSyntaxReceiver is obsolete and will be removed in an upcoming version. Use FromPredicate instead")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IncrementalValuesProvider<TResult> CreateSyntaxReceiver<TResult>(Func<SyntaxNode, CancellationToken, bool> predicate, Func<GeneratorSyntaxContext, CancellationToken, TResult> transform) => FromPredicate(predicate, transform);

        public IncrementalValuesProvider<TResult> FromAttribute<TResult>(string attributeFQN, Func<GeneratorSyntaxAttributeContext, CancellationToken, TResult> transform)
        {
            // CONSIDER: For now we just create a regular syntax input node with custom predicates.
            //           In the future we may consider implementing a custom node type.

            var helper = _syntaxHelper;
            return new IncrementalValuesProvider<TResult>(new SyntaxInputNode<TResult>((n, _) => helper.IsAttribute(n), attributeTransformFunc, RegisterOutputAndDeferredInput)).Where(r => r is not null);

            TResult attributeTransformFunc(GeneratorSyntaxContext context, CancellationToken cancellationToken)
            {
                if (!helper.TryGetAttributeData(attributeFQN, context.Node, context.SemanticModel, cancellationToken, out var attributedSyntaxNode, out var attributeData))
                {
                    // we will filter this later on before the user sees it
                    return default!;
                }

                var attributeContext = new GeneratorSyntaxAttributeContext(attributeFQN, attributedSyntaxNode, context.SemanticModel, attributeData);
                return transform.WrapUserFunction()(attributeContext, cancellationToken);
            }
        }


        /// <summary>
        /// Creates a syntax receiver input node. Only used for back compat in <see cref="SourceGeneratorAdaptor"/>
        /// </summary>
        internal IncrementalValueProvider<ISyntaxContextReceiver?> CreateSyntaxReceiverProvider(SyntaxContextReceiverCreator creator)
        {
            var node = new SyntaxReceiverInputNode(creator, _registerOutput);
            _inputNodes.Add(node);
            return new IncrementalValueProvider<ISyntaxContextReceiver?>(node);
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
