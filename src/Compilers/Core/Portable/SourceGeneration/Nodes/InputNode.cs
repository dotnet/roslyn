// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Input nodes don't actually do anything. They are just placeholders for the value sources
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class InputNode<T> : IIncrementalGeneratorNode<T>
    {
        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable)
        {
            // the input node doesn't change the table. 
            // instead the driver manipulates the previous table to contain the current state of the node.
            // we can just return that state, as it's already up to date.
            return previousTable;
        }

        // PROTOTYPE(source-generators): how does this work? we definitly want to be able to add custom comparers to the input nodes
        // I guess its just a 'compare only' node with this as the input?
        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => this;

    }
}
