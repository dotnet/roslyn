// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class CollectionNode<T> : INode<IEnumerable<T>>
    {
        private readonly INode<T> _input;

        public CollectionNode(INode<T> input)
        {
            _input = input;
        }

        public StateTable<IEnumerable<T>> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<IEnumerable<T>> previousTable)
        {
            var inputTable = stateTable.GetLatestStateTableForNode(_input);
            foreach (var entry in inputTable)
            {
                // collect each entry, update our state table with a single entry
            }

            return previousTable;
        }

        public INode<IEnumerable<T>> WithComparer(IEqualityComparer<IEnumerable<T>> comparer) => this;
    }

    internal class UnrollNode<T> : INode<T>
    {
        private readonly INode<IEnumerable<T>> _input;

        public UnrollNode(INode<IEnumerable<T>> input)
        {
            _input = input;
        }

        public StateTable<T> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<T> previousTable)
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

        public INode<T> WithComparer(IEqualityComparer<T> comparer) => this;
    }
}
