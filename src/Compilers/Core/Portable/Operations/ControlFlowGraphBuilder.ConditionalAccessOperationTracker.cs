// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal sealed partial class ControlFlowGraphBuilder
    {
        private readonly struct ConditionalAccessOperationTracker
        {
            /// <summary>
            /// Represents the stack <see cref="IConditionalAccessOperation.Operation"/>s of a tree of conditional accesses. The top of the stack is the
            /// deepest node, and except in error conditions it should contain a <see cref="IConditionalAccessInstanceOperation"/> that will be visited
            /// when visiting this node. This is the basic recursion that ensures that the operations are visited at the correct time.
            /// </summary>
            public readonly ArrayBuilder<IOperation>? Operations;

            /// <summary>
            /// The basic block to branch to if the top of the <see cref="Operations"/> stack is null.
            /// </summary>
            public readonly BasicBlockBuilder? WhenNull;

            public ConditionalAccessOperationTracker(ArrayBuilder<IOperation> operations, BasicBlockBuilder whenNull)
            {
                Debug.Assert(operations != null && whenNull != null);
                Operations = operations;
                WhenNull = whenNull;
            }

            [MemberNotNullWhen(false, nameof(Operations), nameof(WhenNull))]
            public bool IsDefault => Operations == null;

            public void Free()
            {
                Operations?.Free();
            }
        }
    }
}
