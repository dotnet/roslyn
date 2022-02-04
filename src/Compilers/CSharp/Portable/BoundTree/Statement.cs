// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundNode : IBoundNodeWithIOperationChildren
    {
        ImmutableArray<BoundNode?> IBoundNodeWithIOperationChildren.Children => this.Children;

        /// <summary>
        /// Override this property to return the child bound nodes if the IOperation API corresponding to this bound node is not yet designed or implemented.
        /// </summary>
        /// <remarks>Note that any of the child bound nodes may be null.</remarks>
        protected virtual ImmutableArray<BoundNode?> Children => ImmutableArray<BoundNode?>.Empty;
    }

    internal partial class BoundBadStatement : IBoundInvalidNode
    {
        protected override ImmutableArray<BoundNode?> Children => this.ChildBoundNodes!;

        ImmutableArray<BoundNode> IBoundInvalidNode.InvalidNodeChildren => this.ChildBoundNodes;
    }

    partial class BoundFixedStatement
    {
        protected override ImmutableArray<BoundNode?> Children => ImmutableArray.Create<BoundNode?>(this.Declarations, this.Body);
    }

    partial class BoundPointerIndirectionOperator
    {
        protected override ImmutableArray<BoundNode?> Children => ImmutableArray.Create<BoundNode?>(this.Operand);
    }
}
