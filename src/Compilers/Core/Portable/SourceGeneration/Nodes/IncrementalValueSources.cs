// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
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

        public static readonly InputNode<AnalyzerConfigOptionsProvider> AnalzerConfigOptions = new InputNode<AnalyzerConfigOptionsProvider>();
    }

    /// <summary>
    /// Holds input nodes that are created per-generator
    /// </summary>
    internal sealed class PerGeneratorInputNodes
    {
        public static PerGeneratorInputNodes Empty = new PerGeneratorInputNodes();

        private PerGeneratorInputNodes() { }

        private PerGeneratorInputNodes(InputNode<ISyntaxContextReceiver>? receiverNode)
        {
            this.ReceiverNode = receiverNode;
        }

        public InputNode<ISyntaxContextReceiver>? ReceiverNode { get; }

        public sealed class Builder
        {
            private InputNode<ISyntaxContextReceiver>? _receiverNode;

            public Builder()
            {
            }

            public InputNode<ISyntaxContextReceiver> GetOrCreateReceiverNode() => InterlockedOperations.Initialize(ref _receiverNode, new InputNode<ISyntaxContextReceiver>());

            public PerGeneratorInputNodes ToImmutable() => new PerGeneratorInputNodes(_receiverNode);

        }
    }
}
