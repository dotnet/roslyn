// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class CollectionNode<T> : AbstractNode<IEnumerable<T>>
    {
        private readonly AbstractNode<T> _input;

        public CollectionNode(AbstractNode<T> input)
        {
            _input = input;
        }

        internal override StateTable<IEnumerable<T>> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<IEnumerable<T>> previousTable)
        {
            var inputTable = stateTable.GetLatestStateTableForNode(_input);
            foreach (var entry in inputTable)
            {
                // collect each entry, update our state table with a single entry
            }

            return previousTable;
        }
    }

    internal class UnrollNode<T> : AbstractNode<T>
    {
        private readonly AbstractNode<IEnumerable<T>> _input;

        public UnrollNode(AbstractNode<IEnumerable<T>> input)
        {
            _input = input;
        }

        internal override StateTable<T> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<T> previousTable)
        {
            var inputTable = stateTable.GetLatestStateTableForNode(_input);

            var enumerator = inputTable.GetEnumerator();
            enumerator.MoveNext();
            var entry = enumerator.Current;

            foreach (var item in entry.item)
            {
                // add each item to the output state table
            }


            return previousTable;
        }
    }
}
