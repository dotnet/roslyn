// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AbstractSingleParentNode<T, U>
    {
        protected readonly INode<T> _parentNode;

        public AbstractSingleParentNode(INode<T> parentNode)
        {
            _parentNode = parentNode ?? throw new ArgumentNullException(nameof(parentNode));
        }

        public StateTable<U> UpdateStateTable(GraphStateTable.Builder graphState, StateTable<U> previousTable)
        {
            // get the parent
            StateTable<T> parentState = graphState.GetLatestStateTableForNode(_parentNode);
            if (parentState.IsFaulted)
            {
                return StateTable<U>.FromFaultedTable(parentState);
            }

            return UpdateStateTable(parentState, previousTable);
        }

        protected abstract StateTable<U> UpdateStateTable(StateTable<T> parentTable, StateTable<U> previousTable);
    }
}
