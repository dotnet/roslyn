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

        public SyntaxValueSources Syntax => new SyntaxValueSources(_perGeneratorBuilder);

        public IncrementalValueSource<Compilation> Compilation => new IncrementalValueSource<Compilation>(SharedInputNodes.Compilation);

        public IncrementalValueSource<ParseOptions> ParseOptions => new IncrementalValueSource<ParseOptions>(SharedInputNodes.ParseOptions);

        public IncrementalValueSource<AdditionalText> AdditionalTexts => new IncrementalValueSource<AdditionalText>(SharedInputNodes.AdditionalTexts);

        public IncrementalValueSource<AnalyzerConfigOptionsProvider> AnalyzerConfigOptions => new IncrementalValueSource<AnalyzerConfigOptionsProvider>(SharedInputNodes.AnalzerConfigOptions);

        //only used for back compat in the adaptor
        internal IncrementalValueSource<ISyntaxContextReceiver> CreateSyntaxReceiver() => new IncrementalValueSource<ISyntaxContextReceiver>(_perGeneratorBuilder.GetOrCreateReceiverNode());
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

        public static readonly InputNode<AnalyzerConfigOptionsProvider> AnalzerConfigOptions = new InputNode<AnalyzerConfigOptionsProvider>();
    }

    /// <summary>
    /// Holds input nodes that are created per-generator
    /// </summary>
    internal sealed class PerGeneratorInputNodes
    {
        public static PerGeneratorInputNodes Empty = new PerGeneratorInputNodes();

        private PerGeneratorInputNodes() { }

        private PerGeneratorInputNodes(InputNode<ISyntaxContextReceiver>? receiverNode, ImmutableArray<ISyntaxTransformNode> transformNodes)
        {
            this.ReceiverNode = receiverNode;
            this.TransformNodes = transformNodes;
        }

        public ImmutableArray<ISyntaxTransformNode> TransformNodes { get; }

        public InputNode<ISyntaxContextReceiver>? ReceiverNode { get; }

        public sealed class Builder
        {
            private InputNode<ISyntaxContextReceiver>? _receiverNode;

            bool disposed = false;

            public Builder()
            {
            }

            public InputNode<ISyntaxContextReceiver> GetOrCreateReceiverNode()
            {
                Debug.Assert(!disposed);
                return InterlockedOperations.Initialize(ref _receiverNode, new InputNode<ISyntaxContextReceiver>());
            }

            public ArrayBuilder<ISyntaxTransformNode> SyntaxTransformNodes { get; } = ArrayBuilder<ISyntaxTransformNode>.GetInstance();

            public PerGeneratorInputNodes ToImmutable()
            {
                Debug.Assert(!disposed);
                disposed = true;
                return _receiverNode is null ? Empty : new PerGeneratorInputNodes(_receiverNode, SyntaxTransformNodes.ToImmutableAndFree());
            }
        }
    }
}
