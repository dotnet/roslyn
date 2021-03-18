// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal interface IOutputNode
    {
        object GetStateTable(GraphStateTable.Builder stateTable);

        IStateTable GetEmptyStateTable();
    }

    internal class OutputNode<T, U> : IOutputNode
    {
        private readonly INode<T> _source;
        private readonly Action<U, T> _action;

        public OutputNode(INode<T> source, Action<U, T> action)
        {
            _source = source;
            _action = action;
        }

        public object GetStateTable(GraphStateTable.Builder stateTable)
        {
            return stateTable.GetLatestStateTableForNode(_source);

        }

        public IStateTable GetEmptyStateTable() => StateTable<T>.Empty;

        //internal void GetStateTable(); //?

        // do we need an actual, like, apply method?
        // or can the driver just get the updated state table, and work from there?
    }
}
