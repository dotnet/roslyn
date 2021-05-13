// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
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
    }

    /// <summary>
    /// Holds input nodes that are shared between generators and always exist
    /// </summary>
    internal static class SharedInputNodes
    {
        public static readonly InputNode<Compilation> Compilation = new InputNode<Compilation>(b => ImmutableArray.Create(b.Compilation));

        public static readonly InputNode<ParseOptions> ParseOptions = new InputNode<ParseOptions>(b => ImmutableArray.Create(b.DriverState.ParseOptions));

        public static readonly InputNode<AdditionalText> AdditionalTexts = new InputNode<AdditionalText>(b => b.DriverState.AdditionalTexts);

        public static readonly InputNode<SyntaxTree> SyntaxTrees = new InputNode<SyntaxTree>(b => b.Compilation.SyntaxTrees.ToImmutableArray());

        public static readonly InputNode<AnalyzerConfigOptionsProvider> AnalyzerConfigOptions = new InputNode<AnalyzerConfigOptionsProvider>(b => ImmutableArray.Create(b.DriverState.OptionsProvider));
    }

    /// <summary>
    /// Holds input nodes that are created per-generator
    /// </summary>
    internal sealed class PerGeneratorInputNodes
    {
        public static readonly PerGeneratorInputNodes Empty = new PerGeneratorInputNodes(ImmutableArray<ISyntaxInputNode>.Empty);

        private PerGeneratorInputNodes(ImmutableArray<ISyntaxInputNode> transformNodes)
        {
            this.TransformNodes = transformNodes;
        }

        public ImmutableArray<ISyntaxInputNode> TransformNodes { get; }

        public sealed class Builder
        {
            private ArrayBuilder<ISyntaxInputNode>? _transformNodes;

            bool _disposed = false;

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
                return _transformNodes is null
                    ? Empty
                    : new PerGeneratorInputNodes(_transformNodes.ToImmutable());
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
