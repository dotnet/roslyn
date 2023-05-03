// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a node in the execution pipeline of an incremental generator.
/// </summary>
internal interface IIncrementalGeneratorNode
{
    TransformFactory TransformFactory { get; }

    void RegisterOutput(ArrayBuilder<IIncrementalGeneratorOutputNode> outputNodes, IIncrementalGeneratorOutputNode output);
}
