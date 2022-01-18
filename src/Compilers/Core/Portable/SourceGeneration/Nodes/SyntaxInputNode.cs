// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SyntaxInputNode<T> : IIncrementalGeneratorNode<T>, ISyntaxInputNode
    {
        private readonly Func<GeneratorSyntaxContext, CancellationToken, T> _transformFunc;
        private readonly Action<ISyntaxInputNode, IIncrementalGeneratorOutputNode> _registerOutputAndNode;
        private readonly Func<SyntaxNode, CancellationToken, bool> _filterFunc;
        private readonly IEqualityComparer<T> _comparer;
        private readonly object _filterKey = new object();

        internal SyntaxInputNode(Func<SyntaxNode, CancellationToken, bool> filterFunc, Func<GeneratorSyntaxContext, CancellationToken, T> transformFunc, Action<ISyntaxInputNode, IIncrementalGeneratorOutputNode> registerOutputAndNode, IEqualityComparer<T>? comparer = null, string? name = null)
        {
            _transformFunc = transformFunc;
            _registerOutputAndNode = registerOutputAndNode;
            _filterFunc = filterFunc;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            Name = name;
        }

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken)
        {
            return (NodeStateTable<T>)graphState.GetSyntaxInputTable(this);
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new SyntaxInputNode<T>(_filterFunc, _transformFunc, _registerOutputAndNode, comparer, Name);

        public IIncrementalGeneratorNode<T> WithTrackingName(string name) => new SyntaxInputNode<T>(_filterFunc, _transformFunc, _registerOutputAndNode, _comparer, name);

        public ISyntaxInputBuilder GetBuilder(DriverStateTable table, bool trackIncrementalSteps) => new Builder(this, table, trackIncrementalSteps);

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _registerOutputAndNode(this, output);

        private string? Name { get; }

        private sealed class Builder : ISyntaxInputBuilder
        {
            private readonly SyntaxInputNode<T> _owner;
            private readonly NodeStateTable<SyntaxNode>.Builder _filterTable;

            private readonly NodeStateTable<T>.Builder _transformTable;

            public Builder(SyntaxInputNode<T> owner, DriverStateTable table, bool trackIncrementalSteps)
            {
                _owner = owner;
                _filterTable = table.GetStateTableOrEmpty<SyntaxNode>(_owner._filterKey).ToBuilder(stepName: null, trackIncrementalSteps);
                _transformTable = table.GetStateTableOrEmpty<T>(_owner).ToBuilder(_owner.Name, trackIncrementalSteps);
            }

            public ISyntaxInputNode SyntaxInputNode { get => _owner; }

            public void SaveStateAndFree(ImmutableSegmentedDictionary<object, IStateTable>.Builder tables)
            {
                tables[_owner._filterKey] = _filterTable.ToImmutableAndFree();
                tables[_owner] = _transformTable.ToImmutableAndFree();
            }

            public void VisitTree(Lazy<SyntaxNode> root, EntryState state, SemanticModel? model, CancellationToken cancellationToken)
            {
                // We always have no inputs steps into a SyntaxInputNode, but we track the difference between "no inputs" (empty collection) and "no step information" (default value)
                var noInputStepsStepInfo = _filterTable.TrackIncrementalSteps ? ImmutableArray<(IncrementalGeneratorRunStep, int)>.Empty : default;
                if (state == EntryState.Removed)
                {
                    // mark both syntax *and* transform nodes removed
                    if (_filterTable.TryRemoveEntries(TimeSpan.Zero, noInputStepsStepInfo, out ImmutableArray<SyntaxNode> removedNodes))
                    {
                        for (int i = 0; i < removedNodes.Length; i++)
                        {
                            _transformTable.TryRemoveEntries(TimeSpan.Zero, noInputStepsStepInfo);
                        }
                    }
                }
                else
                {
                    Debug.Assert(model is object);

                    // get the syntax nodes from cache, or a syntax walk using the filter
                    if (state != EntryState.Cached || !_filterTable.TryUseCachedEntries(TimeSpan.Zero, noInputStepsStepInfo, out ImmutableArray<SyntaxNode> nodes))
                    {
                        var stopwatch = SharedStopwatch.StartNew();
                        nodes = IncrementalGeneratorSyntaxWalker.GetFilteredNodes(root.Value, _owner._filterFunc, cancellationToken);
                        _filterTable.AddEntries(nodes, state, stopwatch.Elapsed, noInputStepsStepInfo, state);
                    }

                    // now, using the obtained syntax nodes, run the transform
                    foreach (SyntaxNode node in nodes)
                    {
                        var stopwatch = SharedStopwatch.StartNew();
                        var value = new GeneratorSyntaxContext(node, model);
                        var transformed = _owner._transformFunc(value, cancellationToken);

                        if (state == EntryState.Added || !_transformTable.TryModifyEntry(transformed, _owner._comparer, stopwatch.Elapsed, noInputStepsStepInfo, state))
                        {
                            _transformTable.AddEntry(transformed, EntryState.Added, stopwatch.Elapsed, noInputStepsStepInfo, EntryState.Added);
                        }
                    }
                }
            }
        }
    }
}
