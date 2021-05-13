// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public readonly struct IncrementalValueSources
    {
        private readonly PerGeneratorInputNodes.Builder _perGeneratorBuilder;

        internal IncrementalValueSources(PerGeneratorInputNodes.Builder perGeneratorBuilder)
        {
            _perGeneratorBuilder = perGeneratorBuilder;
        }

        public SyntaxValueSources Syntax => new SyntaxValueSources(_perGeneratorBuilder.SyntaxTransformNodes);

        public IncrementalValueSource<Compilation> Compilation => new IncrementalValueSource<Compilation>(SharedInputNodes.Compilation);

        public IncrementalValueSource<ParseOptions> ParseOptions => new IncrementalValueSource<ParseOptions>(SharedInputNodes.ParseOptions);

        public IncrementalValueSource<AdditionalText> AdditionalTexts => new IncrementalValueSource<AdditionalText>(SharedInputNodes.AdditionalTexts);

        public IncrementalValueSource<AnalyzerConfigOptionsProvider> AnalyzerConfigOptions => new IncrementalValueSource<AnalyzerConfigOptionsProvider>(SharedInputNodes.AnalyzerConfigOptions);

        //only used for back compat in the adaptor
        internal IncrementalValueSource<ISyntaxContextReceiver?> CreateSyntaxReceiver() => new IncrementalValueSource<ISyntaxContextReceiver?>(_perGeneratorBuilder.GetOrCreateReceiverNode());
    }

    /// <summary>
    /// Holds input nodes that are shared between generators and always exist
    /// </summary>
    internal static class SharedInputNodes
    {
        public static readonly InputNode<Compilation> Compilation = new InputNode<Compilation>();

        public static readonly InputNode<ParseOptions> ParseOptions = new InputNode<ParseOptions>();

        public static readonly InputNode<AdditionalText> AdditionalTexts = new InputNode<AdditionalText>();

        public static readonly InputNode<SyntaxTree> SyntaxTrees = new InputNode<SyntaxTree>();

        public static readonly InputNode<AnalyzerConfigOptionsProvider> AnalyzerConfigOptions = new InputNode<AnalyzerConfigOptionsProvider>();
    }

    /// <summary>
    /// Holds input nodes that are created per-generator
    /// </summary>
    internal sealed class PerGeneratorInputNodes
    {
        public static readonly PerGeneratorInputNodes Empty = new PerGeneratorInputNodes(receiverNode: null, ImmutableArray<ISyntaxInputNode>.Empty);

        private PerGeneratorInputNodes(InputNode<ISyntaxContextReceiver?>? receiverNode, ImmutableArray<ISyntaxInputNode> transformNodes)
        {
            this.ReceiverNode = receiverNode;
            this.TransformNodes = transformNodes;
        }

        public ImmutableArray<ISyntaxInputNode> TransformNodes { get; }

        public InputNode<ISyntaxContextReceiver?>? ReceiverNode { get; }

        public sealed class Builder
        {
            private InputNode<ISyntaxContextReceiver?>? _receiverNode;

            private ArrayBuilder<ISyntaxInputNode>? _transformNodes;

            bool _disposed = false;

            public InputNode<ISyntaxContextReceiver?> GetOrCreateReceiverNode()
            {
                Debug.Assert(!_disposed);
                if (_receiverNode is null)
                {
                    // this is called by only internal code which we know to be thread safe
                    _receiverNode = new InputNode<ISyntaxContextReceiver?>();
                }
                return _receiverNode;
            }

            public ArrayBuilder<ISyntaxInputNode> SyntaxTransformNodes
            {
                get
                {
                    Debug.Assert(!_disposed);
                    if (_transformNodes is null)
                    {
                        // PROTOTYPE(source-generators): this is resilient to threading in the user pipeline
                        // but the rest of the structure isn't. We should decide if we want to support that or 
                        // just say this whole thing isn't thread safe.

                        var newNodes = ArrayBuilder<ISyntaxInputNode>.GetInstance();
                        InterlockedOperations.Initialize(ref _transformNodes, newNodes);

                        // in the case another thread beat us to initialization, we will see that threads arraybuilder.
                        // free the one we just created
                        if (newNodes != _transformNodes)
                        {
                            newNodes.Free();
                        }
                    }
                    return _transformNodes;
                }
            }

            public PerGeneratorInputNodes ToImmutable()
            {
                Debug.Assert(!_disposed);
                return _receiverNode is null && _transformNodes is null
                    ? Empty
                    : new PerGeneratorInputNodes(_receiverNode, _transformNodes?.ToImmutable() ?? ImmutableArray<ISyntaxInputNode>.Empty);
            }

            public void Free()
            {
                _disposed = true;
                _transformNodes?.Free();
                _transformNodes = null;
            }
        }
    }
}
