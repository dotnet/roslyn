// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Operations
{
    internal interface IBoundNodeWithIOperationChildren
    {
        /// <summary>
        /// An array of child bound nodes.
        /// </summary>
        /// <remarks>Note that any of the child nodes may be null.</remarks>
        ImmutableArray<BoundNode> Children { get; }
    }
}
