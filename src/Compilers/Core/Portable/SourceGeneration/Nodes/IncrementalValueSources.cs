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

    public struct SyntaxValueSources
    {
        private readonly PerGeneratorInputNodes.Builder _builder;

        internal SyntaxValueSources(PerGeneratorInputNodes.Builder builder)
        {
            _builder = builder;
        }

        //public IncrementalValueSource<T> Transform<T>(Func<GeneratorSyntaxContext, T> func)
        //{
        //    // we need to save the func somewhere. Presumably inside the builder.
        //    // how are we going to handlle the input nodes?


        //  //  new IIncrementalGeneratorNode<T>

        //    // register the transform with the builder.
        //    // do... something?

        //    return default;
        //}

        public IncrementalValueSource<T> TransformMany<T>(Func<GeneratorSyntaxContext, ImmutableArray<T>> func)
        {
            var node = new SyntaxTransformNode<T>(func);
            _builder.SyntaxTransformNodes.Add(node);
            return new IncrementalValueSource<T>(node);
        }

        public IncrementalValueSource<T> Transform<T>(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc)
        {
            var node = new SyntaxTransformNode<T>(filterFunc, transformFunc);
            _builder.SyntaxTransformNodes.Add(node);
            return new IncrementalValueSource<T>(node);
        }

        //public IncrementalValueSource<GeneratorSyntaxContext> Filter(Func<GeneratorSyntaxContext, bool> applies) => default;
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

    internal interface ISyntaxTransformNode
    {
        ISyntaxTransformBuilder GetBuilder();

    }

    internal interface ISyntaxTransformBuilder
    {
        void OnVisitSyntaxNode(GeneratorSyntaxContext context);
        void SetInputState(DriverStateTable.Builder driverStateBuilder);
    }

    internal class SyntaxTransformNode<T> : InputNode<T>, ISyntaxTransformNode
    {
        private readonly Func<GeneratorSyntaxContext, ImmutableArray<T>> _func;

        private readonly Func<SyntaxNode, bool> _filterFunc;

        internal SyntaxTransformNode(Func<GeneratorSyntaxContext, ImmutableArray<T>> func)
        {
            _filterFunc = (n) => true;
            _func = func;
        }

        internal SyntaxTransformNode(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc)
        {
            _func = (t) => ImmutableArray.Create(transformFunc(t));
            _filterFunc = filterFunc;
        }

        public ISyntaxTransformBuilder GetBuilder() => new Builder(this);

        internal class Builder : ISyntaxTransformBuilder
        {
            private readonly SyntaxTransformNode<T> _owner;

            private readonly NodeStateTable<T>.Builder _stateTable;

            public Builder(SyntaxTransformNode<T> owner)
            {
                _owner = owner;
                //PROTOTYPE(source-generators): presumably actually want to get the previous one, right?
                _stateTable = new NodeStateTable<T>.Builder();
            }

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                //PROTOTYPE(source-generators): make this actually be efficient.
                if (_owner._filterFunc(context.Node))
                {
                    var result = _owner._func(context).ToImmutableArray();
                    _stateTable.AddEntries(result, EntryState.Added);
                }
            }

            public void SetInputState(DriverStateTable.Builder driverStateBuilder)
            {
                // set the table from the built up state.
                driverStateBuilder.SetInputState(_owner, _stateTable.ToImmutableAndFree());
            }
        }
    }

    // For now, we build the incremental syntax stuff on top of ISyntaxReceiver, but in the future we probably want to invert the relationsip
    internal class IncrementalSyntaxReceiver : ISyntaxContextReceiver
    {
        private ImmutableArray<ISyntaxTransformBuilder> transformBuilders;

        public IncrementalSyntaxReceiver(ImmutableArray<ISyntaxTransformNode> transformNodes)
        {
            this.transformBuilders = transformNodes.SelectAsArray(n => n.GetBuilder());
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            foreach (var node in transformBuilders)
            {
                node.OnVisitSyntaxNode(context);
            }
        }

        internal void SetInputStates(DriverStateTable.Builder driverStateBuilder)
        {
            foreach (var node in transformBuilders)
            {
                node.SetInputState(driverStateBuilder);
            }
        }
    }
}
