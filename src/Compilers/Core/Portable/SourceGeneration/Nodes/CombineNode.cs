// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CombineNode<TInput1, TInput2> : IIncrementalGeneratorNode<(TInput1, TInput2)>
    {
        private readonly IIncrementalGeneratorNode<TInput1> _input1;
        private readonly IIncrementalGeneratorNode<TInput2> _input2;
        private readonly IEqualityComparer<(TInput1, TInput2)>? _comparer;
        private readonly string? _name;

        public CombineNode(IIncrementalGeneratorNode<TInput1> input1, IIncrementalGeneratorNode<TInput2> input2, IEqualityComparer<(TInput1, TInput2)>? comparer = null, string? name = null)
        {
            _input1 = input1;
            _input2 = input2;
            _comparer = comparer;
            _name = name;
        }

        public NodeStateTable<(TInput1, TInput2)> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<(TInput1, TInput2)>? previousTable, CancellationToken cancellationToken)
        {
            // get both input tables
            var input1Table = graphState.GetLatestStateTableForNode(_input1);
            var input2Table = graphState.GetLatestStateTableForNode(_input2);

            if (input1Table.IsCached && input2Table.IsCached && previousTable is not null)
            {
                if (graphState.DriverState.TrackIncrementalSteps)
                {
                    return RecordStepsForCachedTable(graphState, previousTable, input1Table, input2Table);
                }
                return previousTable;
            }

            var totalEntryItemCount = input1Table.GetTotalEntryItemCountPlusEmpty();
            var builder = graphState.CreateTableBuilder(previousTable, _name, _comparer, totalEntryItemCount);

            // Semantics of a join:
            //
            // When input1[i] is cached:
            //  - cached if input2 is also cached
            //  - modified otherwise
            // State of input1[i] otherwise.

            // get the input2 item
            var isInput2Cached = input2Table.IsCached;
            (TInput2 input2, IncrementalGeneratorRunStep? input2Step) = input2Table.Single();

            // append the input2 item to each item in input1 
            foreach (var entry1 in input1Table)
            {
                builder.CopyEmpty(input1Table);

                var stopwatch = SharedStopwatch.StartNew();

                var stepInputs = builder.TrackIncrementalSteps ? ImmutableArray.Create((entry1.Step!, entry1.OutputIndex), (input2Step!, 0)) : default;

                var state = (entry1.State, isInput2Cached) switch
                {
                    (EntryState.Cached, true) => EntryState.Cached,
                    (EntryState.Cached, false) => EntryState.Modified,
                    _ => entry1.State
                };

                var entry = (entry1.Item, input2);
                if (state != EntryState.Modified || _comparer is null || !builder.TryModifyEntry(entry, _comparer, stopwatch.Elapsed, stepInputs, state))
                {
                    builder.AddEntry(entry, state, stopwatch.Elapsed, stepInputs, state);
                }
            }

            Debug.Assert(builder.Count == totalEntryItemCount);
            return builder.ToImmutableAndFree();
        }

        private NodeStateTable<(TInput1, TInput2)> RecordStepsForCachedTable(DriverStateTable.Builder graphState, NodeStateTable<(TInput1, TInput2)> previousTable, NodeStateTable<TInput1> input1Table, NodeStateTable<TInput2> input2Table)
        {
            Debug.Assert(input1Table.HasTrackedSteps && input2Table.IsCached);
            var builder = graphState.CreateTableBuilder(previousTable, _name, _comparer);
            (_, IncrementalGeneratorRunStep? input2Step) = input2Table.Single();
            foreach (var entry in input1Table)
            {
                var stepInputs = ImmutableArray.Create((entry.Step!, entry.OutputIndex), (input2Step!, 0));

                bool usedCachedEntry = builder.TryUseCachedEntries(TimeSpan.Zero, stepInputs);
                Debug.Assert(usedCachedEntry);
            }
            return builder.ToImmutableAndFree();
        }

        public IIncrementalGeneratorNode<(TInput1, TInput2)> WithComparer(IEqualityComparer<(TInput1, TInput2)> comparer) => new CombineNode<TInput1, TInput2>(_input1, _input2, comparer, _name);

        public IIncrementalGeneratorNode<(TInput1, TInput2)> WithTrackingName(string name) => new CombineNode<TInput1, TInput2>(_input1, _input2, _comparer, name);

        public void RegisterOutput(IIncrementalGeneratorOutputNode output)
        {
            // We have to call register on both branches of the join, as they may chain up to different input nodes
            _input1.RegisterOutput(output);
            _input2.RegisterOutput(output);
        }

    }
}
