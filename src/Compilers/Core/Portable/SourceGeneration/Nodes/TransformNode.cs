// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class TransformNode<TInput, TOutput> : IIncrementalGeneratorNode<TOutput>
    {
        private readonly Func<TInput, CancellationToken, ImmutableArray<TOutput>> _func;
        private readonly IEqualityComparer<TOutput> _comparer;
        private readonly IIncrementalGeneratorNode<TInput> _sourceNode;
        private readonly string? _name;

        public TransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<TInput, CancellationToken, TOutput> userFunc, IEqualityComparer<TOutput>? comparer = null, string? name = null)
            : this(sourceNode, userFunc: (i, token) => ImmutableArray.Create(userFunc(i, token)), comparer, name)
        {
        }

        public TransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<TInput, CancellationToken, ImmutableArray<TOutput>> userFunc, IEqualityComparer<TOutput>? comparer = null, string? name = null)
        {
            _sourceNode = sourceNode;
            _func = userFunc;
            _comparer = comparer ?? EqualityComparer<TOutput>.Default;
            _name = name;
        }

        public IIncrementalGeneratorNode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer)
            => new TransformNode<TInput, TOutput>(_sourceNode, _func, comparer, _name);

        public IIncrementalGeneratorNode<TOutput> WithTrackingName(string name)
            => new TransformNode<TInput, TOutput>(_sourceNode, _func, _comparer, name);

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<TOutput>? previousTable, CancellationToken cancellationToken)
        {
            // grab the source inputs
            var sourceTable = builder.GetLatestStateTableForNode(_sourceNode);
            if (sourceTable.IsCached && previousTable is not null)
            {
                if (builder.DriverState.TrackIncrementalSteps)
                {
                    return previousTable.CreateCachedTableWithUpdatedSteps(sourceTable, _name, _comparer);
                }
                return previousTable;
            }

            // Semantics of a transform:
            // Element-wise comparison of upstream table
            // - Cached or Removed: no transform, just use previous values
            // - Added: perform transform and add
            // - Modified: perform transform and do element wise comparison with previous results

            var totalEntryItemCount = sourceTable.GetTotalEntryItemCountPlusEmpty();
            var newTable = builder.CreateTableBuilder(previousTable, _name, _comparer, totalEntryItemCount);

            foreach (var entry in sourceTable)
            {
                newTable.CopyEmpty(sourceTable);

                var inputs = newTable.TrackIncrementalSteps ? ImmutableArray.Create((entry.Step!, entry.OutputIndex)) : default;
                if (entry.State == EntryState.Removed)
                {
                    newTable.TryRemoveEntries(TimeSpan.Zero, inputs);
                }
                else if (entry.State != EntryState.Cached || !newTable.TryUseCachedEntries(TimeSpan.Zero, inputs))
                {
                    var stopwatch = SharedStopwatch.StartNew();
                    // generate the new entries
                    var newOutputs = _func(entry.Item, cancellationToken);

                    if (entry.State != EntryState.Modified || !newTable.TryModifyEntries(newOutputs, _comparer, stopwatch.Elapsed, inputs, entry.State))
                    {
                        newTable.AddEntries(newOutputs, EntryState.Added, stopwatch.Elapsed, inputs, entry.State);
                    }
                }
            }

            // Can't assert anything about the count of items.  _func may have produced a different amount of items if
            // it's not a 1:1 function.
            return newTable.ToImmutableAndFree();
        }

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _sourceNode.RegisterOutput(output);
    }
}
