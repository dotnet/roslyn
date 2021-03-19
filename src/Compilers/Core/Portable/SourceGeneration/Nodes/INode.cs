// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal interface INode<T>
    {
        StateTable<T> UpdateStateTable(GraphStateTable.Builder stateTableBuilder, StateTable<T> previousTable);

        INode<T> WithComparer(IEqualityComparer<T> comparer);
    }
}
