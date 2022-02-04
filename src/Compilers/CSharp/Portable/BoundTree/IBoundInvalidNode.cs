// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// For nodes that can generate an <see cref="IInvalidOperation"/>, this allows the Lazy implementation
    /// to get the children of this node on demand.
    /// </summary>
    internal interface IBoundInvalidNode
    {
        ImmutableArray<BoundNode> InvalidNodeChildren { get; }
    }
}
