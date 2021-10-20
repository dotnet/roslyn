// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        ImmutableArray<BoundNode?> Children { get; }
    }
}
