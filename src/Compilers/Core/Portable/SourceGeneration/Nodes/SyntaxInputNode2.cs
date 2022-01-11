// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal class SyntaxInputNode2<T> : IIncrementalGeneratorNode<T>, ISyntaxInputNode
    {
        private readonly ISyntaxInputNodeInner<T> _inputNode;
        private readonly Action<ISyntaxInputNode, IIncrementalGeneratorOutputNode> _registerOutput;
        private readonly IEqualityComparer<T> _comparer;
        private readonly string? _name;

        internal SyntaxInputNode2(ISyntaxInputNodeInner<T> inputNode, Action<ISyntaxInputNode, IIncrementalGeneratorOutputNode> registerOutput, IEqualityComparer<T>? comparer = null, string? name = null)
        {
            _inputNode = inputNode;
            _registerOutput = registerOutput;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _name = name;
        }

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken)
        {
            return (NodeStateTable<T>)graphState.SyntaxStore.GetSyntaxInputTable(this, graphState.GetLatestStateTableForNode(SharedInputNodes.SyntaxTrees));
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new SyntaxInputNode2<T>(_inputNode, _registerOutput, comparer, _name);

        public IIncrementalGeneratorNode<T> WithTrackingName(string name) => new SyntaxInputNode2<T>(_inputNode, _registerOutput, _comparer, name);

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _registerOutput(this, output);

        public ISyntaxInputBuilder GetBuilder(StateTableStore table, bool trackIncrementalSteps) => _inputNode.GetBuilder(table, trackIncrementalSteps, _name, _comparer, this);
    }
}
