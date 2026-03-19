// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal abstract class SyntaxInputNode
    {
        internal abstract ISyntaxInputBuilder GetBuilder(StateTableStore table, bool trackIncrementalSteps);
    }

    internal sealed class SyntaxInputNode<T> : SyntaxInputNode, IIncrementalGeneratorNode<T>
    {
        private readonly ISyntaxSelectionStrategy<T> _inputNode;
        private readonly Action<SyntaxInputNode, IIncrementalGeneratorOutputNode> _registerOutput;
        private readonly IEqualityComparer<T> _comparer;
        private readonly string? _name;

        internal SyntaxInputNode(ISyntaxSelectionStrategy<T> inputNode, Action<SyntaxInputNode, IIncrementalGeneratorOutputNode> registerOutput, IEqualityComparer<T>? comparer = null, string? name = null)
        {
            _inputNode = inputNode;
            _registerOutput = registerOutput;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _name = name;
        }

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T>? previousTable, CancellationToken cancellationToken)
        {
            return (NodeStateTable<T>)graphState.SyntaxStore.GetSyntaxInputTable(this, graphState.GetLatestStateTableForNode(SharedInputNodes.SyntaxTrees));
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new SyntaxInputNode<T>(_inputNode, _registerOutput, comparer, _name);

        public IIncrementalGeneratorNode<T> WithTrackingName(string name) => new SyntaxInputNode<T>(_inputNode, _registerOutput, _comparer, name);

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _registerOutput(this, output);

        internal override ISyntaxInputBuilder GetBuilder(StateTableStore table, bool trackIncrementalSteps) => _inputNode.GetBuilder(table, this, trackIncrementalSteps, _name, _comparer);
    }
}
