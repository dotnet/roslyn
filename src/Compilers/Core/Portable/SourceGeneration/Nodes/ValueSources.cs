// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public struct ValueSources
    {
        private readonly GeneratorValueSources.Builder _generatorSourceBuilder;

        internal ValueSources(GeneratorValueSources.Builder generatorSourceBuilder)
        {
            _generatorSourceBuilder = generatorSourceBuilder;
        }

        public IncrementalValueSource<Compilation> Compilation => new IncrementalValueSource<Compilation>(CommonValueSources.Compilation);

        public IncrementalValueSource<AdditionalText> AdditionalTexts => new IncrementalValueSource<AdditionalText>(CommonValueSources.AdditionalTexts);

        public IncrementalValueSource<AnalyzerConfigOptionsProvider> AnalyzerConfigOptions => new IncrementalValueSource<AnalyzerConfigOptionsProvider>(CommonValueSources.AnalzerConfigOptions);

        //only used for back compat in the adaptor
        internal IncrementalValueSource<ISyntaxContextReceiver> SyntaxReceiver => new IncrementalValueSource<ISyntaxContextReceiver>(_generatorSourceBuilder.ReceiverNode);
    }

    /// <summary>
    /// Holds value sources that are shared between generators and always exist
    /// </summary>
    internal static class CommonValueSources
    {
        // PROTOTYPE(source-generators):should this be called commonInputNodes?
        public static readonly InputNode<Compilation> Compilation = new InputNode<Compilation>();

        public static readonly InputNode<AdditionalText> AdditionalTexts = new InputNode<AdditionalText>();

        public static readonly InputNode<AnalyzerConfigOptionsProvider> AnalzerConfigOptions = new InputNode<AnalyzerConfigOptionsProvider>();
    }

    /// <summary>
    /// Holds value sources that are created per-generator
    /// </summary>
    internal sealed class GeneratorValueSources
    {
        public static GeneratorValueSources Empty = new GeneratorValueSources();

        private GeneratorValueSources() { }

        private GeneratorValueSources(InputNode<ISyntaxContextReceiver>? receiverNode)
        {
            this.ReceiverNode = receiverNode;
        }

        public InputNode<ISyntaxContextReceiver>? ReceiverNode { get; }

        public class Builder
        {
            InputNode<ISyntaxContextReceiver>? _receiverNode;

            public Builder()
            {
            }

            public InputNode<ISyntaxContextReceiver> ReceiverNode => InterlockedOperations.Initialize(ref _receiverNode, new InputNode<ISyntaxContextReceiver>());

            public GeneratorValueSources ToImmutable() => new GeneratorValueSources(_receiverNode);

        }
    }
}
