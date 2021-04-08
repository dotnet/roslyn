// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a step in the execution piepline of an incremental generator
    /// </summary>
    /// <typeparam name="T">The type of value this step operates on</typeparam>
    internal interface IIncrementalGeneratorNode<T>
    {
        NodeStateTable<T> UpdateStateTable(PipelineStateTable.Builder graphState, NodeStateTable<T> previousTable);

        IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer);
    }
}
