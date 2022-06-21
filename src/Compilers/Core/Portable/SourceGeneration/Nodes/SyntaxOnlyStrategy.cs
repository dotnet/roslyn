// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SyntaxOnlyStrategy : ISyntaxSelectionStrategy<SyntaxNode>
    {
        //private readonly Func<GeneratorSyntaxContext, CancellationToken, T> _transformFunc;
        private readonly Func<SyntaxNode, CancellationToken, bool> _filterFunc;

        internal SyntaxOnlyStrategy(Func<SyntaxNode, CancellationToken, bool> filterFunc)
        {
            _filterFunc = filterFunc;
        }

        public ISyntaxInputBuilder GetBuilder(StateTableStore table, object key, bool trackIncrementalSteps, string? name, IEqualityComparer<SyntaxNode>? comparer) => new Builder(this, key, table, trackIncrementalSteps, name, comparer ?? EqualityComparer<SyntaxNode>.Default);

        private sealed class Builder : ISyntaxInputBuilder
        {
            private readonly SyntaxOnlyStrategy _owner;
            private readonly string? _name;
            private readonly IEqualityComparer<SyntaxNode> _comparer;
            private readonly object _key;
            private readonly NodeStateTable<SyntaxNode>.Builder _filterTable;


            public Builder(SyntaxOnlyStrategy owner, object key, StateTableStore table, bool trackIncrementalSteps, string? name, IEqualityComparer<SyntaxNode> comparer)
            {
                _owner = owner;
                _name = name;
                _comparer = comparer;
                _key = key;
                _filterTable = table.GetStateTableOrEmpty<SyntaxNode>(_key).ToBuilder(stepName: null, trackIncrementalSteps);
            }

            public void SaveStateAndFree(StateTableStore.Builder tables)
            {
                tables.SetTable(_key, _filterTable.ToImmutableAndFree());
            }

            public void VisitTree(
                Lazy<SyntaxNode> root,
                EntryState state,
                Lazy<SemanticModel>? model,
                CancellationToken cancellationToken)
            {
                // We always have no inputs steps into a SyntaxInputNode, but we track the difference between "no inputs" (empty collection) and "no step information" (default value)
                var noInputStepsStepInfo = _filterTable.TrackIncrementalSteps ? ImmutableArray<(IncrementalGeneratorRunStep, int)>.Empty : default;
                if (state == EntryState.Removed)
                {
                    _filterTable.TryRemoveEntries(TimeSpan.Zero, noInputStepsStepInfo, out ImmutableArray<SyntaxNode> removedNodes);
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
