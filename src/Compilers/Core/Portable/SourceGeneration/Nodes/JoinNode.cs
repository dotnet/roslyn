// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal class JoinNode<TInput1, TInput2> : IIncrementalGeneratorNode<(TInput1, IEnumerable<TInput2>)>
    {
        private readonly IIncrementalGeneratorNode<TInput1> _input1;

        private readonly IIncrementalGeneratorNode<TInput2> _input2;

        public JoinNode(IIncrementalGeneratorNode<TInput1> input1, IIncrementalGeneratorNode<TInput2> input2)
        {
            _input1 = input1;
            _input2 = input2;
        }


        public NodeStateTable<(TInput1, IEnumerable<TInput2>)> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<(TInput1, IEnumerable<TInput2>)> previousTable, CancellationToken cancellationToken)
        {
            // PROTOTYPE(source-generators): all cached, faulted handling etc.

            var builder = new NodeStateTable<(TInput1, IEnumerable<TInput2>)>.Builder();

            // get both input tables
            var input1Table = graphState.GetLatestStateTableForNode(_input1);
            var input2Table = graphState.GetLatestStateTableForNode(_input2);

            // Semantics of a join:
            //
            // When input1[i] is cached:
            //  - cached if input2 is also cached
            //  - modified otherwise
            // State of input[i] otherwise.

            bool isInput2Cached = false; //TODO:

            // gather the input2 items
            ArrayBuilder<TInput2> input2Builder = ArrayBuilder<TInput2>.GetInstance();
            foreach(var entry2 in input2Table)
            {
                // we don't return removed entries to the user.
                // we're creating a new state table as part of this node, so they're no longer needed
                if (entry2.state != EntryState.Removed)
                {
                    input2Builder.Add(entry2.item);
                }
            }
            IEnumerable<TInput2> input2 = input2Builder.ToImmutableAndFree();

            // append the input2 items to each item in input1 
            foreach (var entry1 in input1Table)
            {
                var state = (entry1.state, isInput2Cached) switch
                {
                    (EntryState.Cached, true) => EntryState.Cached,
                    (EntryState.Cached, false) => EntryState.Modified,
                    _ => entry1.state
                };

                builder.AddEntries(ImmutableArray.Create((entry1.item, input2)), state);
            }

            return builder.ToImmutableAndFree();
        }

        // PROTOTYPE(source-generators):
        public IIncrementalGeneratorNode<(TInput1, IEnumerable<TInput2>)> WithComparer(IEqualityComparer<(TInput1, IEnumerable<TInput2>)> comparer) => this;
    }
}
