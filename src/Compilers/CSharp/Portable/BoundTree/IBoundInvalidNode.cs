// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
