// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a node in the execution pipeline of an incremental generator
    /// </summary>
    /// <typeparam name="T">The type of value this step operates on</typeparam>
    internal interface IIncrementalGeneratorNode<T>
    {
        NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T>? previousTable, CancellationToken cancellationToken);

        IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer);

        IIncrementalGeneratorNode<T> WithTrackingName(string name);

        void RegisterOutput(IIncrementalGeneratorOutputNode output);
    }
}
