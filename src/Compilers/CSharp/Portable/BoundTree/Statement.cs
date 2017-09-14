// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundNode : IBoundNodeWithIOperationChildren
    {
        ImmutableArray<BoundNode> IBoundNodeWithIOperationChildren.Children => this.Children;

        /// <summary>
        /// Override this property to return the child bound nodes if the IOperation API corresponding to this bound node is not yet designed or implemented.
        /// </summary>
        /// <remarks>Note that any of the child bound nodes may be null.</remarks>
        protected virtual ImmutableArray<BoundNode> Children => ImmutableArray<BoundNode>.Empty;
    }

    internal partial class BoundBadStatement
    {
        protected override ImmutableArray<BoundNode> Children => this.ChildBoundNodes;
    }

    partial class BoundFixedStatement
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Declarations, this.Body);
    }

    partial class BoundLocalFunctionStatement
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Body);
    }

    partial class BoundMethodGroup
    {
        protected override ImmutableArray<BoundNode> Children
        {
            get
            {
                var builder = ImmutableArray.CreateBuilder<BoundNode>();
                if (InstanceOpt != null)
                {
                    builder.Add(InstanceOpt);
                }
                if (ReceiverOpt != null && ReceiverOpt != InstanceOpt)
                {
                    builder.Add(ReceiverOpt);
                }

                return builder.ToImmutable();
            }
        }
    }

    partial class BoundPointerIndirectionOperator
    {
        protected override ImmutableArray<BoundNode> Children => ImmutableArray.Create<BoundNode>(this.Operand);
    }
}
