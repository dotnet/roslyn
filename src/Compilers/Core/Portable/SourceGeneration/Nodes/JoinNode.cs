// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class JoinNode<TInput1, TInput2> : IIncrementalGeneratorNode<(TInput1, ImmutableArray<TInput2>)>
    {
        private readonly IIncrementalGeneratorNode<TInput1> _input1;

        private readonly IIncrementalGeneratorNode<TInput2> _input2;

        public JoinNode(IIncrementalGeneratorNode<TInput1> input1, IIncrementalGeneratorNode<TInput2> input2)
        {
            _input1 = input1;
            _input2 = input2;
        }

        public NodeStateTable<(TInput1, ImmutableArray<TInput2>)> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<(TInput1, ImmutableArray<TInput2>)> previousTable, CancellationToken cancellationToken)
        {
            // get both input tables
            var input1Table = graphState.GetLatestStateTableForNode(_input1);
            var input2Table = graphState.GetLatestStateTableForNode(_input2);

            if (input1Table.IsCompacted && input2Table.IsCompacted)
            {
                return previousTable;
            }
            if (input1Table.IsFaulted)
            {
                return NodeStateTable<(TInput1, ImmutableArray<TInput2>)>.FromFaultedTable(input1Table);
            }
            if (input2Table.IsFaulted)
            {
                return NodeStateTable<(TInput1, ImmutableArray<TInput2>)>.FromFaultedTable(input2Table);
            }

            var builder = previousTable.ToBuilder();

            // Semantics of a join:
            //
            // When input1[i] is cached:
            //  - cached if input2 is also cached
            //  - modified otherwise
            // State of input1[i] otherwise.

            // gather the input2 items
            var isInput2Cached = input2Table.IsCompacted;
            ImmutableArray<TInput2> input2 = input2Table.Batch();

            // append the input2 items to each item in input1 
            foreach (var entry1 in input1Table)
            {
                var state = (entry1.state, isInput2Cached) switch
                {
                    (EntryState.Cached, true) => EntryState.Cached,
                    (EntryState.Cached, false) => EntryState.Modified,
                    _ => entry1.state
                };

                builder.AddEntry((entry1.item, input2), state);
            }

            return builder.ToImmutableAndFree();
        }

        // PROTOTYPE(source-generators): Is it actually ever meaningful to have a comparer for a join? Perhaps we should just put comparer on the transform nodes?
        //                             : We could run the compare when the input would show up as modified. It's... kind of odd, but at least means something
        public IIncrementalGeneratorNode<(TInput1, ImmutableArray<TInput2>)> WithComparer(IEqualityComparer<(TInput1, ImmutableArray<TInput2>)> comparer) => this;
    }
}
