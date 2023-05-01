// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal abstract class SyntaxInputNode
    {
        internal abstract ISyntaxInputBuilder GetBuilder(StateTableStore table, bool trackIncrementalSteps);
    }

    internal sealed class SyntaxInputNode<T> : SyntaxInputNode, IIncrementalGeneratorNode<T>
    {
        private readonly ISyntaxSelectionStrategy<T> _inputNode;
        private readonly Action<SyntaxInputNode, ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> _registerOutput;
        private readonly IEqualityComparer<T> _comparer;
        private readonly string? _name;

        internal SyntaxInputNode(ISyntaxSelectionStrategy<T> inputNode, TransformFactory transformFactory, Action<SyntaxInputNode, ArrayBuilder<IIncrementalGeneratorOutputNode>, IIncrementalGeneratorOutputNode> registerOutput, IEqualityComparer<T>? comparer = null, string? name = null)
        {
            _inputNode = inputNode;
            TransformFactory = transformFactory;
            _registerOutput = registerOutput;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _name = name;
        }

        public TransformFactory TransformFactory { get; }

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T>? previousTable, CancellationToken cancellationToken)
        {
            return (NodeStateTable<T>)graphState.SyntaxStore.GetSyntaxInputTable(this, graphState.GetLatestStateTableForNode(SharedInputNodes.SyntaxTrees));
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer)
        {
            return TransformFactory.WithComparerAndTrackingName(this, ApplyComparer, ApplyTrackingName, comparer, _name);
        }

        public IIncrementalGeneratorNode<T> WithTrackingName(string name)
        {
            return TransformFactory.WithComparerAndTrackingName(this, ApplyComparer, ApplyTrackingName, _comparer, name);
        }

        private static IIncrementalGeneratorNode<T> ApplyComparer(IIncrementalGeneratorNode<T> node, IEqualityComparer<T>? comparer)
        {
            var inputNode = (SyntaxInputNode<T>)node;
            if (inputNode._comparer == (comparer ?? EqualityComparer<T>.Default))
                return inputNode;

            return new SyntaxInputNode<T>(inputNode._inputNode, inputNode.TransformFactory, inputNode._registerOutput, comparer, inputNode._name);
        }

        private static IIncrementalGeneratorNode<T> ApplyTrackingName(IIncrementalGeneratorNode<T> node, string? name)
        {
            var inputNode = (SyntaxInputNode<T>)node;
            if (inputNode._name == name)
                return inputNode;

            return new SyntaxInputNode<T>(inputNode._inputNode, inputNode.TransformFactory, inputNode._registerOutput, inputNode._comparer, name);
        }

        public void RegisterOutput(ArrayBuilder<IIncrementalGeneratorOutputNode> outputNodes, IIncrementalGeneratorOutputNode output) => _registerOutput(this, outputNodes, output);

        internal override ISyntaxInputBuilder GetBuilder(StateTableStore table, bool trackIncrementalSteps) => _inputNode.GetBuilder(table, this, trackIncrementalSteps, _name, _comparer);
    }
}
