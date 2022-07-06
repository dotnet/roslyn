// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class PredicateSyntaxStrategy<T> : ISyntaxSelectionStrategy<T>
    {
        private readonly Func<GeneratorSyntaxContext, CancellationToken, T> _transformFunc;
        private readonly Func<SyntaxNode, CancellationToken, bool> _filterFunc;
        private readonly object _filterKey = new object();

        internal PredicateSyntaxStrategy(Func<SyntaxNode, CancellationToken, bool> filterFunc, Func<GeneratorSyntaxContext, CancellationToken, T> transformFunc)
        {
            _transformFunc = transformFunc;
            _filterFunc = filterFunc;
        }

        public ISyntaxInputBuilder GetBuilder(StateTableStore table, object key, bool trackIncrementalSteps, string? name, IEqualityComparer<T>? comparer) => new Builder(this, key, table, trackIncrementalSteps, name, comparer ?? EqualityComparer<T>.Default);

        private sealed class Builder : ISyntaxInputBuilder
        {
            private readonly PredicateSyntaxStrategy<T> _owner;
            private readonly string? _name;
            private readonly IEqualityComparer<T> _comparer;
            private readonly object _key;
            private readonly NodeStateTable<SyntaxNode>.Builder _filterTable;

            private readonly NodeStateTable<T>.Builder _transformTable;

            public Builder(PredicateSyntaxStrategy<T> owner, object key, StateTableStore table, bool trackIncrementalSteps, string? name, IEqualityComparer<T> comparer)
            {
                _owner = owner;
                _name = name;
                _comparer = comparer;
                _key = key;
                _filterTable = table.GetStateTableOrEmpty<SyntaxNode>(_owner._filterKey).ToBuilder(stepName: null, trackIncrementalSteps);
                _transformTable = table.GetStateTableOrEmpty<T>(_key).ToBuilder(_name, trackIncrementalSteps);
            }

            public void SaveStateAndFree(StateTableStore.Builder tables)
            {
                tables.SetTable(_owner._filterKey, _filterTable.ToImmutableAndFree());
                tables.SetTable(_key, _transformTable.ToImmutableAndFree());
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
                        nodes = getFilteredNodes(root.Value, _owner._filterFunc, cancellationToken);
                        _filterTable.AddEntries(nodes, state, stopwatch.Elapsed, noInputStepsStepInfo, state);
                    }

                    // now, using the obtained syntax nodes, run the transform
                    foreach (SyntaxNode node in nodes)
                    {
                        var stopwatch = SharedStopwatch.StartNew();
                        var value = new GeneratorSyntaxContext(node, model);
                        var transformed = _owner._transformFunc(value, cancellationToken);

                        // The SemanticModel we provide to GeneratorSyntaxContext is never guaranteed to be the same between runs,
                        // so we never consider the input to the transform as cached.
                        var transformInputState = state == EntryState.Cached ? EntryState.Modified : state;

                        if (transformInputState == EntryState.Added || !_transformTable.TryModifyEntry(transformed, _comparer, stopwatch.Elapsed, noInputStepsStepInfo, transformInputState))
                        {
                            _transformTable.AddEntry(transformed, EntryState.Added, stopwatch.Elapsed, noInputStepsStepInfo, EntryState.Added);
                        }
                    }
                }

                static ImmutableArray<SyntaxNode> getFilteredNodes(SyntaxNode root, Func<SyntaxNode, CancellationToken, bool> func, CancellationToken token)
                {
                    ArrayBuilder<SyntaxNode>? results = null;
                    foreach (var node in root.DescendantNodesAndSelf())
                    {
                        token.ThrowIfCancellationRequested();

                        if (func(node, token))
                        {
                            (results ??= ArrayBuilder<SyntaxNode>.GetInstance()).Add(node);
                        }
                    }

                    return results.ToImmutableOrEmptyAndFree();
                }
            }
        }
    }
}
