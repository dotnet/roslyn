// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{

    //TODO: if we make this an interface, we can make the underlying nodes structs
    internal abstract class AbstractNode<T>
    {
        // this is the 'pull' from the child node.
        internal abstract StateTable<T> UpdateStateTable(GraphStateTable.Builder stateTableBuilder, StateTable<T> previousTable);
    }
}
