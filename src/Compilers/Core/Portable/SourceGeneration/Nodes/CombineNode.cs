// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class CombineNode<TInput1, TInput2> : INode<(TInput1, TInput2)>
    {
        private readonly INode<TInput1> _source1;
        private readonly INode<TInput2> _source2;

        public CombineNode(INode<TInput1> source1, INode<TInput2> source2)
        {
            _source1 = source1;
            _source2 = source2;
        }

        public StateTable<(TInput1, TInput2)> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<(TInput1, TInput2)> previousTable)
        {
            // get *both* state table
            stateTable.GetLatestStateTableForNode(_source1);
            stateTable.GetLatestStateTableForNode(_source2);

            return previousTable;
        }

        public INode<(TInput1, TInput2)> WithComparer(IEqualityComparer<(TInput1, TInput2)> comparer) => this;
    }
}
